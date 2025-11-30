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
        internal float GetNextTargetAge(Pawn pawn, VatSentinelSettings settings, int entryTick)
        {
            if (pawn?.ageTracker == null || settings == null)
            {
                return float.PositiveInfinity;
            }

            var currentAge = pawn.ageTracker.AgeBiologicalYearsFloat;
            var bestAge = float.PositiveInfinity;

            Evaluate(_childRule, settings.EjectAtChild);
            Evaluate(_age7Rule, settings.EjectAtAge7);
            Evaluate(_teenRule, settings.EjectAtTeen);

            // Check time-based target (1 day) - only used for calculation, not logged here
            // Time-based ejection is checked separately in the scheduler
            if (settings.EjectAfterDays && entryTick >= 0 && Find.TickManager != null)
            {
                // Time-based ejection handled separately in scheduler
            }

            return bestAge;

            void Evaluate(VatEjectionRule rule, bool enabled)
            {
                if (!enabled)
                {
                    return;
                }

                var targetAge = rule.TargetAgeYears;
                
                if (currentAge >= targetAge - StageEpsilon)
                {
                    // Already past this stage - set target to current age (or slightly below) to trigger immediate ejection
                    // Set to currentAge - small value so that currentAge + tolerance >= target will be true
                    // This ensures ejection triggers immediately even if age increases slightly between ticks
                    var immediateTarget = currentAge - 0.00005f; // Slightly below current age
                    if (immediateTarget < bestAge)
                    {
                        bestAge = immediateTarget;
                    }
                    return;
                }

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


