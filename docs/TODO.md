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
- [x] Patch entry points: `CompVatGrower.Notify_StartGrowing` to register pawns.
- [x] Patch exit points: `CompVatGrower.Notify_ContentsEjected` and `Building_GrowthVat.EjectContents` to unregister.
- [x] Add fallback cleanup via `Building_GrowthVat.Tick` to prune invalid records.
- [x] Document additional hooks (`JobDriver_EnterGrowthVat`, `LordToil_GrowingVatBirth`, `GrowthVatUtility`) for future logic.
- [x] Implement registry to track pawns entering and exiting vats.
- [x] Ensure registry persists across game saves.
- [x] Add debug logging for pawn tracking events (toggle via settings).

### Phase 3: Age Threshold Logic
- [x] Implement configuration model for age thresholds (birth, age 3 defaults) via `VatEjectionSchedule`.
- [x] Calculate ejection ticks from pawn biological age progression.
- [x] Schedule ejection events and handle edge cases (premature removal, failed ejection retries).
- [x] Add settings UI for enabling/disabling specific thresholds.

### Phase 4: Ejection Execution
- [x] Patch vat tick/update methods to trigger ejection when scheduled.
- [ ] Implement safe removal handler, leveraging reference mod patterns. *(ensure retry logic re-registers occupants on failure and cleans registry noise)*
- [ ] Provide in-game notifications/letters upon ejection.
- [ ] Add safeguards against duplicate ejections and race conditions. *(harden reflection helper, add null checks, prevent double scheduling)*

### Phase 5: UX Polish & Documentation
- [ ] Implement settings descriptions, tooltips, and localization stubs.
- [ ] Add README with installation and usage instructions.
- [ ] Document configuration defaults and compatibility notes.
- [ ] Capture screenshots or diagrams illustrating workflow (optional).

### Phase 6: Testing & Release Prep
- [ ] Validate mod in RimWorld developer mode (new game and existing saves). *(follow `TESTING.md` checklist)*
- [ ] Perform compatibility smoke test with reference mods.
- [ ] Prepare release notes and version number.
- [ ] Package mod folder for distribution (Steam Workshop/local zip).

