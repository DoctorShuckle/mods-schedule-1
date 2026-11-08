using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using MixPackagedDrugs;

[assembly: MelonInfo(typeof(MixPackagedDrugsMod), "Mix Packaged Drugs", "1.0.0", "DoctorShuckle")]
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
// A mixing station refuses packaged product (baggie, jar, brick), and its mixing code counts items, not
// units. The mod lets a packaged product into the station's product slot (PackagedProductPatches) and
// swaps it, before the game stores or stacks it, for the same product unpackaged: quantity times the
// package's units, so a brick is 20 units and needs 20 mixers. After that the game's own mixing runs
// unchanged, and the output is loose. The packaging is used up. Nothing is saved by the mod itself.
// ---------------------------------------------------------------------------------------------

namespace MixPackagedDrugs
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class MixPackagedDrugsMod : MelonMod
    {
        /// <summary>
        /// The patch classes: every type in this assembly marked [HarmonyPatch], in name order so the
        /// log reads the same each launch. Adding a patch class to the Patches folder is enough; the
        /// startup count in the log ("N/N patches applied") shows if one is missing.
        /// </summary>
        private static List<Type> FindPatches()
        {
            Type[] types;
            try { types = typeof(MixPackagedDrugsMod).Assembly.GetTypes(); }
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

            LoggerInstance.Msg("Mix Packaged Drugs 1.0.0 (" +
#if IL2CPP
                "IL2CPP" +
#else
                "Mono" +
#endif
                "): " + applied + "/" + patches.Count + " patches applied.");
        }

        // Pick up edits made in the preferences file while the game is running.
        public override void OnPreferencesLoaded() => ModConfig.Apply();

        public override void OnPreferencesSaved() => ModConfig.Apply();
    }
}
