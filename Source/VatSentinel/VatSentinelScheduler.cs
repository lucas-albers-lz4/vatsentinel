using System;
using System.Reflection;
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
            VatSentinelLogger.Debug($"Tick: {occupant.LabelShort} current age={currentAge:F4} years, target age={record.TargetAgeYears:F4} years");

            // Check time-based ejection (2 days)
            var settings = VatSentinelMod.Instance?.Settings;
            if (settings != null && manager.Schedule.ShouldEjectByTime(record, settings))
            {
                VatSentinelLogger.Debug($"Tick: Time-based ejection triggered for {occupant.LabelShort}");
                TryEject(vat, occupant);
                return;
            }

            // Check age-based ejection
            if (ageTracker.AgeBiologicalYearsFloat + AgeToleranceYears >= record.TargetAgeYears)
            {
                VatSentinelLogger.Debug($"Tick: Age-based ejection triggered for {occupant.LabelShort} (age {currentAge:F4} >= target {record.TargetAgeYears:F4})");
                TryEject(vat, occupant);
            }
            else
            {
                VatSentinelLogger.Debug($"Tick: Ejection conditions not met for {occupant.LabelShort} (age {currentAge:F4} < target {record.TargetAgeYears:F4})");
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

            var success = InvokeTryEject(vat, pawn);
            VatSentinelLogger.Debug($"TryEject: InvokeTryEject returned {success} for {pawn.LabelShort}");
            
            if (!success)
            {
                VatSentinelNotificationUtility.NotifyEjectionFailure(pawn, vat);
                VatSentinelLogger.Warn($"Ejecting {pawn.LabelShort} from {vat.LabelCap} failed. Will retry.");
                manager.ScheduleRetry(pawn);
                return;
            }

            var remainingPawn = CompVatGrowerReflection.GetPawnBeingGrown(vat);
            if (remainingPawn == pawn)
            {
                VatSentinelLogger.Warn($"TryEject: Pawn {pawn.LabelShort} still in vat after eject call, scheduling retry");
                VatSentinelNotificationUtility.NotifyEjectionFailure(pawn, vat);
                VatSentinelLogger.Warn("Vat Sentinel detected pawn still inside vat after successful eject; scheduling retry.");
                manager.ScheduleRetry(pawn);
                return;
            }

            VatSentinelLogger.Debug($"TryEject: Successfully ejected {pawn.LabelShort} from {vat.LabelCap}");
            VatSentinelNotificationUtility.NotifyEjectionSuccess(pawn, vat);
            VatSentinelLogger.Debug($"{pawn.LabelShort} automatically ejected from {vat.LabelCap}.");
            manager.UnregisterPawn(pawn);
        }

        private static bool InvokeTryEject(Building_GrowthVat vat, Pawn pawn)
        {
            // Prefer building-level method
            var args = PrepareArgs(BuildingTryEjectPawn, pawn);
            if (BuildingTryEjectPawn != null && args != null)
            {
                try
                {
                    var result = BuildingTryEjectPawn.Invoke(vat, args);
                    return result is bool boolResult ? boolResult : true;
                }
                catch (TargetInvocationException ex)
                {
                    VatSentinelLogger.Warn($"TryEjectPawn invocation failed: {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (Exception ex)
                {
                    VatSentinelLogger.Warn($"TryEjectPawn invocation failed: {ex.Message}");
                }
            }

            if (BuildingEjectContents != null)
            {
                try
                {
                    BuildingEjectContents.Invoke(vat, Array.Empty<object>());
                    return true;
                }
                catch (TargetInvocationException ex)
                {
                    VatSentinelLogger.Warn($"EjectContents invocation failed: {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (Exception ex)
                {
                    VatSentinelLogger.Warn($"EjectContents invocation failed: {ex.Message}");
                }
            }

            return false;
        }

        private static object[] PrepareArgs(MethodInfo method, Pawn pawn)
        {
            if (method == null)
            {
                return null;
            }

            var parameters = method.GetParameters();
            return parameters.Length switch
            {
                0 => Array.Empty<object>(),
                1 => new object[] { pawn },
                2 => new object[] { pawn, true },
                3 => new object[] { pawn, true, false },
                _ => null
            };
        }
    }
}

