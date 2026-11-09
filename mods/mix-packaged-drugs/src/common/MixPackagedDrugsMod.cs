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
