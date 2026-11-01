using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using PersistentPotAdditives;

[assembly: MelonInfo(typeof(PersistentPotAdditivesMod), "Persistent Pot Additives", "3.0.0", "DoctorShuckle")]
[assembly: MelonGame("TVGS", "Schedule I")]
#if IL2CPP
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#endif
// Patch classes are applied one by one below, so a single missing game method cannot abort the rest.
[assembly: HarmonyDontPatchAll]

// ---------------------------------------------------------------------------------------------
// How the mod works, and why it needs no save data
//
// The game wipes a pot's additives at the end of every harvest (Pot.OnPlantFullyHarvested ->
// ClearAdditives), even when the soil still has uses left. This mod makes additives last for the
// soil's whole lifetime instead. It does so with two small patches and stores NOTHING:
//
//   1. Harvest patch.  Just before the game clears the pot we copy the pot's additive ids into a
//      local variable; just after, if the soil still has uses, we put them back. That copy lives
//      only for the duration of that one method call, so there is nothing to save or load.
//      The game already saves the pot's additives (PotData.AppliedAdditives) and the soil's
//      remaining uses, so they survive save/load, and the game's own clear-on-last-use
//      (ClearSoil) still removes the additives when the soil is spent.
//
//   2. Replant patch.  When the next seed goes in, the game already hands the pot's additives
//      to the new plant, which restores Fertilizer (quality) and PGR (yield). It deliberately
//      leaves out Speed Grow's one-shot growth boost, which only applies when an additive is
//      first added. We add that boost ourselves so Speed Grow works on every plant.
//
// Why "restore after" rather than "block ClearAdditives"? Blocking would mean patching a call
// that happens inside native game code on IL2CPP, which can be inlined and then silently never
// fire. Restoring afterwards depends only on two ordinary public methods. As a bonus, the
// restore goes through the game's networked ApplyAdditive_Server, so clients get the additives
// back even if they do not have this mod installed.
// ---------------------------------------------------------------------------------------------

namespace PersistentPotAdditives
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own and writes no files: see the overview above.
    /// Folders: Rules (game-free rules, unit-tested), Game (everything that touches the game),
    /// Patches (the Harmony patches, one file per feature) and Config (settings and logging).
    /// </summary>
    public sealed class PersistentPotAdditivesMod : MelonMod
    {
        /// <summary>
        /// The patch classes: every type in this assembly marked [HarmonyPatch], in name order so the
        /// log reads the same each launch. Adding a patch class to the Patches folder is enough; the
        /// startup count in the log ("N/N patches applied") shows if one is missing.
        /// </summary>
        private static List<Type> FindPatches()
        {
            Type[] types;
            try { types = typeof(PersistentPotAdditivesMod).Assembly.GetTypes(); }
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
                    // Surface this loudly: a failed patch means that half of the feature is off.
                    LoggerInstance.Error("Patch " + patch.Name + " failed: " + ex.Message);
                }
            }

            LoggerInstance.Msg("Persistent Pot Additives 3.0.0 (" +
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
