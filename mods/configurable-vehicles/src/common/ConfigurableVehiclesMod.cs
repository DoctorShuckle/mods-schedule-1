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
        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            PatchLoader.ApplyAll(this);
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
