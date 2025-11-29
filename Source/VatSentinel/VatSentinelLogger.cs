using Verse;

namespace VatSentinel
{
    internal static class VatSentinelLogger
    {
        internal static void Debug(string message)
        {
            // Always log for debugging purposes
            Log.Message($"[lucas.albers.vatsentinel] {message}");
        }

        internal static void Warn(string message)
        {
            Log.Warning($"[lucas.albers.vatsentinel] {message}");
        }

        internal static void Error(string message)
        {
            Log.Error($"[lucas.albers.vatsentinel] {message}");
        }
    }
}

