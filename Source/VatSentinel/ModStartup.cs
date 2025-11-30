using System.Reflection;
using System.Runtime.InteropServices;
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

            var ejectAtAge7 = Settings.EjectAtAge7;
            listing.CheckboxLabeled("Eject at growth moment (age 7)", ref ejectAtAge7);
            if (ejectAtAge7 != Settings.EjectAtAge7)
            {
                Settings.EjectAtAge7 = ejectAtAge7;
                changed = true;
            }

            var ejectAtTeen = Settings.EjectAtTeen;
            listing.CheckboxLabeled("Eject when reaching adolescence (age 13)", ref ejectAtTeen);
            if (ejectAtTeen != Settings.EjectAtTeen)
            {
                Settings.EjectAtTeen = ejectAtTeen;
                changed = true;
            }

            listing.Gap(12f);
            var ejectAfterDays = Settings.EjectAfterDays;
            listing.CheckboxLabeled("Eject after 1 day in vat (development/testing only)", ref ejectAfterDays);
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

        /// <summary>
        /// Gets the mod version from AssemblyInfo.cs.
        /// Note: This version should be kept in sync with the &lt;modVersion&gt; tag in About/About.xml.
        /// </summary>
        internal static string GetModVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Try AssemblyFileVersion first (from AssemblyInfo.cs)
            var fileVersionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (fileVersionAttr != null && !string.IsNullOrEmpty(fileVersionAttr.Version))
            {
                return fileVersionAttr.Version;
            }
            
            // Fallback to AssemblyVersion
            var version = assembly.GetName().Version;
            if (version != null)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
            
            return "unknown";
        }
    }

    public class VatSentinelSettings : ModSettings
    {
        public bool EjectAtChild = true;
        public bool EjectAtAge7;
        public bool EjectAtTeen;
        // Note: EjectAfterDays is for development/testing purposes only
        public bool EjectAfterDays;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EjectAtChild, "ejectAtChild", true);
            Scribe_Values.Look(ref EjectAtAge7, "ejectAtAge7");
            Scribe_Values.Look(ref EjectAtTeen, "ejectAtTeen");
            // EjectAfterDays defaults to false (development/testing only)
            Scribe_Values.Look(ref EjectAfterDays, "ejectAfterDays", false);
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
            VatSentinelLogger.Debug("Harmony PatchAll completed. Biotech DLC verification will occur when game starts.");
            
            _initialized = true;
        }
    }
}

