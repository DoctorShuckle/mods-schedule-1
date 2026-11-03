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
        /// <summary>
        /// The patch classes: every type in this assembly marked [HarmonyPatch], in name order so the
        /// log reads the same each launch. Adding a patch class to the Patches folder is enough.
        /// </summary>
        private static List<Type> FindPatches()
        {
            Type[] types;
            try { types = typeof(SkipDisclaimerMod).Assembly.GetTypes(); }
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

            LoggerInstance.Msg("Skip Disclaimer 1.0.0 (" +
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
