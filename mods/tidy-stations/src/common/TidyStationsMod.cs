using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using TidyStations;

[assembly: MelonInfo(typeof(TidyStationsMod), "Tidy Stations", "1.0.0", "DoctorShuckle")]
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
// Trash made by work is created in a few specific places, and the mod stops each one when its
// setting (ModConfig, "No...Trash", default true) asks for it. It never touches TrashManager.CreateTrashItem, the shared funnel that the
// map's own trash spawning (TrashGenerator and the spawn volumes) also uses, so the trash that
// bootstraps a new player is left alone.
//
//   * Mixing station, chemistry station, cauldron: the player's tasks call a CreateTrash method
//     after using the ingredients; that method is skipped (StationPatches).
//   * Botanists: after pouring soil, applying an additive or sowing a seed, the game asks the
//     behaviour what to drop (GetTrashPrefab); the answer becomes "nothing" (BotanistPatches).
//   * Plants: the scrap left by the last harvest of a plant is dropped (PlantPatches).
//
// Nothing is saved, so removing the mod returns the game to normal.
// ---------------------------------------------------------------------------------------------

namespace TidyStations
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings: see the overview above.
    /// Folders: Patches (the Harmony patches, one file per source of trash) and Config (settings and logging).
    /// </summary>
    public sealed class TidyStationsMod : MelonMod
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
