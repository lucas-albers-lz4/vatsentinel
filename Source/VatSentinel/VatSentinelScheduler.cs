using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VatSentinel
{
    internal static class VatSentinelScheduler
    {
        private const int EvaluationIntervalTicks = 60;
        private const float AgeToleranceYears = 0.0001f; // ~0.036 RimWorld days

        private static readonly MethodInfo BuildingTryEjectPawn = AccessTools.Method(typeof(Building_GrowthVat), "TryEjectPawn");
        private static readonly MethodInfo BuildingEjectContents = AccessTools.Method(typeof(Building_GrowthVat), "EjectContents");
        private static readonly MethodInfo BuildingFinish = AccessTools.Method(typeof(Building_GrowthVat), "Finish");

        internal static void Tick(Building_GrowthVat vat)
        {
            if (vat == null || VatSentinelWorldComponent.Instance == null)
            {
                VatSentinelLogger.Debug("Tick: vat or manager is null, skipping");
                return;
            }

            if (!CompVatGrowerReflection.IsAvailable)
            {
                VatSentinelLogger.Debug("Tick: CompVatGrower not available, skipping");
                return;
            }

            var tickManager = Find.TickManager;
            if (tickManager == null)
            {
                VatSentinelLogger.Debug("Tick: TickManager is null, skipping");
                return;
            }

            var ticksGame = tickManager.TicksGame;
            
            // Log every 3000 ticks to confirm we're being called
            if (ticksGame % 3000 == 0)
            {
                VatSentinelLogger.Debug($"Tick: VatSentinelScheduler.Tick called for vat {vat.LabelCap} at tick {ticksGame}");
            }
            
            if (ticksGame % EvaluationIntervalTicks != 0)
            {
                return;
            }

            VatSentinelLogger.Debug($"Tick: Evaluating vat {vat.LabelCap} at tick {ticksGame}");

            var occupant = CompVatGrowerReflection.GetPawnBeingGrown(vat);
            if (occupant == null)
            {
                VatSentinelLogger.Debug($"Tick: No occupant in vat {vat.LabelCap}");
                return;
            }

            VatSentinelLogger.Debug($"Tick: Found occupant {occupant.LabelShort} in vat {vat.LabelCap}");

            var manager = VatSentinelWorldComponent.Instance;
            var record = manager.GetRecord(occupant);
            if (record == null)
            {
                VatSentinelLogger.Debug($"Tick: No record found for {occupant.LabelShort}, registering");
                manager.RegisterPawn(occupant, vat);
                record = manager.GetRecord(occupant);
            }

            if (record == null)
            {
                VatSentinelLogger.Warn($"Tick: Failed to get or create record for {occupant.LabelShort}");
                return;
            }

            if (!record.HasTarget)
            {
                VatSentinelLogger.Debug($"Tick: Record for {occupant.LabelShort} has no target (targetAge={record.TargetAgeYears}, entryTick={record.EntryTick})");
                return;
            }

            VatSentinelLogger.Debug($"Tick: Record for {occupant.LabelShort}: targetAge={record.TargetAgeYears:F4} years, entryTick={record.EntryTick}, currentTick={ticksGame}");

            manager.RecalculateScheduleFor(occupant);
            record = manager.GetRecord(occupant); // Refresh record after recalculation

            var ageTracker = occupant.ageTracker;
            if (ageTracker == null)
            {
                VatSentinelLogger.Warn($"Tick: No ageTracker for {occupant.LabelShort}");
                return;
            }

            var currentAge = ageTracker.AgeBiologicalYearsFloat;
            var targetAge = record.TargetAgeYears;
            var ageWithTolerance = currentAge + AgeToleranceYears;
            var shouldEject = ageWithTolerance >= targetAge;
            
            VatSentinelLogger.Debug($"Tick: {occupant.LabelShort} current age={currentAge:F6} years, target age={targetAge:F6} years");
            VatSentinelLogger.Debug($"Tick: Age comparison: {currentAge:F6} + {AgeToleranceYears:F6} = {ageWithTolerance:F6} >= {targetAge:F6} = {shouldEject}");

            // Check time-based ejection (1 day)
            var settings = VatSentinelMod.Instance?.Settings;
            if (settings != null && manager.Schedule.ShouldEjectByTime(record, settings))
            {
                VatSentinelLogger.Debug($"Tick: Time-based ejection triggered for {occupant.LabelShort}");
                TryEject(vat, occupant);
                return;
            }

            // Check age-based ejection
            if (shouldEject)
            {
                VatSentinelLogger.Debug($"Tick: Age-based ejection triggered for {occupant.LabelShort} (age {currentAge:F6} + tolerance {AgeToleranceYears:F6} = {ageWithTolerance:F6} >= target {targetAge:F6})");
                TryEject(vat, occupant);
            }
            else
            {
                VatSentinelLogger.Debug($"Tick: Ejection conditions not met for {occupant.LabelShort} (age {currentAge:F6} + tolerance {AgeToleranceYears:F6} = {ageWithTolerance:F6} < target {targetAge:F6})");
            }
        }

        private static void TryEject(Building_GrowthVat vat, Pawn pawn)
        {
            VatSentinelLogger.Debug($"TryEject: Attempting to eject {pawn?.LabelShort ?? "null"} from {vat?.LabelCap ?? "null"}");
            
            var manager = VatSentinelWorldComponent.Instance;
            if (manager == null)
            {
                VatSentinelLogger.Warn("TryEject: Manager is null, cannot eject");
                return;
            }

            // Verify the pawn is actually in the vat before attempting ejection
            var occupant = CompVatGrowerReflection.GetPawnBeingGrown(vat);
            if (occupant == null)
            {
                VatSentinelLogger.Debug($"TryEject: No occupant found in vat {vat?.LabelCap ?? "null"}, skipping ejection");
                return;
            }

            if (occupant != pawn)
            {
                VatSentinelLogger.Debug($"TryEject: Vat occupant {occupant.LabelShort} doesn't match target pawn {pawn?.LabelShort ?? "null"}, skipping ejection");
                return;
            }

            VatSentinelLogger.Debug($"TryEject: Verified {pawn.LabelShort} is in vat {vat.LabelCap}, proceeding with ejection");
            
            // Check vat state before ejection
            VatSentinelLogger.Debug($"TryEject: Vat state - Spawned: {vat.Spawned}, Destroyed: {vat.Destroyed}, Forbidden: {vat.IsForbidden(Faction.OfPlayer)}");
            VatSentinelLogger.Debug($"TryEject: Pawn state - Spawned: {pawn.Spawned}, Destroyed: {pawn.Destroyed}, Dead: {pawn.Dead}");

            string errorDetails = null;
            var success = InvokeTryEject(vat, pawn, out errorDetails);
            VatSentinelLogger.Debug($"TryEject: InvokeTryEject returned {success} for {pawn.LabelShort}");
            
            // Double-check if pawn is still in vat after ejection attempt
            var stillInVat = CompVatGrowerReflection.GetPawnBeingGrown(vat);
            if (stillInVat == pawn)
            {
                VatSentinelLogger.Debug($"TryEject: Pawn {pawn.LabelShort} is still in vat after ejection attempt (success={success})");
            }
            else
            {
                VatSentinelLogger.Debug($"TryEject: Pawn {pawn.LabelShort} is no longer in vat after ejection attempt");
            }
            
            if (!success)
            {
                VatSentinelNotificationUtility.NotifyEjectionFailure(pawn, vat, errorDetails);
                VatSentinelLogger.Warn($"Ejecting {pawn.LabelShort} from {vat.LabelCap} failed. Will retry.{(string.IsNullOrEmpty(errorDetails) ? "" : $" Error: {errorDetails}")}");
                manager.ScheduleRetry(pawn);
                return;
            }

            var remainingPawn = CompVatGrowerReflection.GetPawnBeingGrown(vat);
            if (remainingPawn == pawn)
            {
                VatSentinelLogger.Warn($"TryEject: Pawn {pawn.LabelShort} still in vat after eject call, scheduling retry");
                VatSentinelNotificationUtility.NotifyEjectionFailure(pawn, vat, "Pawn still in vat after ejection call");
                VatSentinelLogger.Warn("Vat Sentinel detected pawn still inside vat after successful eject; scheduling retry.");
                manager.ScheduleRetry(pawn);
                return;
            }

            VatSentinelLogger.Debug($"TryEject: Successfully ejected {pawn.LabelShort} from {vat.LabelCap}");
            VatSentinelNotificationUtility.NotifyEjectionSuccess(pawn, vat);
            VatSentinelLogger.Debug($"{pawn.LabelShort} automatically ejected from {vat.LabelCap}.");
            manager.UnregisterPawn(pawn);
        }

        private static bool InvokeTryEject(Building_GrowthVat vat, Pawn pawn, out string errorDetails)
        {
            errorDetails = null;
            
            // Try Finish() method first - this is the working method we discovered
            // This is the primary and fastest path
            if (BuildingFinish != null)
            {
                try
                {
                    VatSentinelLogger.Debug("InvokeTryEject: Calling Finish() method via AccessTools (primary ejection method)");
                    var result = BuildingFinish.Invoke(vat, Array.Empty<object>());
                    VatSentinelLogger.Debug($"InvokeTryEject: Finish() succeeded! Returned: {result ?? "null"}");
                    return true;
                }
                catch (TargetInvocationException ex)
                {
                    var innerEx = ex.InnerException;
                    var errorMsg = $"{innerEx?.GetType().Name ?? ex.GetType().Name}: {innerEx?.Message ?? ex.Message}";
                    VatSentinelLogger.Warn($"InvokeTryEject: Finish() method failed: {errorMsg}");
                    errorDetails = $"Finish() exception: {errorMsg}";
                }
                catch (Exception ex)
                {
                    var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
                    VatSentinelLogger.Warn($"InvokeTryEject: Finish() method failed: {errorMsg}");
                    errorDetails = $"Finish() exception: {errorMsg}";
                }
            }
            else
            {
                VatSentinelLogger.Debug("InvokeTryEject: BuildingFinish method not found via AccessTools, trying manual discovery...");
            }
            
            // Fallback: try to find Finish method manually if AccessTools didn't find it
            var allMethods = typeof(Building_GrowthVat).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var finishMethod = Array.Find(allMethods, m => m.Name == "Finish" && m.GetParameters().Length == 0);
            if (finishMethod != null)
            {
                try
                {
                    VatSentinelLogger.Debug("InvokeTryEject: Found Finish() method manually, trying to invoke...");
                    var result = finishMethod.Invoke(vat, Array.Empty<object>());
                    VatSentinelLogger.Debug($"InvokeTryEject: Finish() succeeded! Returned: {result ?? "null"}");
                    return true;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
                    VatSentinelLogger.Debug($"InvokeTryEject: Manual Finish() failed: {errorMsg}");
                    if (string.IsNullOrEmpty(errorDetails))
                    {
                        errorDetails = $"Manual Finish() exception: {errorMsg}";
                    }
                }
            }
            
            // Only do expensive method discovery if Finish() completely fails
            // This should rarely happen, but provides fallback for edge cases
            VatSentinelLogger.Warn("InvokeTryEject: Finish() method not available, trying fallback methods...");
            var lastError = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(errorDetails))
            {
                lastError.AppendLine(errorDetails);
            }
            var ejectMethods = Array.FindAll(allMethods, m => m.Name.Contains("Eject", StringComparison.OrdinalIgnoreCase));
            
            // Also search for methods with "Remove", "Take", "Get", "Release", "Cancel", "Finish" which might be related
            var relatedMethods = Array.FindAll(allMethods, m => 
                m.Name.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Take", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Get", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Release", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Pawn", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Cancel", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Finish", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("Complete", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("End", StringComparison.OrdinalIgnoreCase));
            
            // Specifically look for Cancel and Finish methods
            var cancelMethods = Array.FindAll(allMethods, m => m.Name.Contains("Cancel", StringComparison.OrdinalIgnoreCase));
            var finishMethods = Array.FindAll(allMethods, m => m.Name.Contains("Finish", StringComparison.OrdinalIgnoreCase));
            
            VatSentinelLogger.Debug($"InvokeTryEject: Building_GrowthVat has {allMethods.Length} total methods");
            
            if (ejectMethods.Length > 0)
            {
                VatSentinelLogger.Debug($"InvokeTryEject: Found {ejectMethods.Length} methods with 'Eject' in name:");
                foreach (var method in ejectMethods)
                {
                    var paramInfo = method.GetParameters();
                    var paramStr = string.Join(", ", Array.ConvertAll(paramInfo, p => $"{p.ParameterType.Name} {p.Name}"));
                    VatSentinelLogger.Debug($"  - {method.Name}({paramStr}) [{(method.IsPublic ? "Public" : "NonPublic")}]");
                }
            }
            else
            {
                VatSentinelLogger.Warn("InvokeTryEject: No methods with 'Eject' in name found!");
            }
            
            // Log Cancel methods specifically
            if (cancelMethods.Length > 0)
            {
                VatSentinelLogger.Debug($"InvokeTryEject: Found {cancelMethods.Length} methods with 'Cancel' in name (PROMISING):");
                foreach (var method in cancelMethods)
                {
                    var paramInfo = method.GetParameters();
                    var paramStr = string.Join(", ", Array.ConvertAll(paramInfo, p => $"{p.ParameterType.Name} {p.Name}"));
                    VatSentinelLogger.Debug($"  - {method.Name}({paramStr}) [{(method.IsPublic ? "Public" : "NonPublic")}]");
                }
            }
            
            // Log Finish methods specifically
            if (finishMethods.Length > 0)
            {
                VatSentinelLogger.Debug($"InvokeTryEject: Found {finishMethods.Length} methods with 'Finish' in name (PROMISING):");
                foreach (var method in finishMethods)
                {
                    var paramInfo = method.GetParameters();
                    var paramStr = string.Join(", ", Array.ConvertAll(paramInfo, p => $"{p.ParameterType.Name} {p.Name}"));
                    VatSentinelLogger.Debug($"  - {method.Name}({paramStr}) [{(method.IsPublic ? "Public" : "NonPublic")}]");
                }
            }
            
            // Log other related methods (but limit output to avoid spam)
            if (relatedMethods.Length > 0 && relatedMethods.Length <= 50)
            {
                VatSentinelLogger.Debug($"InvokeTryEject: Found {relatedMethods.Length} potentially related methods (Remove/Take/Get/Release/Pawn/Cancel/Finish/Complete/End):");
                foreach (var method in relatedMethods)
                {
                    var paramInfo = method.GetParameters();
                    var paramStr = string.Join(", ", Array.ConvertAll(paramInfo, p => $"{p.ParameterType.Name} {p.Name}"));
                    VatSentinelLogger.Debug($"  - {method.Name}({paramStr}) [{(method.IsPublic ? "Public" : "NonPublic")}]");
                }
            }
            else if (relatedMethods.Length > 50)
            {
                VatSentinelLogger.Debug($"InvokeTryEject: Found {relatedMethods.Length} potentially related methods (too many to list, focusing on Cancel/Finish methods)");
            }
            
            // Check for properties that might help
            var allProperties = typeof(Building_GrowthVat).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var relevantProps = Array.FindAll(allProperties, p => 
                p.Name.Contains("Pawn", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Selected", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Occupant", StringComparison.OrdinalIgnoreCase));
            if (relevantProps.Length > 0)
            {
                VatSentinelLogger.Debug($"InvokeTryEject: Found {relevantProps.Length} potentially relevant properties:");
                foreach (var prop in relevantProps)
                {
                    VatSentinelLogger.Debug($"  - {prop.Name}: {prop.PropertyType.Name} [{(prop.GetMethod?.IsPublic ?? false ? "Public" : "NonPublic")}] [Settable: {prop.SetMethod != null}]");
                }
            }
            
            // Check base classes for methods
            var baseType = typeof(Building_GrowthVat).BaseType;
            var baseClassMethods = new List<System.Reflection.MethodInfo>();
            while (baseType != null && baseType != typeof(object))
            {
                var baseMethods = baseType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var baseEjectMethods = Array.FindAll(baseMethods, m => m.Name.Contains("Eject", StringComparison.OrdinalIgnoreCase));
                if (baseEjectMethods.Length > 0)
                {
                    VatSentinelLogger.Debug($"InvokeTryEject: Found {baseEjectMethods.Length} methods with 'Eject' in base class {baseType.Name}:");
                    foreach (var method in baseEjectMethods)
                    {
                        var paramInfo = method.GetParameters();
                        var paramStr = string.Join(", ", Array.ConvertAll(paramInfo, p => $"{p.ParameterType.Name} {p.Name}"));
                        VatSentinelLogger.Debug($"  - {baseType.Name}.{method.Name}({paramStr})");
                        baseClassMethods.Add(method);
                    }
                }
                baseType = baseType.BaseType;
            }
            
            // Check components for methods
            if (vat is ThingWithComps thingWithComps)
            {
                VatSentinelLogger.Debug($"InvokeTryEject: Checking {thingWithComps.AllComps.Count} components for ejection methods");
                foreach (var comp in thingWithComps.AllComps)
                {
                    var compType = comp.GetType();
                    var compMethods = compType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    var compEjectMethods = Array.FindAll(compMethods, m => m.Name.Contains("Eject", StringComparison.OrdinalIgnoreCase));
                    if (compEjectMethods.Length > 0)
                    {
                        VatSentinelLogger.Debug($"InvokeTryEject: Found {compEjectMethods.Length} methods with 'Eject' in component {compType.Name}:");
                        foreach (var method in compEjectMethods)
                        {
                            var paramInfo = method.GetParameters();
                            var paramStr = string.Join(", ", Array.ConvertAll(paramInfo, p => $"{p.ParameterType.Name} {p.Name}"));
                            VatSentinelLogger.Debug($"  - {compType.Name}.{method.Name}({paramStr})");
                        }
                    }
                }
            }

            // Log what we're trying to use
            if (BuildingTryEjectPawn == null)
            {
                VatSentinelLogger.Debug("InvokeTryEject: BuildingTryEjectPawn method not found via AccessTools");
            }
            else
            {
                var parameters = BuildingTryEjectPawn.GetParameters();
                var paramStr = string.Join(", ", Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                VatSentinelLogger.Debug($"InvokeTryEject: BuildingTryEjectPawn found: TryEjectPawn({paramStr})");
            }

            if (BuildingEjectContents == null)
            {
                VatSentinelLogger.Debug("InvokeTryEject: BuildingEjectContents method not found via AccessTools");
            }
            else
            {
                var parameters = BuildingEjectContents.GetParameters();
                var paramStr = string.Join(", ", Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                VatSentinelLogger.Debug($"InvokeTryEject: BuildingEjectContents found: EjectContents({paramStr})");
            }

            // Try TryEjectPawn with different parameter combinations
            if (BuildingTryEjectPawn != null)
            {
                var parameters = BuildingTryEjectPawn.GetParameters();
                VatSentinelLogger.Debug($"InvokeTryEject: Attempting TryEjectPawn with {parameters.Length} parameters");
                
                // Try different parameter combinations based on actual parameter types
                object[][] argVariations = null;
                if (parameters.Length == 0)
                {
                    argVariations = new[] { Array.Empty<object>() };
                }
                else if (parameters.Length == 1)
                {
                    // Check parameter type
                    var paramType = parameters[0].ParameterType;
                    if (paramType == typeof(Pawn))
                    {
                        argVariations = new[] { new object[] { pawn } };
                    }
                    else if (paramType == typeof(bool))
                    {
                        // Maybe it's a force parameter?
                        argVariations = new[] { new object[] { true }, new object[] { false } };
                    }
                }
                else if (parameters.Length == 2)
                {
                    // Common pattern: TryEjectPawn(Pawn pawn, bool force)
                    if (parameters[0].ParameterType == typeof(Pawn) && parameters[1].ParameterType == typeof(bool))
                    {
                        argVariations = new[] 
                        { 
                            new object[] { pawn, true },  // Try with force=true first
                            new object[] { pawn, false }
                        };
                    }
                    else
                    {
                        // Try generic combinations
                        argVariations = new[] 
                        { 
                            new object[] { pawn, true },
                            new object[] { pawn, false }
                        };
                    }
                }
                else if (parameters.Length == 3)
                {
                    argVariations = new[] 
                    { 
                        new object[] { pawn, true, false },
                        new object[] { pawn, false, false },
                        new object[] { pawn, true, true }
                    };
                }

                if (argVariations != null)
                {
                    foreach (var args in argVariations)
                    {
                        try
                        {
                            var argStr = string.Join(", ", Array.ConvertAll(args, a => a?.ToString() ?? "null"));
                            VatSentinelLogger.Debug($"InvokeTryEject: Trying TryEjectPawn({argStr})");
                            var result = BuildingTryEjectPawn.Invoke(vat, args);
                            var success = result is bool boolResult ? boolResult : true;
                            if (success)
                            {
                                VatSentinelLogger.Debug($"InvokeTryEject: TryEjectPawn succeeded! Returned: {result}");
                                errorDetails = null;
                                return true;
                            }
                            else
                            {
                                var msg = $"TryEjectPawn returned false with {args.Length} arguments";
                                VatSentinelLogger.Debug($"InvokeTryEject: {msg}");
                                lastError.AppendLine(msg);
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            var innerEx = ex.InnerException;
                            var errorMsg = $"{innerEx?.GetType().Name ?? ex.GetType().Name}: {innerEx?.Message ?? ex.Message}";
                            VatSentinelLogger.Warn($"InvokeTryEject: TryEjectPawn threw exception: {errorMsg}");
                            VatSentinelLogger.Debug($"InvokeTryEject: Exception message: {errorMsg}");
                            lastError.AppendLine($"TryEjectPawn exception: {errorMsg}");
                            if (innerEx != null)
                            {
                                VatSentinelLogger.Debug($"InvokeTryEject: Stack trace: {innerEx.StackTrace}");
                            }
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
                            VatSentinelLogger.Warn($"InvokeTryEject: TryEjectPawn failed: {errorMsg}");
                            VatSentinelLogger.Debug($"InvokeTryEject: Stack trace: {ex.StackTrace}");
                            lastError.AppendLine($"TryEjectPawn exception: {errorMsg}");
                        }
                    }
                }
                else
                {
                    var msg = $"Could not determine parameter combinations for TryEjectPawn with {parameters.Length} parameters";
                    VatSentinelLogger.Warn($"InvokeTryEject: {msg}");
                    lastError.AppendLine(msg);
                }
            }
            else
            {
                // Try to find TryEjectPawn manually if AccessTools didn't find it
                var manualMethod = Array.Find(allMethods, m => m.Name == "TryEjectPawn");
                if (manualMethod != null)
                {
                    VatSentinelLogger.Debug($"InvokeTryEject: Found TryEjectPawn manually, trying to invoke...");
                    try
                    {
                        // Try common signatures
                        var paramCount = manualMethod.GetParameters().Length;
                        object result = null;
                        if (paramCount == 1)
                        {
                            result = manualMethod.Invoke(vat, new object[] { pawn });
                        }
                        else if (paramCount == 2)
                        {
                            result = manualMethod.Invoke(vat, new object[] { pawn, true });
                        }
                        else if (paramCount == 0)
                        {
                            result = manualMethod.Invoke(vat, Array.Empty<object>());
                        }
                        
                        if (result is bool boolResult && boolResult)
                        {
                            VatSentinelLogger.Debug("InvokeTryEject: Manual TryEjectPawn invocation succeeded!");
                            errorDetails = null;
                            return true;
                        }
                        else
                        {
                            lastError.AppendLine($"Manual TryEjectPawn returned: {result}");
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
                        VatSentinelLogger.Debug($"InvokeTryEject: Manual TryEjectPawn invocation failed: {errorMsg}");
                        lastError.AppendLine($"Manual TryEjectPawn exception: {errorMsg}");
                    }
                }
            }

            // Fallback to EjectContents if TryEjectPawn failed
            if (BuildingEjectContents != null)
            {
                try
                {
                    VatSentinelLogger.Debug("InvokeTryEject: Trying EjectContents as fallback");
                    var result = BuildingEjectContents.Invoke(vat, Array.Empty<object>());
                    VatSentinelLogger.Debug($"InvokeTryEject: EjectContents completed, returned: {result ?? "null"}");
                    // EjectContents might not return a bool, so we check if pawn is still in vat after a short delay
                    // For now, assume success if no exception
                    errorDetails = null;
                    return true;
                }
                catch (TargetInvocationException ex)
                {
                    var innerEx = ex.InnerException;
                    var errorMsg = $"{innerEx?.GetType().Name ?? ex.GetType().Name}: {innerEx?.Message ?? ex.Message}";
                    VatSentinelLogger.Warn($"EjectContents invocation failed: {errorMsg}");
                    VatSentinelLogger.Debug($"EjectContents exception message: {errorMsg}");
                    lastError.AppendLine($"EjectContents exception: {errorMsg}");
                    if (innerEx != null)
                    {
                        VatSentinelLogger.Debug($"EjectContents stack trace: {innerEx.StackTrace}");
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
                    VatSentinelLogger.Warn($"EjectContents invocation failed: {errorMsg}");
                    VatSentinelLogger.Debug($"EjectContents stack trace: {ex.StackTrace}");
                    lastError.AppendLine($"EjectContents exception: {errorMsg}");
                }
            }
            else
            {
                // Try to find EjectContents manually
                var manualMethod = Array.Find(allMethods, m => m.Name == "EjectContents");
                if (manualMethod != null)
                {
                    VatSentinelLogger.Debug("InvokeTryEject: Found EjectContents manually, trying to invoke...");
                    try
                    {
                        var result = manualMethod.Invoke(vat, Array.Empty<object>());
                        VatSentinelLogger.Debug($"InvokeTryEject: Manual EjectContents invocation completed, returned: {result ?? "null"}");
                        errorDetails = null;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
                        VatSentinelLogger.Debug($"InvokeTryEject: Manual EjectContents invocation failed: {errorMsg}");
                        lastError.AppendLine($"Manual EjectContents exception: {errorMsg}");
                    }
                }
            }

            // Try Cancel methods
            foreach (var method in cancelMethods)
            {
                try
                {
                    var paramCount = method.GetParameters().Length;
                    object result = null;
                    
                    VatSentinelLogger.Debug($"InvokeTryEject: Trying Cancel method {method.Name} with {paramCount} parameters");
                    
                    if (paramCount == 0)
                    {
                        result = method.Invoke(vat, Array.Empty<object>());
                    }
                    else if (paramCount == 1)
                    {
                        // Try with pawn, then bool, then null
                        var paramType = method.GetParameters()[0].ParameterType;
                        if (paramType == typeof(Pawn))
                        {
                            result = method.Invoke(vat, new object[] { pawn });
                        }
                        else if (paramType == typeof(bool))
                        {
                            result = method.Invoke(vat, new object[] { true });
                        }
                        else
                        {
                            result = method.Invoke(vat, new object[] { null });
                        }
                    }
                    else if (paramCount == 2)
                    {
                        result = method.Invoke(vat, new object[] { pawn, true });
                    }
                    
                    var success = result is bool boolResult ? boolResult : (result != null);
                    if (success || result == null) // null might indicate success for void methods
                    {
                        VatSentinelLogger.Debug($"InvokeTryEject: Cancel method {method.Name} succeeded! Returned: {result ?? "null"}");
                        errorDetails = null;
                        return true;
                    }
                    else
                    {
                        VatSentinelLogger.Debug($"InvokeTryEject: Cancel method {method.Name} returned false");
                        lastError.AppendLine($"Cancel method {method.Name} returned false");
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
                    VatSentinelLogger.Debug($"InvokeTryEject: Cancel method {method.Name} failed: {errorMsg}");
                    lastError.AppendLine($"Cancel method {method.Name} exception: {errorMsg}");
                }
            }
            
            // Try Finish methods
            foreach (var method in finishMethods)
            {
                try
                {
                    var paramCount = method.GetParameters().Length;
                    object result = null;
                    
                    VatSentinelLogger.Debug($"InvokeTryEject: Trying Finish method {method.Name} with {paramCount} parameters");
                    
                    if (paramCount == 0)
                    {
                        result = method.Invoke(vat, Array.Empty<object>());
                    }
                    else if (paramCount == 1)
                    {
                        // Try with pawn, then bool, then null
                        var paramType = method.GetParameters()[0].ParameterType;
                        if (paramType == typeof(Pawn))
                        {
                            result = method.Invoke(vat, new object[] { pawn });
                        }
                        else if (paramType == typeof(bool))
                        {
                            result = method.Invoke(vat, new object[] { true });
                        }
                        else
                        {
                            result = method.Invoke(vat, new object[] { null });
                        }
                    }
                    else if (paramCount == 2)
                    {
                        result = method.Invoke(vat, new object[] { pawn, true });
                    }
                    
                    var success = result is bool boolResult ? boolResult : (result != null);
                    if (success || result == null) // null might indicate success for void methods
                    {
                        VatSentinelLogger.Debug($"InvokeTryEject: Finish method {method.Name} succeeded! Returned: {result ?? "null"}");
                        errorDetails = null;
                        return true;
                    }
                    else
                    {
                        VatSentinelLogger.Debug($"InvokeTryEject: Finish method {method.Name} returned false");
                        lastError.AppendLine($"Finish method {method.Name} returned false");
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
                    VatSentinelLogger.Debug($"InvokeTryEject: Finish method {method.Name} failed: {errorMsg}");
                    lastError.AppendLine($"Finish method {method.Name} exception: {errorMsg}");
                }
            }
            
            // Set error details from collected errors
            var totalEjectMethods = ejectMethods.Length + baseClassMethods.Count;
            if (totalEjectMethods == 0 && cancelMethods.Length == 0 && finishMethods.Length == 0)
            {
                errorDetails = $"No ejection/Cancel/Finish methods found. Found {relatedMethods.Length} related methods, {relevantProps.Length} relevant properties. Check logs for details.";
            }
            else if (lastError.Length > 0)
            {
                errorDetails = lastError.ToString().Trim();
            }
            else
            {
                errorDetails = $"Found {totalEjectMethods} ejection methods, {cancelMethods.Length} Cancel methods, {finishMethods.Length} Finish methods, but all returned false or failed silently";
            }
            
            VatSentinelLogger.Warn($"InvokeTryEject: All ejection methods failed - {errorDetails}");
            VatSentinelLogger.Warn($"InvokeTryEject: Summary - Eject: {ejectMethods.Length}, Cancel: {cancelMethods.Length}, Finish: {finishMethods.Length}, Base class: {baseClassMethods.Count}, Related: {relatedMethods.Length}, Properties: {relevantProps.Length}");
            return false;
        }

    }
}

