using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using WarehouseAccess;

[assembly: MelonInfo(typeof(WarehouseAccessMod), "Warehouse Access", "1.0.0", "DoctorShuckle")]
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
// The game keeps the warehouse shut by hours, by a police chase, by a "Knock" prompt at the door and
// by Igor standing guard at it. The mod asks one question (WarehouseGate): has the warehouse quest
// been completed, and is the mod set to always open or does the player have the rank it asks for?
// When the answer is yes, four small patches (WarehousePatches) make the game's own checks agree.
// No game code is replaced, nothing is saved, and DarkMarket.Unlocked is left alone because setting
// it would also save the unlock and run the game's unlock events.
// ---------------------------------------------------------------------------------------------

namespace WarehouseAccess
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings and the quest it found: see the overview above.
    /// Folders: Game (reads and writes of game state), Patches (the Harmony patches), Rules (the
    /// game-free decisions) and Config (settings and logging).
    /// </summary>
    public sealed class WarehouseAccessMod : MelonMod
    {
        /// <summary>
        /// The patch classes: every type in this assembly marked [HarmonyPatch], in name order so the
        /// log reads the same each launch. Adding a patch class to the Patches folder is enough; the
        /// startup count in the log ("N/N patches applied") shows if one is missing.
        /// </summary>
        private static List<Type> FindPatches()
        {
            Type[] types;
            try { types = typeof(WarehouseAccessMod).Assembly.GetTypes(); }
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

            LoggerInstance.Msg("Warehouse Access 1.0.0 (" +
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
