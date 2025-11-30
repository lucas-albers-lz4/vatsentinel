## Development Plan: Vat Sentinel RimWorld Mod

### Overview

Create a RimWorld mod that enforces configurable time limits for pawns inside vat-growing pods. The mod monitors pawn biological age milestones and automatically ejects pawns at birth and age 3, ensuring compatibility with RimWorld 1.6 vat-related mechanics while laying groundwork for advanced nutrition and power resilience features.

### Core Functionality

- **Input:** Pawn vat assignments and configurable age thresholds.
- **Process:**
  - Track pawns currently gestating or growing within vats.
  - Evaluate biological age milestones (birth, age 3) against configurable settings.
  - Trigger vat ejection events when thresholds are reached.
  - Provide UI feedback and logging for scheduled and completed ejections.
- **Output:** Pawns ejected from vats at specified ages with clear notifications.

### Component Architecture

1. **Vat Tracking Manager**
   - Central registry for pawns in vats.
   - Updates on pawn assignment, removal, and tick events.

2. **Age Threshold Evaluator**
   - Calculates upcoming ejection ticks per pawn.
   - Supports default milestones (birth, age 3) and future configurability.

3. **Ejection Executor**
   - Handles safe pawn removal from vats.
   - Integrates with RimWorld vat device interfaces.

4. **User Interface & Feedback**
   - In-game settings menu entries for thresholds.
   - Alerts/messages when ejections are scheduled or completed.

### Resumability and State Tracking

- Persist scheduled ejection data with save games.
- Recompute schedules on load to handle version updates.
- Ensure safe fallback if dependent mods are removed.

### Compatibility Considerations

- Target RimWorld 1.6 (backward compatible with 1.4 and 1.5) and Biotech DLC vat mechanics.
- Reference implementations: `EnhancedVatLearning`, `RimWorld-GrowthAccelerator`.
- Use Harmony patches conservatively to avoid conflicts.
- Reflection-based API access ensures compatibility across RimWorld versions.

### Logging and Diagnostics

- Utilize RimWorld logging conventions for debug output.
- Provide verbose logging toggle for troubleshooting.

### Configuration

- Default thresholds: birth (tick 0) and age 3 (biological age years).
- Allow future expansion for custom ages via settings file.

### Output Verification

- Unit tests (where feasible) for age calculations.
- In-game verification checklist for ejection events.

### Command-Line / Build Tooling

- Use Harmony patching framework via `rimworld-chocolately` dependencies.
- Build with provided mods' existing setup scripts as reference.

### Future Enhancements

- Custom age thresholds per pawn or per vat.
- Integration with colony management mods for alerts.
- Extended UI for scheduling and override controls.
- Time-based ejection with configurable day thresholds.
- Integration with nutrition and power management systems.

