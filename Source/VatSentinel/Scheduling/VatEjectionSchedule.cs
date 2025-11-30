using Verse;

namespace VatSentinel.Scheduling
{
    internal sealed class VatEjectionSchedule : IExposable
    {
        private readonly VatEjectionRule _childRule = new("VatSentinel_EjectAtChild", 3f);
        private readonly VatEjectionRule _age7Rule = new("VatSentinel_EjectAtAge7", 7f);
        private readonly VatEjectionRule _teenRule = new("VatSentinel_EjectAtTeen", 13f);
        private const float StageEpsilon = 0.01f; // ~3.6 days
        private const int TicksPerDay = 60000; // RimWorld standard: 60,000 ticks per day

        // LOGGING STRATEGY: This method is called frequently (every tick via RegisterPawn/SyncVatState),
        // so we do NOT log here to avoid log spam. Logging occurs in the scheduler during hourly evaluations.
        // 
        // BUG FIX: We now use entryAgeYears instead of currentAge to determine which thresholds apply.
        // If a pawn enters the vat at age 3.5, we skip the age 3 threshold and use the next applicable one (7 or 13).
        internal float GetNextTargetAge(Pawn pawn, VatSentinelSettings settings, int entryTick, float entryAgeYears)
        {
            if (pawn?.ageTracker == null || settings == null)
            {
                return float.PositiveInfinity;
            }

            // If entryAgeYears is invalid (negative), fall back to current age for backward compatibility
            // This handles edge cases where entryAgeYears wasn't set (e.g., old saves)
            var ageToCompare = entryAgeYears >= 0 ? entryAgeYears : pawn.ageTracker.AgeBiologicalYearsFloat;
            var bestAge = float.PositiveInfinity;

            // Evaluate rules based on entry age, not current age
            // Only consider thresholds that are greater than the entry age
            Evaluate(_childRule, settings.EjectAtChild, ageToCompare);
            Evaluate(_age7Rule, settings.EjectAtAge7, ageToCompare);
            Evaluate(_teenRule, settings.EjectAtTeen, ageToCompare);

            // Check time-based target (1 day) - only used for calculation, not logged here
            // Time-based ejection is checked separately in the scheduler
            if (settings.EjectAfterDays && entryTick >= 0 && Find.TickManager != null)
            {
                // Time-based ejection handled separately in scheduler
            }

            return bestAge;

            void Evaluate(VatEjectionRule rule, bool enabled, float entryAge)
            {
                if (!enabled)
                {
                    return;
                }

                var targetAge = rule.TargetAgeYears;
                
                // Only consider thresholds that are greater than the entry age
                // If the pawn entered at age 3.5 and the threshold is 3, skip it and use the next one
                if (entryAge >= targetAge - StageEpsilon)
                {
                    // Pawn was already past this threshold when they entered - skip it
                    return;
                }

                // This threshold is applicable - use it if it's the best (lowest) target
                if (targetAge < bestAge)
                {
                    bestAge = targetAge;
                }
            }
        }

        internal bool ShouldEjectByTime(VatTrackingRecord record, VatSentinelSettings settings)
        {
            if (!settings.EjectAfterDays || record.EntryTick < 0 || Find.TickManager == null)
            {
                return false;
            }

            var currentTick = Find.TickManager.TicksGame;
            var ticksElapsed = currentTick - record.EntryTick;
            var daysElapsed = ticksElapsed / (float)TicksPerDay;
            var targetDays = 1.0f;

            VatSentinelLogger.Debug($"ShouldEjectByTime: pawn={record.Pawn?.LabelShort ?? "null"}, currentTick={currentTick}, entryTick={record.EntryTick}, ticksElapsed={ticksElapsed}, daysElapsed={daysElapsed:F4}, targetDays={targetDays}");

            return daysElapsed >= targetDays;
        }

        public void ExposeData()
        {
            // Reserved for future dynamic configuration
        }
    }
}


