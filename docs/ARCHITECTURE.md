# Architecture Documentation

This document provides a comprehensive overview of Vat Sentinel's architecture, design patterns, and technical implementation details.

## System Overview

Vat Sentinel is built on RimWorld's modding framework using Harmony for runtime patching and reflection for API access. The mod follows a component-based architecture with clear separation of concerns, ensuring maintainability and extensibility.

## Core Components

### 1. VatSentinelWorldComponent

**Purpose**: Persistent game component that manages the mod's state across game sessions.

**Responsibilities**:
- Maintains registry of tracked pawns (`_trackedPawns`)
- Manages ejection schedule calculations
- Handles save/load persistence via `ExposeData`
- Provides singleton access pattern for other components

**Key Methods**:
- `RegisterPawn(Pawn, Thing)`: Registers a pawn for tracking when inserted into a vat
- `UnregisterPawn(Pawn)`: Removes a pawn from tracking when ejected or removed
- `SyncVatState(Building_GrowthVat)`: Synchronizes tracking state with actual vat state
- `RecalculateAllSchedules()`: Recomputes ejection schedules (e.g., after settings changes)

**Design Pattern**: Singleton via `Instance` property, persisted via RimWorld's `GameComponent` system

### 2. VatTrackingRecord

**Purpose**: Data model representing a single tracked pawn and its ejection schedule.

**Properties**:
- `Pawn`: Reference to the tracked pawn
- `Vat`: Reference to the growth vat containing the pawn
- `TargetAgeYears`: Calculated biological age at which ejection should occur
- `EntryTick`: Game tick when the pawn was inserted into the vat

**Responsibilities**:
- Maintains pawn-vat association
- Stores ejection target calculation
- Validates record integrity (`IsValid` property)
- Handles serialization for save/load

**Design Pattern**: Value object with validation logic

### 3. VatEjectionSchedule

**Purpose**: Business logic for calculating ejection targets based on configured age thresholds.

**Responsibilities**:
- Evaluates enabled age thresholds (Child: 3, Teen: 13, Adult: 18)
- Calculates the next target age for a given pawn
- Determines time-based ejection eligibility (2-day threshold)
- Returns the earliest applicable threshold

**Key Methods**:
- `GetNextTargetAge(Pawn, VatSentinelSettings, int)`: Calculates target age based on current age and enabled settings
- `ShouldEjectByTime(VatTrackingRecord, VatSentinelSettings)`: Checks if time-based ejection threshold is met

**Design Pattern**: Strategy pattern for threshold evaluation

### 4. VatSentinelScheduler

**Purpose**: Tick-based evaluation and execution of ejection logic.

**Responsibilities**:
- Evaluates tracked pawns every 60 ticks (EvaluationIntervalTicks)
- Compares current biological age against target age
- Executes ejection when thresholds are met
- Handles ejection failures with retry logic

**Key Methods**:
- `Tick(Building_GrowthVat)`: Main evaluation loop called from Harmony patch
- `TryEject(Building_GrowthVat, Pawn)`: Attempts to eject a pawn with validation
- `InvokeTryEject(Building_GrowthVat, Pawn)`: Reflection-based ejection method invocation

**Design Pattern**: Command pattern for ejection execution

### 5. CompVatGrowerReflection

**Purpose**: Reflection-based access to RimWorld's vat API, ensuring compatibility across versions.

**Responsibilities**:
- Provides type-safe access to `Building_GrowthVat` properties
- Abstracts reflection complexity from business logic
- Handles API differences between RimWorld versions
- Validates API availability at runtime

**Key Methods**:
- `GetPawnBeingGrown(ThingWithComps)`: Retrieves the pawn currently in a vat
- `IsAvailable`: Checks if required RimWorld types are available

**Design Pattern**: Adapter pattern for API abstraction

### 6. Harmony Patches

**Location**: `Patches/BuildingGrowthVatPatches.cs`

**Patches**:
- `TryAcceptPawn_Postfix`: Registers pawn when inserted into vat
- `Notify_PawnRemoved_Postfix`: Unregisters pawn when removed
- `Tick_Postfix`: Triggers scheduler evaluation and cleanup

**Design Pattern**: Aspect-oriented programming via Harmony

## Data Flow

### Pawn Registration Flow

```
1. Player inserts pawn into vat
   ↓
2. RimWorld calls Building_GrowthVat.TryAcceptPawn()
   ↓
3. Harmony patch (TryAcceptPawn_Postfix) intercepts
   ↓
4. VatSentinelWorldComponent.RegisterPawn() called
   ↓
5. VatTrackingRecord created/updated
   ↓
6. VatEjectionSchedule.GetNextTargetAge() calculates target
   ↓
7. Record stored in _trackedPawns list
```

### Ejection Evaluation Flow

```
1. Every 60 ticks, Building_GrowthVat.Tick() called
   ↓
2. Harmony patch (Tick_Postfix) intercepts
   ↓
3. VatSentinelScheduler.Tick() called
   ↓
4. CompVatGrowerReflection.GetPawnBeingGrown() retrieves occupant
   ↓
5. VatTrackingRecord retrieved for pawn
   ↓
6. Current biological age compared to TargetAgeYears
   ↓
7. If threshold met: VatSentinelScheduler.TryEject() called
   ↓
8. Reflection-based ejection method invoked
   ↓
9. Success/failure notification displayed
   ↓
10. On success: VatTrackingRecord removed
    On failure: Retry scheduled
```

## State Management

### Persistence

Vat Sentinel uses RimWorld's `IExposable` interface for save/load persistence:

- `VatSentinelWorldComponent.ExposeData()`: Saves/loads tracked pawns and schedule
- `VatTrackingRecord.ExposeData()`: Saves/loads individual tracking records

**Persistence Strategy**:
- All tracking data persists with save games
- On load, schedules are recalculated to handle settings changes
- Invalid records (destroyed pawns/vats) are pruned during load

### State Validation

- `VatTrackingRecord.IsValid`: Validates record integrity (pawn exists, vat exists, pawn not spawned)
- `VatSentinelCleanupUtility`: Periodic cleanup of invalid records
- `SyncVatState()`: Synchronizes tracking state with actual vat state

## Error Handling

### Ejection Failures

When ejection fails (e.g., vat blocked, pawn inaccessible):
1. `InvokeTryEject()` returns `false`
2. Failure notification displayed to player
3. `VatTrackingRecord.ScheduleRetry()` called
4. Target age adjusted slightly to trigger retry on next evaluation
5. Retry continues until successful or pawn manually removed

### Reflection Failures

When RimWorld API is unavailable or changed:
1. `CompVatGrowerReflection.IsAvailable` returns `false`
2. Mod gracefully degrades (no tracking, no ejections)
3. Logs warning messages for debugging
4. No crashes or exceptions thrown

## Performance Considerations

### Optimization Strategies

1. **Evaluation Interval**: Ejection evaluation runs every 60 ticks (~1 second) rather than every tick
2. **Lazy Evaluation**: Reflection members are cached using `Lazy<T>` pattern
3. **Selective Logging**: Debug logging is conditional to avoid performance impact
4. **Cleanup Batching**: Invalid record cleanup runs every 600 ticks, not continuously

### Memory Management

- Tracking records are automatically cleaned up when pawns are removed
- References use RimWorld's reference system (no memory leaks)
- Periodic cleanup removes orphaned records

## Extension Points

### Adding New Ejection Rules

To add a new age threshold:
1. Add setting to `VatSentinelSettings`
2. Create new `VatEjectionRule` in `VatEjectionSchedule`
3. Add evaluation logic in `GetNextTargetAge()`
4. Add UI option in `VatSentinelMod.DoSettingsWindowContents()`

### Custom Ejection Logic

To customize ejection behavior:
1. Extend `VatSentinelScheduler.TryEject()`
2. Modify `InvokeTryEject()` for custom ejection methods
3. Add custom validation in `VatTrackingRecord`

## Dependencies

### External Dependencies

- **Harmony** (Lib.Harmony v2.3.6): Runtime patching framework
- **RimWorld API**: Reflection-based access to game systems
- **.NET Framework 4.7.2**: Target framework

### Internal Dependencies

- All components depend on `VatSentinelWorldComponent` for state access
- Scheduler depends on reflection helper for API access
- Patches depend on scheduler for ejection logic

## Testing Strategy

### Unit Testing

- Age calculation logic in `VatEjectionSchedule`
- Record validation in `VatTrackingRecord`
- Schedule recalculation logic

### Integration Testing

- Harmony patch integration
- Save/load persistence
- Ejection execution flow

### Compatibility Testing

- RimWorld version compatibility (1.4, 1.5, 1.6)
- Mod compatibility (EnhancedVatLearning, GrowthAccelerator)
- API changes detection

## Security Considerations

- No external network access
- No file system access beyond RimWorld's standard paths
- Reflection is read-only (no modification of RimWorld internals)
- All user input validated through RimWorld's UI system

## Future Architecture Enhancements

1. **Plugin System**: Allow other mods to register custom ejection rules
2. **Event System**: Publish events for ejection triggers (for mod integration)
3. **Configuration API**: Programmatic configuration for advanced users
4. **Analytics**: Optional telemetry for usage patterns (with user consent)

