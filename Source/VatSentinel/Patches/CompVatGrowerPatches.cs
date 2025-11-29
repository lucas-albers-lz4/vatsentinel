using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VatSentinel.Patches
{
    [HarmonyPatch(typeof(Building_GrowthVat))]
    internal static class BuildingGrowthVatPatches
    {
        [HarmonyPatch("TryAcceptPawn")]
        [HarmonyPostfix]
        private static void TryAcceptPawn_Postfix(Building_GrowthVat __instance, Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            // Check if the pawn was actually accepted by checking SelectedPawn
            var selectedPawn = CompVatGrowerReflection.GetPawnBeingGrown(__instance);
            if (selectedPawn != pawn)
            {
                // Pawn wasn't accepted, or a different pawn is selected
                return;
            }

            VatSentinelLogger.Debug($"TryAcceptPawn: Pawn {pawn.LabelShort} started growing in vat {__instance?.LabelCap ?? "null"}");
            var manager = VatSentinelWorldComponent.Instance;
            manager?.RegisterPawn(pawn, __instance);
            manager?.RecalculateScheduleFor(pawn);
        }

        [HarmonyPatch("Notify_PawnRemoved")]
        [HarmonyPostfix]
        private static void Notify_PawnRemoved_Postfix(Building_GrowthVat __instance)
        {
            var pawn = CompVatGrowerReflection.GetPawnBeingGrown(__instance);
            if (pawn != null)
            {
                VatSentinelWorldComponent.Instance?.UnregisterPawn(pawn);
            }
        }

        [HarmonyPatch("Tick")]
        [HarmonyPostfix]
        private static void Tick_Postfix(Building_GrowthVat __instance)
        {
            // Only log occasionally to avoid spam
            var tickManager = Find.TickManager;
            var shouldLog = tickManager != null && tickManager.TicksGame % 3000 == 0; // Every 3000 ticks (~50 seconds)
            
            if (shouldLog)
            {
                VatSentinelLogger.Debug($"Building_GrowthVat.Tick.Postfix: Called at tick {tickManager.TicksGame}");
            }
            
            var manager = VatSentinelWorldComponent.Instance;
            if (manager == null)
            {
                if (shouldLog)
                {
                    VatSentinelLogger.Debug("Tick.Postfix: WorldComponent instance is null");
                }
                return;
            }
            
            manager.SyncVatState(__instance);
            VatSentinelCleanupUtility.Tick(__instance);
            VatSentinelScheduler.Tick(__instance);
        }
    }
}

