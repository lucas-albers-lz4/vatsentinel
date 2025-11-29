using RimWorld;
using Verse;

namespace VatSentinel
{
    internal static class VatSentinelNotificationUtility
    {
        internal static void NotifyEjectionSuccess(Pawn pawn, Building_GrowthVat vat)
        {
            if (pawn == null)
            {
                return;
            }

            var vatLabel = vat?.LabelCap ?? "vat";
            var message = $"{pawn.LabelShort} automatically ejected from {vatLabel}.";
            Messages.Message(message, MessageTypeDefOf.PositiveEvent, historical: false);
        }

        internal static void NotifyEjectionFailure(Pawn pawn, Building_GrowthVat vat)
        {
            if (pawn == null)
            {
                return;
            }

            var vatLabel = vat?.LabelCap ?? "vat";
            var message = $"Ejecting {pawn.LabelShort} from {vatLabel} failed. Will retry.";
            Messages.Message(message, MessageTypeDefOf.NegativeEvent, historical: true);
        }
    }
}


