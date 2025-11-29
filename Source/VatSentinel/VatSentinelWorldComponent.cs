using System;
using System.Collections.Generic;
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
                VatSentinelLogger.Debug($"RegisterPawn: pawn={pawn?.LabelShort ?? "null"}, vat={vat?.LabelCap ?? "null"}, skipping");
                return;
            }

            VatSentinelLogger.Debug($"RegisterPawn: Registering {pawn.LabelShort} in vat {vat.LabelCap}");

            var settings = ActiveSettings;
            VatSentinelLogger.Debug($"RegisterPawn: Settings - EjectAtChild={settings?.EjectAtChild}, EjectAtTeen={settings?.EjectAtTeen}, EjectAtAdult={settings?.EjectAtAdult}, EjectAfterDays={settings?.EjectAfterDays}");

            var record = _trackedPawns.Find(r => r.MatchesPawn(pawn));
            var isNew = record == null;
            if (record == null)
            {
                VatSentinelLogger.Debug($"RegisterPawn: Creating new record for {pawn.LabelShort}");
                record = new VatTrackingRecord();
                _trackedPawns.Add(record);
            }
            else
            {
                VatSentinelLogger.Debug($"RegisterPawn: Found existing record for {pawn.LabelShort}, entryTick={record.EntryTick}");
            }

            record.SetTrackedEntities(pawn, vat);
            var changed = record.UpdateScheduledTarget(settings, _schedule);

            if (isNew)
            {
                VatSentinelLogger.Debug($"Registered pawn {pawn.LabelShort} in vat {vat.LabelCap}, targetAge={record.TargetAgeYears:F4} years, entryTick={record.EntryTick}.");
            }
            else if (changed)
            {
                VatSentinelLogger.Debug($"Updated ejection target for {pawn.LabelShort} in vat {vat.LabelCap} (target age {record.TargetAgeYears:F4} years, entryTick={record.EntryTick}).");
            }
            else
            {
                VatSentinelLogger.Debug($"RegisterPawn: No change for {pawn.LabelShort}, targetAge={record.TargetAgeYears:F4} years, entryTick={record.EntryTick}");
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

            var comp = CompVatGrowerReflection.GetVatComp(vat);
            var occupant = CompVatGrowerReflection.GetPawnBeingGrown(comp);

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

