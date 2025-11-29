using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VatSentinel
{
    public sealed class VatSentinelWorldComponent : GameComponent
    {
        private static VatSentinelWorldComponent _instance;

        private List<VatTrackingRecord> _trackedPawns = new List<VatTrackingRecord>();
        private Scheduling.VatEjectionSchedule _schedule = new Scheduling.VatEjectionSchedule();

        public VatSentinelWorldComponent(Game game) : base()
        {
            VatSentinelLogger.Debug("VatSentinelWorldComponent constructor called");
            _instance = this;
            VerifyBiotechAvailable();
        }

        public override void GameComponentTick()
        {
            // Check once after a short delay to see if Biotech becomes available
            if (Find.TickManager?.TicksGame == 100)
            {
                VerifyBiotechAvailable();
            }
        }

        private static void VerifyBiotechAvailable()
        {
            // Check for Building_GrowthVat - this is the actual type we use (CompVatGrower doesn't exist)
            var growthVatType = typeof(Building_GrowthVat);
            var hasGrowthVat = growthVatType != null;
            
            if (hasGrowthVat)
            {
                VatSentinelLogger.Debug($"Building_GrowthVat found at: {growthVatType.Assembly.FullName}");
                VatSentinelLogger.Debug($"Building_GrowthVat namespace: {growthVatType.Namespace}");
                
                // Try to find CompVatGrower in the same assembly
                try
                {
                    var biotechAssembly = growthVatType.Assembly;
                    var allTypes = biotechAssembly.GetTypes();
                    VatSentinelLogger.Debug($"Searching {allTypes.Length} types in Biotech assembly for CompVatGrower...");
                    
                    // First, search for ANY type with "CompVat" or "VatGrower" in the name (not just ThingComp)
                    var allVatGrowerTypes = Array.FindAll(allTypes, t => 
                        (t.Name.Contains("CompVat") || t.Name.Contains("VatGrower")));
                    
                    if (allVatGrowerTypes.Length > 0)
                    {
                        VatSentinelLogger.Debug($"Found types with 'CompVat' or 'VatGrower' in name: {string.Join(", ", Array.ConvertAll(allVatGrowerTypes, t => $"{t.FullName} (IsSubclassOfThingComp: {t.IsSubclassOf(typeof(ThingComp))})"))}");
                    }
                    
                    // Search for ThingComp subclasses with "Vat" in the name
                    var vatComps = Array.FindAll(allTypes, t => 
                        t.IsSubclassOf(typeof(ThingComp)) && 
                        (t.Name.Contains("Vat") || t.Name.Contains("Grower")));
                    
                    if (vatComps.Length > 0)
                    {
                        VatSentinelLogger.Debug($"Found Vat/Grower-related ThingComp types: {string.Join(", ", Array.ConvertAll(vatComps, t => t.FullName))}");
                    }
                    
                    // Also check Building_GrowthVat's components to see what type it actually uses
                    try
                    {
                        var vatDef = DefDatabase<ThingDef>.GetNamed("GrowthVat", false);
                        if (vatDef != null)
                        {
                            VatSentinelLogger.Debug($"GrowthVat ThingDef found. Comp classes: {string.Join(", ", vatDef.comps?.Select(c => c?.compClass?.FullName ?? "null") ?? new string[0])}");
                        }
                        
                        // Check if Building_GrowthVat has the methods we're looking for
                        var growthVatMethods = growthVatType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        var notifyMethod = Array.Find(growthVatMethods, m => m.Name == "Notify_StartGrowing" || m.Name.Contains("StartGrowing") || m.Name == "StartGrowing");
                        var compTickMethod = Array.Find(growthVatMethods, m => m.Name == "CompTick" || m.Name == "Tick");
                        
                        if (notifyMethod != null)
                        {
                            VatSentinelLogger.Debug($"Found Notify_StartGrowing/StartGrowing method on Building_GrowthVat: {notifyMethod.Name} (declaring type: {notifyMethod.DeclaringType?.FullName})");
                        }
                        if (compTickMethod != null)
                        {
                            VatSentinelLogger.Debug($"Found CompTick/Tick method on Building_GrowthVat: {compTickMethod.Name} (declaring type: {compTickMethod.DeclaringType?.FullName})");
                        }
                        
                        // Check all methods with "Vat" or "Grow" in the name
                        var vatMethods = Array.FindAll(growthVatMethods, m => m.Name.Contains("Vat") || m.Name.Contains("Grow") || m.Name.Contains("Eject") || m.Name.Contains("Notify") || m.Name.Contains("Start"));
                        if (vatMethods.Length > 0)
                        {
                            VatSentinelLogger.Debug($"Building_GrowthVat methods with Vat/Grow/Eject/Notify/Start: {string.Join(", ", Array.ConvertAll(vatMethods, m => $"{m.Name}({m.DeclaringType?.Name})"))}");
                        }
                        
                        // Also check base classes for these methods
                        var baseType = growthVatType.BaseType;
                        while (baseType != null && baseType != typeof(object))
                        {
                            var baseMethods = baseType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            var baseNotify = Array.Find(baseMethods, m => m.Name == "Notify_StartGrowing" || m.Name.Contains("StartGrowing"));
                            var baseTick = Array.Find(baseMethods, m => m.Name == "CompTick");
                            if (baseNotify != null || baseTick != null)
                            {
                                VatSentinelLogger.Debug($"Found methods in base class {baseType.FullName}: Notify={baseNotify?.Name ?? "null"}, Tick={baseTick?.Name ?? "null"}");
                            }
                            baseType = baseType.BaseType;
                        }
                        
                        // Check if there's a component property or field that might be CompVatGrower
                        var fields = growthVatType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        var compFields = Array.FindAll(fields, f => f.FieldType.Name.Contains("Comp") && (f.FieldType.Name.Contains("Vat") || f.FieldType.Name.Contains("Grow")));
                        if (compFields.Length > 0)
                        {
                            VatSentinelLogger.Debug($"Building_GrowthVat fields with Comp/Vat/Grow: {string.Join(", ", Array.ConvertAll(compFields, f => $"{f.Name}: {f.FieldType.FullName}"))}");
                        }
                        
                        var properties = growthVatType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        var compProps = Array.FindAll(properties, p => p.PropertyType.Name.Contains("Comp") && (p.PropertyType.Name.Contains("Vat") || p.PropertyType.Name.Contains("Grow")));
                        if (compProps.Length > 0)
                        {
                            VatSentinelLogger.Debug($"Building_GrowthVat properties with Comp/Vat/Grow: {string.Join(", ", Array.ConvertAll(compProps, p => $"{p.Name}: {p.PropertyType.FullName}"))}");
                        }
                        
                        // Check for properties/fields that might hold the pawn
                        var pawnProps = Array.FindAll(properties, p => p.PropertyType.Name == "Pawn" || p.PropertyType.Name.Contains("Pawn"));
                        if (pawnProps.Length > 0)
                        {
                            VatSentinelLogger.Debug($"Building_GrowthVat properties with Pawn: {string.Join(", ", Array.ConvertAll(pawnProps, p => $"{p.Name}: {p.PropertyType.FullName}"))}");
                        }
                        
                        var pawnFields = Array.FindAll(fields, f => f.FieldType.Name == "Pawn" || f.FieldType.Name.Contains("Pawn"));
                        if (pawnFields.Length > 0)
                        {
                            VatSentinelLogger.Debug($"Building_GrowthVat fields with Pawn: {string.Join(", ", Array.ConvertAll(pawnFields, f => $"{f.Name}: {f.FieldType.FullName}"))}");
                        }
                        
                        // Check all methods with "Pawn" in the name
                        var pawnMethods = Array.FindAll(growthVatMethods, m => m.Name.Contains("Pawn") || m.Name.Contains("Embryo") || m.Name.Contains("Start"));
                        if (pawnMethods.Length > 0)
                        {
                            VatSentinelLogger.Debug($"Building_GrowthVat methods with Pawn/Embryo/Start: {string.Join(", ", Array.ConvertAll(pawnMethods, m => $"{m.Name}({string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name))})"))}");
                        }
                    }
                    catch (Exception ex2)
                    {
                        VatSentinelLogger.Debug($"Could not check GrowthVat ThingDef or methods: {ex2.Message}");
                    }
                }
                catch (Exception ex)
                {
                    VatSentinelLogger.Warn($"Error searching for CompVatGrower type: {ex.Message}");
                }
            }
            
            if (hasGrowthVat)
            {
                VatSentinelLogger.Debug($"Biotech DLC verified: Building_GrowthVat found at {growthVatType.Assembly.FullName}");
            }
            else
            {
                VatSentinelLogger.Warn("Building_GrowthVat type not found! Vat Sentinel requires Biotech DLC to function. Please ensure Biotech DLC is enabled and loaded.");
            }
        }

        internal static VatSentinelWorldComponent Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (Current.Game == null)
                    {
                        VatSentinelLogger.Debug("VatSentinelWorldComponent.Instance: Current.Game is null");
                        return null;
                    }
                    
                    _instance = Current.Game.GetComponent<VatSentinelWorldComponent>();
                    if (_instance == null)
                    {
                        VatSentinelLogger.Warn("VatSentinelWorldComponent.Instance: Component not found on Game! This may indicate a registration issue.");
                    }
                    else
                    {
                        VatSentinelLogger.Debug("VatSentinelWorldComponent.Instance: Retrieved from Game");
                    }
                }

                return _instance;
            }
        }

        private VatSentinelSettings ActiveSettings => VatSentinelMod.Instance?.Settings;

        internal IReadOnlyList<VatTrackingRecord> TrackedPawns => _trackedPawns;
        internal Scheduling.VatEjectionSchedule Schedule => _schedule;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _trackedPawns, "trackedPawns", LookMode.Deep);
            Scribe_Deep.Look(ref _schedule, "schedule");
            _trackedPawns ??= new List<VatTrackingRecord>();
            _schedule ??= new Scheduling.VatEjectionSchedule();
            _trackedPawns.RemoveAll(record => !record.IsValid);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RecalculateAllSchedules();
            }
        }

        internal void RegisterPawn(Pawn pawn, Thing vat)
        {
            if (pawn == null || vat == null)
            {
                VatSentinelLogger.Debug($"RegisterPawn: pawn={pawn?.LabelShort ?? "null"}, vat={vat?.LabelCap ?? "null"}, skipping");
                return;
            }

            VatSentinelLogger.Debug($"RegisterPawn: Registering {pawn.LabelShort} in vat {vat.LabelCap}");

            var settings = ActiveSettings;
            VatSentinelLogger.Debug($"RegisterPawn: Settings - EjectAtChild={settings?.EjectAtChild}, EjectAtTeen={settings?.EjectAtTeen}, EjectAtAdult={settings?.EjectAtAdult}, EjectAfterDays={settings?.EjectAfterDays}");

            var record = _trackedPawns.Find(r => r.MatchesPawn(pawn));
            var isNew = record == null;
            if (record == null)
            {
                VatSentinelLogger.Debug($"RegisterPawn: Creating new record for {pawn.LabelShort}");
                record = new VatTrackingRecord();
                _trackedPawns.Add(record);
            }
            else
            {
                VatSentinelLogger.Debug($"RegisterPawn: Found existing record for {pawn.LabelShort}, entryTick={record.EntryTick}");
            }

            record.SetTrackedEntities(pawn, vat);
            var changed = record.UpdateScheduledTarget(settings, _schedule);

            if (isNew)
            {
                VatSentinelLogger.Debug($"Registered pawn {pawn.LabelShort} in vat {vat.LabelCap}, targetAge={record.TargetAgeYears:F4} years, entryTick={record.EntryTick}.");
            }
            else if (changed)
            {
                VatSentinelLogger.Debug($"Updated ejection target for {pawn.LabelShort} in vat {vat.LabelCap} (target age {record.TargetAgeYears:F4} years, entryTick={record.EntryTick}).");
            }
            else
            {
                VatSentinelLogger.Debug($"RegisterPawn: No change for {pawn.LabelShort}, targetAge={record.TargetAgeYears:F4} years, entryTick={record.EntryTick}");
            }
        }

        internal void UnregisterPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (_trackedPawns.RemoveAll(record => record.MatchesPawn(pawn)) > 0)
            {
                VatSentinelLogger.Debug($"Unregistered pawn {pawn.LabelShort} from vat tracking.");
            }
        }

        internal void ClearInvalidEntries()
        {
            if (_trackedPawns.RemoveAll(record => !record.IsValid) > 0)
            {
                VatSentinelLogger.Debug("Pruned invalid vat tracking records.");
            }
        }

        internal void RecalculateScheduleFor(Pawn pawn)
        {
            VatSentinelLogger.Debug($"RecalculateScheduleFor: Recalculating for {pawn?.LabelShort ?? "null"}");
            var settings = ActiveSettings;
            var record = _trackedPawns.Find(r => r.MatchesPawn(pawn));
            if (record != null)
            {
                var changed = record.UpdateScheduledTarget(settings, _schedule);
                VatSentinelLogger.Debug($"RecalculateScheduleFor: {pawn.LabelShort} schedule recalculated, changed={changed}, targetAge={record.TargetAgeYears:F4} years");
            }
            else
            {
                VatSentinelLogger.Debug($"RecalculateScheduleFor: No record found for {pawn?.LabelShort ?? "null"}");
            }
        }

        internal void RecalculateAllSchedules()
        {
            var settings = ActiveSettings;
            foreach (var record in _trackedPawns)
            {
                record?.UpdateScheduledTarget(settings, _schedule);
            }
        }

        internal void ScheduleRetry(Pawn pawn)
        {
            var record = _trackedPawns.Find(r => r.MatchesPawn(pawn));
            if (record == null)
            {
                return;
            }

            record.ScheduleRetry();
            VatSentinelLogger.Debug($"Scheduled retry for {pawn.LabelShort} due to failed ejection.");
        }

        internal VatTrackingRecord GetRecord(Pawn pawn) =>
            _trackedPawns.Find(record => record.MatchesPawn(pawn));

        internal void SyncVatState(Building_GrowthVat vat)
        {
            if (vat == null || !CompVatGrowerReflection.IsAvailable)
            {
                return;
            }

            var occupant = CompVatGrowerReflection.GetPawnBeingGrown(vat);

            if (occupant != null)
            {
                RegisterPawn(occupant, vat);
                return;
            }

            var removed = _trackedPawns.RemoveAll(record =>
                record.MatchesVat(vat) &&
                (!record.HasValidPawn ||
                 record.Pawn == null ||
                 record.Pawn.Spawned ||
                 record.Pawn.DestroyedOrNull()));

            if (removed > 0)
            {
                VatSentinelLogger.Debug($"Removed tracking for empty vat {vat.LabelCap}.");
            }
        }
    }
}

