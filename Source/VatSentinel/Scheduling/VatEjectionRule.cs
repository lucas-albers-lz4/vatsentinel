using Verse;

namespace VatSentinel.Scheduling
{
    internal sealed class VatEjectionRule
    {
        internal VatEjectionRule(string labelKey, float targetBiologicalAgeYears)
        {
            LabelKey = labelKey;
            TargetAgeYears = targetBiologicalAgeYears;
        }

        internal string LabelKey { get; }
        internal float TargetAgeYears { get; }

        internal float CalculateTargetAge(Pawn pawn)
        {
            if (pawn == null)
            {
                return float.PositiveInfinity;
            }

            var ageTracker = pawn.ageTracker;
            if (ageTracker == null)
            {
                return float.PositiveInfinity;
            }

            return TargetAgeYears;
        }
    }
}

