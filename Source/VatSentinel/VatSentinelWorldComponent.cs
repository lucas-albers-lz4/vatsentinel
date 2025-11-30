using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VatSentinel
{
    public sealed class VatSentinelWorldComponent : GameComponent
    {
        private static VatSentinelWorldComponent _instance;

        private List<VatTrackingRecord> _trackedPawns = new List<VatTrackingRecord>();
        private Scheduling.VatEjectionSchedule _schedule = new Scheduling.VatEjectionSchedule();

        public VatSentinelWorldComponent(Game game) : base()
        {
            VatSentinelLogger.Debug("VatSentinelWorldComponent constructor called");
            _instance = this;
            VerifyBiotechAvailable();
        }

        public override void GameComponentTick()
        {
            // Check once after a short delay to see if Biotech becomes available
            if (Find.TickManager?.TicksGame == 100)
            {
                VerifyBiotechAvailable();
            }
        }

        private static void VerifyBiotechAvailable()
        {
            // Check for Building_GrowthVat - this is the actual type we use (CompVatGrower doesn't exist)
            var growthVatType = typeof(Building_GrowthVat);
            var hasGrowthVat = growthVatType != null;
            
            if (!hasGrowthVat)
            {
                VatSentinelLogger.Warn("Building_GrowthVat type not found! Vat Sentinel requires Biotech DLC to function. Please ensure Biotech DLC is enabled and loaded.");
                return;
            }

            // Verify essential dependencies
            var missingDependencies = new System.Collections.Generic.List<string>();
            
            // Check SelectedPawn property (essential for tracking)
            var selectedPawnProp = AccessTools.Property(growthVatType, "SelectedPawn");
            if (selectedPawnProp == null)
            {
                missingDependencies.Add("SelectedPawn property");
            }
            
            // Check Finish() method (essential for ejection)
            var finishMethod = AccessTools.Method(growthVatType, "Finish");
            if (finishMethod == null)
            {
                missingDependencies.Add("Finish() method");
            }
            
            // Check methods we patch
            var tryAcceptPawnMethod = AccessTools.Method(growthVatType, "TryAcceptPawn", new[] { typeof(Pawn) });
            if (tryAcceptPawnMethod == null)
            {
                missingDependencies.Add("TryAcceptPawn(Pawn) method");
            }
            
            var notifyPawnRemovedMethod = AccessTools.Method(growthVatType, "Notify_PawnRemoved");
            if (notifyPawnRemovedMethod == null)
            {
                missingDependencies.Add("Notify_PawnRemoved() method");
            }
            
            var tickMethod = AccessTools.Method(growthVatType, "Tick");
            if (tickMethod == null)
            {
                missingDependencies.Add("Tick() method");
            }
            
            if (missingDependencies.Count > 0)
            {
                VatSentinelLogger.Warn($"Biotech DLC found but missing required members: {string.Join(", ", missingDependencies)}. Vat Sentinel may not function correctly.");
            }
            else
            {
                VatSentinelLogger.Debug("Biotech DLC verified: All required Building_GrowthVat members found.");
            }
        }

        internal static VatSentinelWorldComponent Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (Current.Game == null)
                    {
                        VatSentinelLogger.Debug("VatSentinelWorldComponent.Instance: Current.Game is null");
                        return null;
                    }
                    
                    _instance = Current.Game.GetComponent<VatSentinelWorldComponent>();
                    if (_instance == null)
                    {
                        VatSentinelLogger.Warn("VatSentinelWorldComponent.Instance: Component not found on Game! This may indicate a registration issue.");
                    }
                    else
                    {
                        VatSentinelLogger.Debug("VatSentinelWorldComponent.Instance: Retrieved from Game");
                    }
                }

                return _instance;
            }
        }

        private VatSentinelSettings ActiveSettings => VatSentinelMod.Instance?.Settings;

        internal IReadOnlyList<VatTrackingRecord> TrackedPawns => _trackedPawns;
        internal Scheduling.VatEjectionSchedule Schedule => _schedule;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _trackedPawns, "trackedPawns", LookMode.Deep);
            Scribe_Deep.Look(ref _schedule, "schedule");
            _trackedPawns ??= new List<VatTrackingRecord>();
            _schedule ??= new Scheduling.VatEjectionSchedule();
            _trackedPawns.RemoveAll(record => !record.IsValid);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RecalculateAllSchedules();
            }
        }

        internal void RegisterPawn(Pawn pawn, Thing vat)
        {
            if (pawn == null || vat == null)
            {
                return;
            }

            var settings = ActiveSettings;
            var record = _trackedPawns.Find(r => r.MatchesPawn(pawn));
            var isNew = record == null;
            if (record == null)
            {
                record = new VatTrackingRecord();
                _trackedPawns.Add(record);
            }

            record.SetTrackedEntities(pawn, vat);
            var changed = record.UpdateScheduledTarget(settings, _schedule);

            // LOGGING STRATEGY: RegisterPawn is called every tick via SyncVatState, so we only log meaningful events:
            // - When a new pawn is first registered (helps track when pawns enter vats)
            // - When the ejection target changes (helps debug configuration changes)
            // We do NOT log when nothing changes to avoid log spam
            if (isNew)
            {
                VatSentinelLogger.Debug($"Registered pawn {pawn.LabelShort} in vat {vat.LabelCap}, targetAge={record.TargetAgeYears:F4} years.");
            }
            else if (changed)
            {
                VatSentinelLogger.Debug($"Updated ejection target for {pawn.LabelShort} in vat {vat.LabelCap} (target age {record.TargetAgeYears:F4} years).");
            }
        }

        internal void UnregisterPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (_trackedPawns.RemoveAll(record => record.MatchesPawn(pawn)) > 0)
            {
                VatSentinelLogger.Debug($"Unregistered pawn {pawn.LabelShort} from vat tracking.");
            }
        }

        internal void ClearInvalidEntries()
        {
            if (_trackedPawns.RemoveAll(record => !record.IsValid) > 0)
            {
                VatSentinelLogger.Debug("Pruned invalid vat tracking records.");
            }
        }

        internal void RecalculateScheduleFor(Pawn pawn)
        {
            VatSentinelLogger.Debug($"RecalculateScheduleFor: Recalculating for {pawn?.LabelShort ?? "null"}");
            var settings = ActiveSettings;
            var record = _trackedPawns.Find(r => r.MatchesPawn(pawn));
            if (record != null)
            {
                var changed = record.UpdateScheduledTarget(settings, _schedule);
                VatSentinelLogger.Debug($"RecalculateScheduleFor: {pawn.LabelShort} schedule recalculated, changed={changed}, targetAge={record.TargetAgeYears:F4} years");
            }
            else
            {
                VatSentinelLogger.Debug($"RecalculateScheduleFor: No record found for {pawn?.LabelShort ?? "null"}");
            }
        }

        internal void RecalculateAllSchedules()
        {
            var settings = ActiveSettings;
            foreach (var record in _trackedPawns)
            {
                record?.UpdateScheduledTarget(settings, _schedule);
            }
        }

        internal void ScheduleRetry(Pawn pawn)
        {
            var record = _trackedPawns.Find(r => r.MatchesPawn(pawn));
            if (record == null)
            {
                return;
            }

            record.ScheduleRetry();
            VatSentinelLogger.Debug($"Scheduled retry for {pawn.LabelShort} due to failed ejection.");
        }

        internal VatTrackingRecord GetRecord(Pawn pawn) =>
            _trackedPawns.Find(record => record.MatchesPawn(pawn));

        internal void SyncVatState(Building_GrowthVat vat)
        {
            if (vat == null || !CompVatGrowerReflection.IsAvailable)
            {
                return;
            }

            var occupant = CompVatGrowerReflection.GetPawnBeingGrown(vat);

            if (occupant != null)
            {
                RegisterPawn(occupant, vat);
                return;
            }

            var removed = _trackedPawns.RemoveAll(record =>
                record.MatchesVat(vat) &&
                (!record.HasValidPawn ||
                 record.Pawn == null ||
                 record.Pawn.Spawned ||
                 record.Pawn.DestroyedOrNull()));

            if (removed > 0)
            {
                VatSentinelLogger.Debug($"Removed tracking for empty vat {vat.LabelCap}.");
            }
        }
    }
}

