## TODO.md - Vat Sentinel RimWorld Mod Implementation Plan

### Phase 0: Environment Setup
- [x] Install RimWorld modding prerequisites via Chocolatey (Unity dependencies, Harmony build tools). *Installed `netfx-4.7.2-devpack` and `visualstudio2022buildtools`; Harmony will be managed via NuGet.*
- [x] Clone reference mods (`EnhancedVatLearning`, `RimWorld-GrowthAccelerator`) locally for study.
- [x] Set up solution/project structure mirroring RimWorld mod conventions.
- [x] Verify RimWorld assemblies are referenced correctly for compilation.

### Phase 1: Core Mod Scaffold
- [x] Create baseline mod folder layout (`About`, `Defs`, `Assemblies`, `Textures`).
- [x] Add `About.xml` with mod metadata and dependency declarations.
- [x] Initialize C# project targeting .NET Framework 4.7.2 (matching RimWorld 1.5).
- [x] Add Harmony dependency and bootstrap class for patch registration.
- [x] Implement minimal `Mod` subclass to expose settings.

### Phase 2: Vat Tracking Foundation
- [x] Patch entry points: register pawns. *Implemented in `BuildingGrowthVatPatches.TryAcceptPawn_Postfix`*
- [x] Patch exit points: to unregister. *Implemented in `BuildingGrowthVatPatches.Notify_PawnRemoved_Postfix`*
- [x] Add fallback cleanup to prune invalid records. *Implemented in `VatSentinelCleanupUtility` and `VatSentinelWorldComponent.ClearInvalidEntries`*
- [x] Document additional hooks for future logic. *Reflection helper `CompVatGrowerReflection` provides extensible access*
- [x] Implement registry to track pawns entering and exiting vats. *Implemented via `VatTrackingRecord` and `VatSentinelWorldComponent._trackedPawns`*
- [x] Ensure registry persists across game saves. *Implemented via `ExposeData` methods in `VatSentinelWorldComponent` and `VatTrackingRecord`*
- [x] Add debug logging for pawn tracking events (toggle via settings). *Implemented via `VatSentinelLogger` (currently always-on for debugging)*

### Phase 3: Age Threshold Logic
- [x] Implement configuration model for age thresholds (birth, age 3 defaults) via `VatEjectionSchedule`. *Implemented with `VatEjectionRule` for Child(3), Teen(13), Adult(18)*
- [x] Calculate ejection ticks from pawn biological age progression. *Implemented in `VatEjectionSchedule.GetNextTargetAge` and `VatSentinelScheduler.Tick`*
- [x] Schedule ejection events and handle edge cases (premature removal, failed ejection retries). *Retry logic in `VatTrackingRecord.ScheduleRetry` and `VatSentinelScheduler.TryEject`*
- [x] Add settings UI for enabling/disabling specific thresholds. *Implemented in `VatSentinelMod.DoSettingsWindowContents` with Child, Teen, Adult, and AfterDays options*

### Phase 4: Ejection Execution
- [x] Patch vat tick/update methods to trigger ejection when scheduled. *Implemented in `BuildingGrowthVatPatches.Tick_Postfix` calling `VatSentinelScheduler.Tick`*
- [x] Implement safe removal handler, leveraging reference mod patterns. *Implemented in `VatSentinelScheduler.TryEject` with retry logic and fallback methods*
- [x] Provide in-game notifications/letters upon ejection. *Implemented in `VatSentinelNotificationUtility` for success/failure messages*
- [x] Add safeguards against duplicate ejections and race conditions. *Null checks throughout, retry scheduling, and validation in `VatTrackingRecord.IsValid`*

### Phase 5: UX Polish & Documentation
- [ ] Implement settings descriptions, tooltips, and localization stubs.
- [ ] Add README with installation and usage instructions.
- [ ] Document configuration defaults and compatibility notes.
- [ ] Capture screenshots or diagrams illustrating workflow (optional).

### Phase 6: Testing & Release Prep
- [x] Validate mod in RimWorld developer mode (new game and existing saves). *Basic functionality working - mod loads, registers events, triggers ejections*
- [ ] **IN PROGRESS**: Fix issue with `EjectAfterDays` setting - debug log shows setting is read correctly, but actual error needs investigation
- [ ] Perform compatibility smoke test with reference mods.
- [ ] Prepare release notes and version number.
- [ ] Package mod folder for distribution (Steam Workshop/local zip).

### Current Issues & Next Steps
- **Issue**: Log message shows `EjectAfterDays=True` being logged, but user reports an error. Need to:
  1. Identify the actual exception/error (the provided log snippet only shows a debug message, not an error)
  2. Verify `EjectAfterDays` functionality is working correctly (time-based ejection at 2 days)
  3. Check if there are any null reference exceptions or other runtime errors
- **Status**: Core functionality is working (age-based ejection at 3 years confirmed working)
- **Next**: Investigate and fix the `EjectAfterDays` error, then complete testing phase

