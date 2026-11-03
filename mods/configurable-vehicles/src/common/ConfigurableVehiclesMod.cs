using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using ConfigurableVehicles;

[assembly: MelonInfo(typeof(ConfigurableVehiclesMod), "Configurable Vehicles", "1.0.0", "DoctorShuckle")]
[assembly: MelonGame("TVGS", "Schedule I")]
#if IL2CPP
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#endif
// Patch classes are applied one by one below, so a single missing game method cannot abort the rest.
[assembly: HarmonyDontPatchAll]

// ---------------------------------------------------------------------------------------------
// How the mod works
//
// Every car is a LandVehicle whose handling (mass, torque curve, steering, brakes, downforce) is a set
// of fields filled in from its prefab, and whose wheels are Unity WheelColliders. As a car starts, the
// mod writes the values the player set for that car's code (VehicleTuner), computed from the game's
// own values, which it remembers, so applying again never compounds. A trunk is a StorageEntity that
// builds its item slots in Awake from SlotCount, so just before that the mod sets SlotCount and the
// number of rows the trunk screen uses. Saving the config re-tunes the cars already in the world. All of
// it runs from the game's own calls and the preferences events: nothing runs per frame, nothing is saved.
//
// Not changed: the body's shape and centre of mass, gearing, suspension and the handbrake.
// ---------------------------------------------------------------------------------------------

namespace ConfigurableVehicles
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class ConfigurableVehiclesMod : MelonMod
    {
        /// <summary>
        /// The patch classes: every type in this assembly marked [HarmonyPatch], in name order so the
        /// log reads the same each launch. Adding a patch class to the Patches folder is enough; the
        /// startup count in the log ("N/N patches applied") shows if one is missing.
        /// </summary>
        private static List<Type> FindPatches()
        {
            Type[] types;
            try { types = typeof(ConfigurableVehiclesMod).Assembly.GetTypes(); }
            // A type that cannot load (a game class that has gone) must not hide the rest.
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }

            var patches = new List<Type>();
            foreach (Type type in types)
                if (type != null && type.IsDefined(typeof(HarmonyPatch), false)) patches.Add(type);
            patches.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return patches;
        }

        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            List<Type> patches = FindPatches();
            if (patches.Count == 0) LoggerInstance.Error("No patches found: the mod will do nothing.");

            int applied = 0;
            foreach (Type patch in patches)
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(patch).Patch();
                    applied++;
                }
                catch (Exception ex)
                {
                    // Surface this loudly: a failed patch means that feature is off.
                    LoggerInstance.Error("Patch " + patch.Name + " failed: " + ex.Message);
                }
            }

            LoggerInstance.Msg("Configurable Vehicles 1.0.0 (" +
#if IL2CPP
                "IL2CPP" +
#else
                "Mono" +
#endif
                "): " + applied + "/" + patches.Count + " patches applied.");
        }

        // Pick up edits made in the preferences file while the game is running.
        // Vehicles already in the world are re-tuned too. (Trunk sizes only apply when a vehicle spawns.)
        public override void OnPreferencesLoaded()
        {
            ModConfig.Apply();
            VehicleTuner.ReapplyAll();
        }

        public override void OnPreferencesSaved()
        {
            ModConfig.Apply();
            VehicleTuner.ReapplyAll();
        }
    }
}
