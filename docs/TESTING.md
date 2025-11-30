## Testing Guide – Vat Sentinel Validation Checklist

This document outlines the comprehensive testing procedures for Vat Sentinel, ensuring reliability, compatibility, and correctness across all functionality.

**Quick Start**: For a fast validation before release, see the [Quick Smoke Test](#quick-smoke-test) section at the end of this document.

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

2. **Age Threshold Ejection Testing (CRITICAL):**
   - **Test Age 3**: Enable only "Eject when reaching childhood (age 3)"
     - Insert pawn at age < 3
     - Fast-forward to age 3
     - **Expected Result**: Ejection occurs at age 3
   - **Test Age 7**: Enable only "Eject at growth moment (age 7)"
     - Insert pawn at age < 7 (but > 3)
     - Fast-forward to age 7
     - **Expected Result**: Ejection occurs at age 7
   - **Test Age 13**: Enable only "Eject when reaching adolescence (age 13)"
     - Insert pawn at age < 13 (but > 7)
     - Fast-forward to age 13
     - **Expected Result**: Ejection occurs at age 13

3. **Entry Age Tracking Bug Fix (CRITICAL):**
   - Enable "Eject when reaching childhood (age 3)"
   - Insert a pawn at age < 3 years into a vat
   - Fast-forward until pawn reaches age 3 and is ejected
   - **Re-insert the same pawn** (now age 3+) back into the vat
   - Wait for next hourly check
   - **Expected Result**: 
     - Pawn should NOT be immediately ejected
     - If age 7 or 13 thresholds are enabled, pawn should wait for those thresholds
     - If no other thresholds enabled, pawn should remain until RimWorld's automatic ejection at 18

4. **Multiple Thresholds - Earliest Wins (CRITICAL):**
   - Enable all three thresholds (3, 7, 13)
   - Insert pawn at age < 3
   - Fast-forward to age 3
   - **Expected Result**: Ejection occurs at age 3 (earliest enabled threshold), not at 7 or 13

5. **Settings Changes Mid-Game (CRITICAL):**
   - Enable "Eject when reaching childhood (age 3)"
   - Insert pawn at age < 3
   - **While pawn is in vat**, change settings to disable age 3, enable age 13
   - Fast-forward to age 3
   - **Expected Result**: 
     - No ejection at age 3 (setting was disabled)
     - Ejection occurs at age 13 (new setting active)

### 5. Error Handling and Edge Cases

**Failure Scenario Testing:**

1. **Blocked Ejection Retry (CRITICAL):**
   - Insert pawn into vat
   - Fast-forward until ejection should occur
   - **Before ejection**, forbid the vat or block access with walls
   - Wait for ejection attempt
   - **Expected Result**: 
     - Ejection attempt fails
     - Warning notification displayed
     - Retry is scheduled
     - After unblocking, ejection succeeds on retry

2. **Manual Removal Cleanup (CRITICAL):**
   - Insert pawn into vat
   - Verify pawn is tracked (check logs)
   - **Manually remove pawn** using RimWorld interface
   - Check logs
   - **Expected Result**: 
     - Tracking record is removed/cleaned up
     - No orphaned records remain
     - No errors in logs

3. **Save/Load Persistence (CRITICAL):**
   - Enable "Eject when reaching childhood (age 3)"
   - Insert pawn at age < 3
   - **Save the game**
   - **Load the saved game**
   - Fast-forward to age 3
   - **Expected Result**: 
     - Pawn is still tracked after load
     - Ejection occurs at age 3
     - No errors in logs

4. **Operational Vat Checking:**
   - Insert pawn into vat
   - **Turn off the vat** (flick switch) or remove power
   - Check logs
   - **Expected Result**: 
     - No "No occupant in vat" messages for turned-off vats
     - No unnecessary processing of non-operational vats

5. **Time-Based Ejection (Development Feature):**
   - Enable "Eject after 1 day in vat (development/testing only)"
   - Insert pawn into vat
   - Fast-forward 1 day (60,000 ticks)
   - **Expected Result**: Ejection occurs after 1 day regardless of age

6. **Multiple Vats Simultaneously:**
   - Enable "Eject when reaching childhood (age 3)"
   - Create 3+ vats
   - Insert different pawns into each vat (all at age < 3)
   - Fast-forward and verify each pawn ejects at age 3
   - **Expected Result**: 
     - Each pawn is tracked independently
     - All pawns eject at the same threshold (age 3) since thresholds are global
     - No interference between different vats/pawns

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
- [ ] All critical test cases pass (marked CRITICAL in Section 4-5)
- [ ] All functional tests pass (Section 4)
- [ ] All error handling tests pass (Section 5)
- [ ] Compatibility tests pass (Section 6)
- [ ] Performance is acceptable (Section 7)
- [ ] No critical bugs or crashes identified
- [ ] Log output is clean (no unexpected errors)
- [ ] Settings UI works correctly
- [ ] Mod loads without errors
- [ ] Version number is correct in AssemblyInfo.cs and About.xml
- [ ] Documentation is accurate and complete

**Documentation:**
- Update `TODO.md` Phase 6 checklist with test results
- Document any issues found in GitHub Issues
- Capture log snippets or screenshots for any anomalies
- Update `CHANGELOG.md` with test coverage information

### 9. Quick Smoke Test

For rapid validation before release (approximately 5 minutes), test these critical scenarios:

1. **Entry Age Tracking Bug Fix** - Re-insert a pawn after ejection and verify it's not immediately ejected again
2. **Age 3 Threshold** - Verify basic ejection at age 3 works
3. **Settings Changes Mid-Game** - Change settings while pawn is in vat and verify immediate effect
4. **Save/Load** - Save and load game with tracked pawn, verify persistence

These four tests cover the most critical functionality and recent bug fixes. If all pass, the mod is ready for release testing.

### 9. Automated Testing (Future)

**Planned Enhancements:**
- Unit tests for age calculation logic
- Integration tests for ejection scheduling
- Automated compatibility testing framework
- Performance benchmarking suite


