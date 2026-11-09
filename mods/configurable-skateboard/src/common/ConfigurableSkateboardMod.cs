using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using ConfigurableSkateboard;

[assembly: MelonInfo(typeof(ConfigurableSkateboardMod), "Configurable Skateboard", "1.0.0", "DoctorShuckle")]
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
// A board's physics read one settings object (SkateboardSettings) on every step. The game builds
// it from the board's default data, and rebuilds it on every weather change to blend in the rain.
// The mod multiplies some of its values (top speed, push force, jump force, sideways grip and the
// coasting drag curve) right after each build (SkateboardPatches, BoardTuner), and can switch off the
// board's terrain slowdown. A push spends stamina through PlayerMovement.ChangeStamina, so while
// riding the mod scales any spend there. No game code is replaced and nothing is saved.
//
// Not reachable without rewriting game code: the stamina needed to start a push (12.5), the
// jump charge time and the mount and dismount momentum. They are constants the compiler inlined.
// ---------------------------------------------------------------------------------------------

namespace ConfigurableSkateboard
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class ConfigurableSkateboardMod : MelonMod
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
