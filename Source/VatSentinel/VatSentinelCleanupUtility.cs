using RimWorld;
using Verse;

namespace VatSentinel
{
    internal static class VatSentinelCleanupUtility
    {
        private const int CleanupIntervalTicks = 600;
        private static int _lastCleanupTick = -1;

        internal static void Tick(Building_GrowthVat vat)
        {
            if (VatSentinelWorldComponent.Instance == null)
            {
                return;
            }

            var tickManager = Find.TickManager;
            if (tickManager == null)
            {
                return;
            }

            var ticksGame = tickManager.TicksGame;
            if (_lastCleanupTick >= 0 && ticksGame - _lastCleanupTick < CleanupIntervalTicks)
            {
                return;
            }

            _lastCleanupTick = ticksGame;
            VatSentinelWorldComponent.Instance.ClearInvalidEntries();
        }
    }
}

