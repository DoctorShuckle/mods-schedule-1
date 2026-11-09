using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using BrickMixer;

[assembly: MelonInfo(typeof(BrickMixerMod), "Brick Mixer", "1.0.0", "DoctorShuckle")]
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
// The mod adds a buildable, the Brick Mixer, by copying the game's Mixing Station Mk2: a copy of its item
// (registered with the game's item registry each time a game starts, and listed in the shop that sells the
// Mk2) and a copy of its prefab with a 20-unit batch and no hands-on step. The copy is still a Mk2 to the
// game, so Chemists and the config panel work. The station is told apart by its item ID, and a few patches
// give only that station its rules: a mix starts only with 20 units of product and 20 mixers, and the output
// is packed as a brick. What goes into the product slot is the game's own filter; Mix Packaged Drugs, when
// installed, opens it to packages on every mixing station, this one included. The mod keeps no files: the
// game saves the station like any other.
// ---------------------------------------------------------------------------------------------

namespace BrickMixer
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings and the item it adds: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class BrickMixerMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            PatchLoader.ApplyAll(this);
        }

        // A changed price applies to the item the next time it is built; the item is only built once per run.
        // Pick up edits made in the preferences file while the game is running.
        public override void OnPreferencesLoaded() { ModConfig.Apply(); GameAccess.RefreshPrice(); }

        public override void OnPreferencesSaved() { ModConfig.Apply(); GameAccess.RefreshPrice(); }
    }
}
