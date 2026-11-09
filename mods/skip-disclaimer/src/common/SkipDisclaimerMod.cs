using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using SkipDisclaimer;

[assembly: MelonInfo(typeof(SkipDisclaimerMod), "Skip Disclaimer", "1.0.0", "DoctorShuckle")]
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
// When the menu scene loads, the game's Disclaimer component checks a static "Shown" flag. If it is
// set, the component closes itself at once and lets the menu continue; if not, it fades in a
// disclaimer and waits a few seconds. A prefix on Disclaimer.Awake sets the flag, so the game's own
// skip path runs. Nothing is replaced or saved, and the mod waits for the event (Awake) rather than
// checking anything every frame.
// ---------------------------------------------------------------------------------------------

namespace SkipDisclaimer
{
    /// <summary>
    /// Entry point. The mod keeps no state besides its settings. Folders: Patches (the Harmony
    /// patch) and Config (settings and logging).
    /// </summary>
    public sealed class SkipDisclaimerMod : MelonMod
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
