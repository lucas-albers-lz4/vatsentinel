using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VatSentinel.Patches
{
    internal static class CompVatGrowerPatches
    {
        private const string CompVatGrowerTypeName = CompVatGrowerReflection.TypeName;
        private static readonly Type CompVatGrowerType = AccessTools.TypeByName(CompVatGrowerTypeName);

        [HarmonyPatch]
        private static class NotifyStartGrowing
        {
            static bool Prepare()
            {
                var available = CompVatGrowerType != null;
                VatSentinelLogger.Debug($"NotifyStartGrowing.Prepare: CompVatGrowerType available = {available}");
                return available;
            }

            static MethodBase TargetMethod()
            {
                if (CompVatGrowerType == null)
                {
                    VatSentinelLogger.Debug("NotifyStartGrowing.TargetMethod: CompVatGrowerType is null, returning null");
                    return null;
                }
                
                var method = AccessTools.Method(CompVatGrowerType, "Notify_StartGrowing");
                VatSentinelLogger.Debug($"NotifyStartGrowing.TargetMethod: Found method = {method != null}");
                return method;
            }

            static void Postfix(ThingComp __instance, Pawn pawn)
            {
                pawn ??= CompVatGrowerReflection.GetPawnBeingGrown(__instance);
                if (pawn == null)
                {
                    VatSentinelLogger.Debug("NotifyStartGrowing: No pawn found");
                    return;
                }

                var vat = __instance.parent;
                VatSentinelLogger.Debug($"NotifyStartGrowing: Pawn {pawn.LabelShort} started growing in vat {vat?.LabelCap ?? "null"}");
                var manager = VatSentinelWorldComponent.Instance;
                manager?.RegisterPawn(pawn, vat);
                manager?.RecalculateScheduleFor(pawn);
            }
        }

        [HarmonyPatch]
        private static class NotifyContentsEjected
        {
            static bool Prepare() => CompVatGrowerType != null;

            static MethodBase TargetMethod() =>
                CompVatGrowerType == null
                    ? null
                    : AccessTools.Method(CompVatGrowerType, "Notify_ContentsEjected");

            static void Prefix(ThingComp __instance, out Pawn __state)
            {
                __state = CompVatGrowerReflection.GetPawnBeingGrown(__instance);
            }

            static void Postfix(Pawn __state)
            {
                if (__state != null)
                {
                    VatSentinelWorldComponent.Instance?.UnregisterPawn(__state);
                }
            }
        }

        [HarmonyPatch]
        private static class CompTick
        {
            static bool Prepare()
            {
                var available = CompVatGrowerType != null;
                VatSentinelLogger.Debug($"CompTick.Prepare: CompVatGrowerType available = {available}");
                return available;
            }

            static MethodBase TargetMethod()
            {
                if (CompVatGrowerType == null)
                {
                    VatSentinelLogger.Debug("CompTick.TargetMethod: CompVatGrowerType is null, returning null");
                    return null;
                }
                
                var method = AccessTools.Method(CompVatGrowerType, "CompTick");
                VatSentinelLogger.Debug($"CompTick.TargetMethod: Found method = {method != null}");
                return method;
            }

            static void Postfix(ThingComp __instance)
            {
                if (__instance?.parent is Building_GrowthVat vat)
                {
                    var manager = VatSentinelWorldComponent.Instance;
                    if (manager == null)
                    {
                        VatSentinelLogger.Debug("CompTick.Postfix: WorldComponent instance is null");
                        return;
                    }
                    
                    manager.SyncVatState(vat);
                    VatSentinelCleanupUtility.Tick(vat);
                    VatSentinelScheduler.Tick(vat);
                }
                else
                {
                    VatSentinelLogger.Debug($"CompTick.Postfix: __instance.parent is not Building_GrowthVat (type: {__instance?.parent?.GetType()?.Name ?? "null"})");
                }
            }
        }
    }
}

