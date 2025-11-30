using Verse;

namespace VatSentinel.Scheduling
{
    internal sealed class VatEjectionSchedule : IExposable
    {
        private readonly VatEjectionRule _childRule = new("VatSentinel_EjectAtChild", 3f);
        private readonly VatEjectionRule _teenRule = new("VatSentinel_EjectAtTeen", 13f);
        private readonly VatEjectionRule _adultRule = new("VatSentinel_EjectAtAdult", 18f);
        private const float StageEpsilon = 0.01f; // ~3.6 days
        private const int TicksPerDay = 60000; // RimWorld standard: 60,000 ticks per day

        internal float GetNextTargetAge(Pawn pawn, VatSentinelSettings settings, int entryTick)
        {
            if (pawn?.ageTracker == null || settings == null)
            {
                VatSentinelLogger.Debug($"GetNextTargetAge: pawn={pawn?.LabelShort ?? "null"}, settings={settings != null}, returning infinity");
                return float.PositiveInfinity;
            }

            var currentAge = pawn.ageTracker.AgeBiologicalYearsFloat;
            var bestAge = float.PositiveInfinity;

            VatSentinelLogger.Debug($"GetNextTargetAge: Evaluating targets for {pawn.LabelShort}, current age={currentAge:F4} years, entryTick={entryTick}");

            Evaluate(_childRule, settings.EjectAtChild);
            Evaluate(_teenRule, settings.EjectAtTeen);
            Evaluate(_adultRule, settings.EjectAtAdult);

            // Check time-based target (1 day)
            if (settings.EjectAfterDays && entryTick >= 0 && Find.TickManager != null)
            {
                var currentTick = Find.TickManager.TicksGame;
                var ticksElapsed = currentTick - entryTick;
                var daysElapsed = ticksElapsed / (float)TicksPerDay;
                var targetDays = 1.0f;
                
                VatSentinelLogger.Debug($"Time-based check: currentTick={currentTick}, entryTick={entryTick}, ticksElapsed={ticksElapsed}, daysElapsed={daysElapsed:F4}, targetDays={targetDays}");
                
                if (daysElapsed < targetDays)
                {
                    // Calculate what age the pawn will be in 1 day
                    // RimWorld vats accelerate growth, but for simplicity, we'll use a time-based check
                    // This will be handled separately in the scheduler
                    VatSentinelLogger.Debug($"Time-based target not yet reached: {daysElapsed:F4} < {targetDays} days");
                }
                else
                {
                    VatSentinelLogger.Debug($"Time-based target reached: {daysElapsed:F4} >= {targetDays} days");
                }
            }

            VatSentinelLogger.Debug($"GetNextTargetAge: Best age target for {pawn.LabelShort} = {(float.IsPositiveInfinity(bestAge) ? "infinity" : bestAge.ToString("F4"))}");

            return bestAge;

            void Evaluate(VatEjectionRule rule, bool enabled)
            {
                if (!enabled)
                {
                    return;
                }

                var targetAge = rule.TargetAgeYears;
                VatSentinelLogger.Debug($"Evaluating rule {rule.LabelKey}: targetAge={targetAge}, currentAge={currentAge:F4}");
                
                if (currentAge >= targetAge - StageEpsilon)
                {
                    // Already past this stage - set target to current age (or slightly below) to trigger immediate ejection
                    // Set to currentAge - small value so that currentAge + tolerance >= target will be true
                    // This ensures ejection triggers immediately even if age increases slightly between ticks
                    var immediateTarget = currentAge - 0.00005f; // Slightly below current age
                    if (immediateTarget < bestAge)
                    {
                        bestAge = immediateTarget;
                        VatSentinelLogger.Debug($"Rule {rule.LabelKey}: Already past target age ({currentAge:F6} >= {targetAge:F4}), setting immediate ejection target = {bestAge:F6} (currentAge - 0.00005)");
                    }
                    return;
                }

                if (targetAge < bestAge)
                {
                    bestAge = targetAge;
                    VatSentinelLogger.Debug($"Rule {rule.LabelKey}: New best age target = {bestAge:F4}");
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


