using System;
using Verse;

namespace VatSentinel
{
    internal sealed class VatTrackingRecord : IExposable
    {
        private Pawn _pawn;
        private Thing _vat;
        private float _targetAgeYears = float.PositiveInfinity;
        private int _entryTick = -1;
        private float _entryAgeYears = -1f; // Age of pawn when they entered the vat

        public VatTrackingRecord()
        {
        }

        internal VatTrackingRecord(Pawn pawn, Thing vat)
        {
            SetTrackedEntities(pawn, vat);
        }

        internal Pawn Pawn => _pawn;
        internal Thing Vat => _vat;
        internal float TargetAgeYears => _targetAgeYears;
        internal int EntryTick => _entryTick;
        internal float EntryAgeYears => _entryAgeYears;
        internal bool HasTarget => !float.IsPositiveInfinity(_targetAgeYears) || _entryTick >= 0;
        internal bool HasValidPawn => _pawn != null && !_pawn.DestroyedOrNull() && !_pawn.Dead;
        internal bool HasValidVat => _vat != null && !_vat.DestroyedOrNull();
        internal bool IsValid => HasValidPawn && HasValidVat && !_pawn.Spawned;

        internal bool MatchesPawn(Pawn pawn) => pawn != null && _pawn != null && pawn.ThingID == _pawn.ThingID;
        internal bool MatchesVat(Thing vat) => vat != null && _vat != null && vat.ThingID == _vat.ThingID;

        internal void SetTrackedEntities(Pawn pawn, Thing vat)
        {
            var wasNew = _pawn == null || _vat == null;
            var oldPawn = _pawn; // Save old pawn reference before updating
            var oldVat = _vat; // Save old vat reference before updating
            var vatChanged = oldVat != null && vat != null && oldVat.ThingID != vat.ThingID;
            var samePawn = oldPawn != null && pawn != null && oldPawn.ThingID == pawn.ThingID;
            // Pawn is re-inserted if: same pawn, and (old pawn was spawned/ejected OR vat changed)
            var pawnReinserted = samePawn && (oldPawn != null && oldPawn.Spawned || vatChanged);
            
            // Only update entry tick/age if it hasn't been set yet, or if the vat actually changed
            // Don't update just because pawnReinserted is true - that can be incorrectly true during insertion
            var entryNotSet = _entryTick < 0 || _entryAgeYears < 0;
            
            _pawn = pawn;
            _vat = vat;
            
            // Set entry age/tick only when needed (first time or vat actually changed)
            // This prevents log spam when SetTrackedEntities is called every tick during normal operation
            // Note: For re-insertion after ejection, the record should be removed on ejection via UnregisterPawn,
            // so re-insertion will create a new record (wasNew=true) and entryNotSet will be true
            if (entryNotSet && Find.TickManager != null && pawn?.ageTracker != null)
            {
                _entryTick = Find.TickManager.TicksGame;
                _entryAgeYears = pawn.ageTracker.AgeBiologicalYearsFloat;
                VatSentinelLogger.Debug($"Recorded entry tick {_entryTick} and entry age {_entryAgeYears:F4} years for pawn {pawn?.LabelShort ?? "null"} in vat {vat?.LabelCap ?? "null"} (wasNew={wasNew}, reinserted={pawnReinserted})");
            }
            else if (vatChanged && Find.TickManager != null && pawn?.ageTracker != null)
            {
                // Vat changed - update entry for the new vat
                _entryTick = Find.TickManager.TicksGame;
                _entryAgeYears = pawn.ageTracker.AgeBiologicalYearsFloat;
                VatSentinelLogger.Debug($"Recorded entry tick {_entryTick} and entry age {_entryAgeYears:F4} years for pawn {pawn?.LabelShort ?? "null"} in vat {vat?.LabelCap ?? "null"} (vat changed from previous vat, wasNew={wasNew}, reinserted={pawnReinserted})");
            }
        }

        internal bool UpdateScheduledTarget(VatSentinelSettings settings, Scheduling.VatEjectionSchedule schedule)
        {
            var previous = _targetAgeYears;

            if (schedule == null || settings == null || _pawn == null)
            {
                _targetAgeYears = float.PositiveInfinity;
                VatSentinelLogger.Debug($"UpdateScheduledTarget: schedule={schedule != null}, settings={settings != null}, pawn={_pawn?.LabelShort ?? "null"}, returning infinity");
                return !float.IsPositiveInfinity(previous);
            }

            _targetAgeYears = schedule.GetNextTargetAge(_pawn, settings, _entryTick, _entryAgeYears);
            var changed = Math.Abs(_targetAgeYears - previous) > 1e-6f;
            
            if (changed)
            {
                VatSentinelLogger.Debug($"UpdateScheduledTarget: Target changed for {_pawn.LabelShort} from {previous:F4} to {_targetAgeYears:F4} years");
            }
            
            return changed;
        }

        internal void ScheduleRetry()
        {
            if (_pawn?.ageTracker == null)
            {
                _targetAgeYears = float.PositiveInfinity;
                return;
            }

            var currentAge = _pawn.ageTracker.AgeBiologicalYearsFloat;
            _targetAgeYears = currentAge + 0.001f;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref _pawn, "pawn");
            Scribe_References.Look(ref _vat, "vat");
            Scribe_Values.Look(ref _targetAgeYears, "targetAgeYears", float.PositiveInfinity);
            Scribe_Values.Look(ref _entryTick, "entryTick", -1);
            Scribe_Values.Look(ref _entryAgeYears, "entryAgeYears", -1f);
            
            // If loading an old save without entryAgeYears, try to calculate it from current age
            // This is a best-effort approach for backward compatibility
            if (Scribe.mode == LoadSaveMode.PostLoadInit && _entryAgeYears < 0 && _pawn?.ageTracker != null)
            {
                // We can't know the exact entry age, so we'll use current age as a fallback
                // This means the logic will work, but may not be perfect for old saves
                _entryAgeYears = _pawn.ageTracker.AgeBiologicalYearsFloat;
            }
        }
    }
}

