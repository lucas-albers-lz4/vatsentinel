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
            var manager = VatSentinelWorldComponent.Instance;
            if (manager == null)
            {
                return;
            }
            
            // Sync state and cleanup run every tick (lightweight operations)
            manager.SyncVatState(__instance);
            VatSentinelCleanupUtility.Tick(__instance);
            
            // Scheduler evaluation runs hourly (handles its own interval check)
            VatSentinelScheduler.Tick(__instance);
        }
    }
}

