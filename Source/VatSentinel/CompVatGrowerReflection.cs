using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace VatSentinel
{
    internal static class CompVatGrowerReflection
    {
        internal const string TypeName = "RimWorld.CompVatGrower";

        private static readonly Lazy<Type> CompTypeLazy = new(() => AccessTools.TypeByName(TypeName));
        private static readonly Lazy<PropertyInfo> PawnBeingGrownPropertyLazy = new(GetPawnBeingGrownProperty);
        private static readonly Lazy<MethodInfo> TryEjectPawnLazy = new(GetTryEjectPawn);

        private static bool _warnedMissingType;
        private static bool _warnedMissingProperty;

        internal static bool IsAvailable
        {
            get
            {
                var available = CompTypeLazy.Value != null;
                if (!_warnedMissingType && !available)
                {
                    VatSentinelLogger.Debug("CompVatGrowerReflection.IsAvailable: CompVatGrower type not found");
                }
                return available;
            }
        }

        internal static ThingComp GetVatComp(ThingWithComps thing)
        {
            var compType = CompTypeLazy.Value;
            if (compType == null)
            {
                WarnMissingType();
                return null;
            }

            if (thing?.AllComps == null)
            {
                return null;
            }

            for (var i = 0; i < thing.AllComps.Count; i++)
            {
                var comp = thing.AllComps[i];
                if (comp != null && compType.IsInstanceOfType(comp))
                {
                    return comp;
                }
            }

            return null;
        }

        internal static Pawn GetPawnBeingGrown(ThingComp comp)
        {
            if (comp == null)
            {
                return null;
            }

            var property = PawnBeingGrownPropertyLazy.Value;
            if (property == null)
            {
                WarnMissingProperty();
                return null;
            }

            try
            {
                return property.GetValue(comp) as Pawn;
            }
            catch (TargetInvocationException ex)
            {
                VatSentinelLogger.Warn($"Failed to read PawnBeingGrown: {ex.InnerException?.Message ?? ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                VatSentinelLogger.Warn($"Failed to read PawnBeingGrown: {ex.Message}");
                return null;
            }
        }

        internal static MethodInfo TryEjectPawnMethod => TryEjectPawnLazy.Value;

        private static PropertyInfo GetPawnBeingGrownProperty()
        {
            var type = CompTypeLazy.Value;
            if (type == null)
            {
                return null;
            }

            return AccessTools.Property(type, "PawnBeingGrown");
        }

        private static MethodInfo GetTryEjectPawn()
        {
            var method = AccessTools.Method(TypeName + ":TryEjectPawn");
            if (method == null && IsAvailable)
            {
                VatSentinelLogger.Warn("CompVatGrower.TryEjectPawn could not be located; using building fallback only.");
            }

            return method;
        }

        private static void WarnMissingType()
        {
            if (_warnedMissingType)
            {
                return;
            }

            _warnedMissingType = true;
            VatSentinelLogger.Warn("CompVatGrower type not found; Vat Sentinel will skip vat monitoring until Biotech is available.");
        }

        private static void WarnMissingProperty()
        {
            if (_warnedMissingProperty)
            {
                return;
            }

            _warnedMissingProperty = true;
            VatSentinelLogger.Warn("PawnBeingGrown property not found on CompVatGrower; automatic ejection may be unavailable.");
        }
    }
}

