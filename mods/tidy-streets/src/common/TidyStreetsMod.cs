using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using TidyStreets;

[assembly: MelonInfo(typeof(TidyStreetsMod), "Tidy Streets", "1.0.0", "DoctorShuckle")]
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
// The map's trash comes from three spawners (TrashGenerator and TrashSpawnVolume, which run when
// the player sleeps and wakes, and TrashGenerationVolume.Generate). Each is skipped by name unless
// its setting is turned off (StreetPatches). Trash already in the world is removed once each time a
// save finishes loading, by calling what the game's own "cleartrash" console command calls
// (TrashClearing). TrashManager.CreateTrashItem is never patched, because the stations,
// botanists and the save loader create trash through it as well.
//
// Nothing is saved, so removing the mod returns the game to normal.
// ---------------------------------------------------------------------------------------------

namespace TidyStreets
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings: see the overview above.
    /// Folders: Game (the load hook), Patches (the Harmony patches) and Config (settings and logging).
    /// </summary>
    public sealed class TidyStreetsMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            PatchLoader.ApplyAll(this);
        }

        // The load manager outlives scenes but may not exist yet at startup, so try on each scene.
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            try { TrashClearing.Hook(); }
            catch (Exception ex) { LoggerInstance.Warning("Could not listen for the end of a save load: " + ex); }
        }

        // Pick up edits made in the preferences file while the game is running.
        public override void OnPreferencesLoaded() => ModConfig.Apply();

        public override void OnPreferencesSaved() => ModConfig.Apply();
    }
}
