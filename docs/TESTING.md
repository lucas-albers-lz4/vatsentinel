## Testing Guide – Vat Sentinel Validation Checklist

This document outlines the comprehensive testing procedures for Vat Sentinel, ensuring reliability, compatibility, and correctness across all functionality.

### 1. Prerequisites

**Environment Setup:**
- RimWorld 1.6 installed (Biotech DLC enabled)
- Visual Studio Build Tools 2022 with MSBuild
- `msbuild` available in the shell (environment initialized via `VsDevCmd.bat` or equivalent)
- Mod source checked out at repository root
- Developer mode enabled in RimWorld for testing tools

### 2. Compile The Mod Assembly

**Build Verification:**
1. Open a Developer Command Prompt (or use the provided `build.bat` script)
2. Navigate to the repository root:
   ```batch
   cd C:\Users\<you>\gitroot\vatsentinel
   ```
3. Restore NuGet packages and build the solution:
   ```batch
   build.bat
   ```
   Or manually:
   ```batch
   msbuild VatSentinel.sln /t:Build /p:Configuration=Release /verbosity:minimal
   ```
4. **Verification Criteria:**
   - Build completes without errors
   - `Assemblies/VatSentinel.dll` is generated
   - `Assemblies/VatSentinel.pdb` is generated (for debugging)
   - MSB3277 warnings are expected and can be ignored (reference assembly version mismatches)
   - Code analyzer warnings should be reviewed but do not block the build

### 3. Install Into RimWorld

**Installation Verification:**
1. Create (or update) the destination mod folder:
   ```
   %RIMWORLD_ROOT%\Mods\VatSentinel\
   ```
2. Copy the following directories from the repository to the destination:
   - `About/` - Mod metadata
   - `Assemblies/` - Compiled DLL and symbols
   - `Defs/` - RimWorld XML definitions
   - `Languages/` - Localization files
   - `Textures/` - Mod assets (if present)
3. **Verification Steps:**
   - Launch RimWorld
   - Navigate to Mod Manager
   - Verify "Vat Sentinel" appears in the mod list
   - Enable the mod and restart when prompted
   - Confirm no errors appear in the mod loading screen
   - Verify Biotech DLC dependency is satisfied

### 4. In-Game Functional Testing

**Core Functionality Validation:**

1. **Initial Setup:**
   - Enable **Developer Mode** in RimWorld options (Options → Development Mode)
   - Start a new save game (ensures clean world component state)
   - Verify Vat Sentinel loads without errors in the log

2. **Basic Ejection Testing:**
   - Use debug tools to spawn a growth vat (`Building_GrowthVat`)
   - Spawn a pawn embryo or baby and insert into the vat using debug actions
   - Verify pawn registration: Check logs for "RegisterPawn" messages
   - Fast-forward time using `Ctrl` + `1/2/3` until biological age reaches 3 years
   - **Expected Result**: Automatic ejection occurs with success notification
   - Verify pawn is no longer in the vat and is accessible in the world

3. **Settings Configuration Testing:**
   - Open Options → Mod Settings → Vat Sentinel
   - **Test Case 1**: Disable "Eject at childhood (age 3)", enable "Eject at adolescence (age 13)"
   - Reinsert a pawn and fast-forward to age 3
   - **Expected Result**: No ejection at age 3
   - Continue to age 13
   - **Expected Result**: Ejection occurs at age 13
   - **Test Case 2**: Enable all thresholds (3, 13, 18)
   - **Expected Result**: Ejection occurs at the earliest enabled threshold (age 3)

4. **Multiple Threshold Testing:**
   - Configure mod to eject at multiple ages (e.g., 3 and 18)
   - Insert pawn and verify ejection occurs at age 3 (earliest threshold)
   - Reconfigure to only eject at age 18
   - Insert new pawn and verify no ejection at age 3, but ejection at age 18

### 5. Error Handling and Edge Cases

**Failure Scenario Testing:**

1. **Blocked Ejection Testing:**
   - Insert a pawn into a vat
   - Forbid the vat or place walls to block access
   - Fast-forward to trigger ejection age
   - **Expected Result**: 
     - Ejection attempt fails
     - Warning notification displayed to player
     - Log entry: "Ejecting [pawn] from [vat] failed. Will retry."
     - Pawn remains in vat
     - Retry scheduled (check logs for retry scheduling)

2. **Manual Removal Testing:**
   - Insert a pawn and verify registration
   - Manually remove the pawn using RimWorld's standard interface
   - **Expected Result**:
     - Pawn is removed from vat
     - Tracking record is cleaned up automatically
     - Log entry: "Unregistered pawn [name] from vat tracking" or "Pruned invalid vat tracking records"
     - No orphaned records remain

3. **Save/Load Testing:**
   - Insert a pawn and configure ejection settings
   - Save the game
   - Load the saved game
   - **Expected Result**:
     - Pawn tracking persists across save/load
     - Ejection schedules are recalculated on load
     - No errors during save/load process
     - Ejection continues to work correctly after load

4. **State Validation Testing:**
   - Insert multiple pawns into different vats
   - Verify each pawn is tracked independently
   - Remove one pawn manually
   - **Expected Result**: Other tracked pawns remain unaffected

### 6. Compatibility and Regression Testing

**Mod Compatibility:**
1. **Reference Mod Testing:**
   - Enable `EnhancedVatLearning` mod alongside Vat Sentinel
   - Repeat core functionality tests (steps 4-5)
   - **Expected Result**: No conflicts, both mods function correctly
   - Enable `RimWorld-GrowthAccelerator` mod
   - Repeat tests
   - **Expected Result**: No conflicts, accelerated growth works with Vat Sentinel

2. **Version Compatibility:**
   - Test with RimWorld 1.4, 1.5, and 1.6
   - Verify mod loads and functions correctly in each version
   - Check for version-specific API differences

3. **Regression Testing:**
   - Load an existing save game with active vats from a previous version
   - **Expected Result**: 
     - No errors during load
     - Existing occupants receive new schedules
     - Tracking records are validated and cleaned if invalid
     - Mod functions correctly with existing save data

### 7. Performance Testing

**Performance Validation:**
1. **Tick Performance:**
   - Monitor game performance with multiple vats (10+)
   - Verify no significant frame rate impact
   - Check log for excessive debug output (should be minimal in release builds)

2. **Memory Usage:**
   - Monitor memory usage over extended play sessions
   - Verify no memory leaks in tracking records
   - Confirm cleanup removes invalid records properly

### 8. Test Sign-Off

**Completion Criteria:**
- [ ] All functional tests pass (Section 4)
- [ ] All error handling tests pass (Section 5)
- [ ] Compatibility tests pass (Section 6)
- [ ] Performance is acceptable (Section 7)
- [ ] No critical bugs or crashes identified
- [ ] Log output is clean (no unexpected errors)

**Documentation:**
- Update `TODO.md` Phase 6 checklist with test results
- Document any issues found in GitHub Issues
- Capture log snippets or screenshots for any anomalies
- Update `CHANGELOG.md` with test coverage information

### 9. Automated Testing (Future)

**Planned Enhancements:**
- Unit tests for age calculation logic
- Integration tests for ejection scheduling
- Automated compatibility testing framework
- Performance benchmarking suite


