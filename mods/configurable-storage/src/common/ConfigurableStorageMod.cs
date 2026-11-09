using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using ConfigurableStorage;

[assembly: MelonInfo(typeof(ConfigurableStorageMod), "Configurable Storage", "1.0.0", "DoctorShuckle")]
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
// Every player-placed container (the storage racks, the storage closets and the safe) is a
// PlaceableStorageEntity built by instantiating the prefab of its item. The game creates the
// container's slots from StorageEntity.SlotCount while that prefab is instantiated, and lays the
// storage screen out in DisplayRowCount rows. The mod finds the prefab of each configured item ID
// in the item registry and sets those two numbers on it when a game starts and when the settings
// change, so every container built or loaded afterwards has the size you chose. No game code is
// replaced, nothing is saved, and nothing runs per frame.
// ---------------------------------------------------------------------------------------------

namespace ConfigurableStorage
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings and the game's original container sizes: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class ConfigurableStorageMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            PatchLoader.ApplyAll(this);
        }

        // Pick up edits made in the preferences file while the game is running.
        public override void OnPreferencesLoaded() => ModConfig.Apply();

        public override void OnPreferencesSaved() => ModConfig.Apply();
    }
}
