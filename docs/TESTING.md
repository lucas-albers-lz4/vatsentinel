## TESTING.md – Vat Sentinel Validation Checklist

### 1. Prerequisites
- RimWorld 1.6 installed (Biotech DLC enabled).
- Visual Studio Build Tools 2022 with MSBuild (already part of Phase 0 setup).
- `msbuild` available in the shell (environment initialized via `VsDevCmd.bat` or equivalent).
- Mod source checked out at `vat-timer/`.

### 2. Compile The Mod Assembly
1. Open an elevated Developer Command Prompt.
2. Change directory to the repo root:
   ```
   cd C:\Users\<you>\gitroot\vat-timer
   ```
3. Restore and build the solution:
   ```
   msbuild VatSentinel.sln /t:Build /p:Configuration=Release /verbosity:minimal
   ```
4. Confirm the build writes `Assemblies/VatSentinel.dll` and no errors are reported (MSB3277 warnings are expected with reference assemblies).

### 3. Install Into RimWorld
1. Create (or update) the destination mod folder, e.g.
   ```
   %RIMWORLD_ROOT%\Mods\VatSentinel\
   ```
2. Copy the following from the repo into the destination:
   - `About/`
   - `Assemblies/`
   - `Defs/`
   - `Languages/`
   - `Textures/` (if present)
   - Supporting files (`About.xml`, `README.md`, etc.) as desired.
3. Launch RimWorld and enable “Vat Sentinel” in the Mod manager. Restart when prompted.

### 4. In-Game Developer Validation
1. Enable **Developer Mode** in RimWorld options.
2. Start a new save (ensures clean world component state).
3. Use debug tools to spawn:
   - A growth vat (`Building_GrowthVat`).
   - A pawn embryo or baby to insert (use the “Add pawn to vat” debug action on the vat).
4. Fast-forward time (`Ctrl` + `1/2/3`) until birth triggers. Expect an automatic ejection at age 0 with the debug message “VatSentinel_EjectionTriggered”.
5. Reinsert the pawn, enable accelerated growth (dev gizmo) to reach biological age 3. Verify second ejection when the threshold is reached.
6. Toggle settings:
   - Open Options → Mod Settings → Vat Sentinel.
   - Disable Child ejection, enable Teen and Adult.
   - Reinstate the pawn and verify that age-3 ejection no longer fires, while higher milestones do.

### 5. Failure Handling Checks
- Force an ejection failure by forbidding the vat or placing a wall to block interaction; confirm warning log (`VatSentinel_EjectionFailed`) and that the pawn remains scheduled for retry.
- Remove the pawn manually; ensure cleanup removes the orphaned record (`Pruned invalid vat tracking records` log).

### 6. Regression Sweep
- Load an existing save with active vats; ensure no errors during load and that existing occupants receive schedules.
- Enable reference mods (`EnhancedVatLearning`, `RimWorld-GrowthAccelerator`) and repeat steps 4–5 to spot conflicts early.

### 7. Sign-Off
- Update `TODO.md` Phase 6 checklist items with findings.
- Capture log snippets or screenshots when anomalies occur and link them in `RELEASE.md`.


