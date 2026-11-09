using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using ConfigurableAtm;

[assembly: MelonInfo(typeof(ConfigurableAtmMod), "Configurable ATM", "1.0.0", "DoctorShuckle")]
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
// The game's weekly deposit limit is a const the compiler wrote into each method that uses it, so no
// setting can change it. On Mono the mod rewrites those methods' IL, swapping the literal for the
// configured limit (MonoLimitPatches). On IL2CPP the IL is gone, so the mod replaces the small
// methods that compute with the limit and redraws the parts of the screen that show it
// (Il2CppLimitPatches). Both builds also set the ATM's six amount buttons as the screen starts
// (AtmPatches). The Clean Cash quest, which starts when the weekly total reaches the game's limit,
// starts at the smaller of the configured and the game's limit. Nothing is saved.
// ---------------------------------------------------------------------------------------------

namespace ConfigurableAtm
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class ConfigurableAtmMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.PresetsChanged += OnPresetsChanged;
            ModConfig.Apply();

            PatchLoader.ApplyAll(this);
        }

        // The game's ATM screens are already up when the buttons setting changes: relabel them now.
        private static void OnPresetsChanged(int[] presets)
        {
            try { GameAccess.RefreshPresets(presets); }
            catch (Exception ex) { ModLog.Warning("Could not relabel the ATM's amount buttons: " + ex); }
        }

        // Pick up settings changed while the game is running (they arrive as preferences being saved).
        public override void OnPreferencesLoaded() => ModConfig.Apply();

        public override void OnPreferencesSaved() => ModConfig.Apply();
    }
}
