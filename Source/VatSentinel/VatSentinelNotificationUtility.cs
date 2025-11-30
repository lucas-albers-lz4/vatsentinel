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

        internal static void NotifyEjectionFailure(Pawn pawn, Building_GrowthVat vat, string errorDetails = null)
        {
            if (pawn == null)
            {
                return;
            }

            var vatLabel = vat?.LabelCap ?? "vat";
            var message = $"Ejecting {pawn.LabelShort} from {vatLabel} failed. Will retry.";
            if (!string.IsNullOrEmpty(errorDetails))
            {
                // Truncate error details for in-game message (keep first line or first 100 chars)
                var shortError = errorDetails;
                var firstNewline = errorDetails.IndexOf('\n');
                if (firstNewline > 0 && firstNewline < 100)
                {
                    shortError = errorDetails.Substring(0, firstNewline);
                }
                else if (errorDetails.Length > 100)
                {
                    shortError = errorDetails.Substring(0, 100) + "...";
                }
                message += $" Error: {shortError}";
            }
            Messages.Message(message, MessageTypeDefOf.NegativeEvent, historical: true);
        }
    }
}


