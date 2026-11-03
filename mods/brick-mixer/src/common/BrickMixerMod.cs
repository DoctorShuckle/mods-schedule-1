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
// give only that station its rules: its product slot takes bricks and nothing else, a brick is unpacked as it
// goes in (20 units, so 20 mixers), a mix starts only with a full brick and 20 mixers, and the output
// is packed as a brick again. The mod keeps no files: the game saves the station like any other.
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
        /// <summary>
        /// The patch classes: every type in this assembly marked [HarmonyPatch], in name order so the
        /// log reads the same each launch. Adding a patch class to the Patches folder is enough; the
        /// startup count in the log ("N/N patches applied") shows if one is missing.
        /// </summary>
        private static List<Type> FindPatches()
        {
            Type[] types;
            try { types = typeof(BrickMixerMod).Assembly.GetTypes(); }
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

            LoggerInstance.Msg("Brick Mixer 1.0.0 (" +
#if IL2CPP
                "IL2CPP" +
#else
                "Mono" +
#endif
                "): " + applied + "/" + patches.Count + " patches applied.");
        }

        // A changed price applies to the item the next time it is built; the item is only built once per run.
        // Pick up edits made in the preferences file while the game is running.
        public override void OnPreferencesLoaded() { ModConfig.Apply(); GameAccess.RefreshPrice(); }

        public override void OnPreferencesSaved() { ModConfig.Apply(); GameAccess.RefreshPrice(); }
    }
}
