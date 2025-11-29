using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VatSentinel
{
    internal static class CompVatGrowerReflection
    {
        private static readonly Lazy<PropertyInfo> SelectedPawnPropertyLazy = new(() => 
            AccessTools.Property(typeof(Building_GrowthVat), "SelectedPawn"));
        private static readonly Lazy<MethodInfo> TryEjectPawnLazy = new(() => 
            AccessTools.Method(typeof(Building_GrowthVat), "TryEjectPawn"));

        internal static bool IsAvailable => typeof(Building_GrowthVat) != null;

        internal static ThingComp GetVatComp(ThingWithComps thing)
        {
            // CompVatGrower doesn't exist - functionality is in Building_GrowthVat itself
            // Return null since we don't need a comp anymore
            return null;
        }

        internal static Pawn GetPawnBeingGrown(ThingWithComps thing)
        {
            if (thing is not Building_GrowthVat vat)
            {
                return null;
            }

            var property = SelectedPawnPropertyLazy.Value;
            if (property == null)
            {
                return null;
            }

            try
            {
                return property.GetValue(vat) as Pawn;
            }
            catch (TargetInvocationException ex)
            {
                VatSentinelLogger.Warn($"Failed to read SelectedPawn: {ex.InnerException?.Message ?? ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                VatSentinelLogger.Warn($"Failed to read SelectedPawn: {ex.Message}");
                return null;
            }
        }

        // Overload for compatibility with old code that passes ThingComp
        internal static Pawn GetPawnBeingGrown(ThingComp comp)
        {
            return comp?.parent is Building_GrowthVat vat ? GetPawnBeingGrown(vat) : null;
        }

        internal static MethodInfo TryEjectPawnMethod => TryEjectPawnLazy.Value;
    }
}

