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
