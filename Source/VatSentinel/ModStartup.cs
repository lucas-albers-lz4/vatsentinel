using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace VatSentinel
{
    public class VatSentinelMod : Mod
    {
        internal static VatSentinelMod Instance { get; private set; }

        public VatSentinelMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<VatSentinelSettings>();
            Log.Message($"[lucas.albers.vatsentinel] Loaded version {GetModVersion()}");
            VatSentinelHarmony.Bootstrap();
        }

        public VatSentinelSettings Settings { get; }

        public override string SettingsCategory() => "Vat Sentinel";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            var changed = false;

            var ejectAtChild = Settings.EjectAtChild;
            listing.CheckboxLabeled("Eject when reaching childhood (age 3)", ref ejectAtChild);
            if (ejectAtChild != Settings.EjectAtChild)
            {
                Settings.EjectAtChild = ejectAtChild;
                changed = true;
            }

            var ejectAtTeen = Settings.EjectAtTeen;
            listing.CheckboxLabeled("Eject when reaching adolescence (age 13)", ref ejectAtTeen);
            if (ejectAtTeen != Settings.EjectAtTeen)
            {
                Settings.EjectAtTeen = ejectAtTeen;
                changed = true;
            }

            var ejectAtAdult = Settings.EjectAtAdult;
            listing.CheckboxLabeled("Eject when reaching adulthood (age 18)", ref ejectAtAdult);
            if (ejectAtAdult != Settings.EjectAtAdult)
            {
                Settings.EjectAtAdult = ejectAtAdult;
                changed = true;
            }

            listing.Gap(12f);
            var ejectAfterDays = Settings.EjectAfterDays;
            listing.CheckboxLabeled("Eject after 2 days in vat", ref ejectAfterDays);
            if (ejectAfterDays != Settings.EjectAfterDays)
            {
                Settings.EjectAfterDays = ejectAfterDays;
                changed = true;
            }

            listing.End();

            if (changed)
            {
                VatSentinelWorldComponent.Instance?.RecalculateAllSchedules();
            }
        }

        private static string GetModVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version == null
                ? "unknown"
                : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public class VatSentinelSettings : ModSettings
    {
        public bool EjectAtChild = true;
        public bool EjectAtTeen;
        public bool EjectAtAdult = true;
        public bool EjectAfterDays;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EjectAtChild, "ejectAtChild", true);
            Scribe_Values.Look(ref EjectAtTeen, "ejectAtTeen");
            Scribe_Values.Look(ref EjectAtAdult, "ejectAtAdult", true);
            Scribe_Values.Look(ref EjectAfterDays, "ejectAfterDays");
        }
    }

    internal static class VatSentinelHarmony
    {
        private static bool _initialized;

        internal static void Bootstrap()
        {
            if (_initialized)
            {
                VatSentinelLogger.Debug("Harmony already initialized, skipping");
                return;
            }

            VatSentinelLogger.Debug("Initializing Harmony patches...");
            var harmony = new Harmony("lucas.albers.vatsentinel");
            harmony.PatchAll();
            VatSentinelLogger.Debug("Harmony PatchAll completed. Checking if CompVatGrower type is available...");
            
            var compVatGrowerType = AccessTools.TypeByName("RimWorld.CompVatGrower");
            if (compVatGrowerType == null)
            {
                VatSentinelLogger.Warn("CompVatGrower type not found! Patches may not be active. Is Biotech DLC enabled?");
            }
            else
            {
                VatSentinelLogger.Debug($"CompVatGrower type found: {compVatGrowerType.FullName}");
            }
            
            _initialized = true;
        }
    }
}

