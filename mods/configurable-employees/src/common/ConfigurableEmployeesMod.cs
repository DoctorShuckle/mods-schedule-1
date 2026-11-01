using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using ConfigurableEmployees;

[assembly: MelonInfo(typeof(ConfigurableEmployeesMod), "Configurable Employees", "1.0.0", "DoctorShuckle")]
[assembly: MelonGame("TVGS", "Schedule I")]
#if IL2CPP
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#endif
// Patch classes are applied one by one (found by their [HarmonyPatch] attribute, see FindPatches),
// so a single missing game method cannot abort the rest.
[assembly: HarmonyDontPatchAll]

// ---------------------------------------------------------------------------------------------
// How the mod works
//
// Every setting is applied at the place the game gets the number, not on a timer afterwards:
//
//   * Wage, signing fee, assigned-things limits and packing speed are fields on the employee
//     prefab. New hires are cloned from it, and the Fixer reads the signing fee from it, so
//     writing the prefab when the game creates its EmployeeManager covers everyone hired later.
//     Employee.Start then covers the employees a save loads, and the configuration limits that
//     the game builds from fixed numbers.
//   * Walk speed is set by the game from NPC data every time an NPC is initialised, which would
//     overwrite an earlier change, so it is applied right after NPC.InitializeNPC.
//   * Botanist task times: the duration methods are constant stubs that cannot be detoured on
//     IL2CPP, so the botanist's work speed is changed from the start/stop hooks around the task.
//     Chemist and packager tasks work the same way, from the base Behaviour lifecycle.
//   * The Fixer's extra fee is a static method; its result is replaced.
//   * A property's employee capacity is set before the property initialises itself.
//
// The mod saves nothing: removing it returns the game to normal on the next launch. The patch
// classes (Patches folder, one file per feature) are applied one by one (see the mod class), so
// one missing game method disables one feature and the rest keep working.
// ---------------------------------------------------------------------------------------------

namespace ConfigurableEmployees
{
    /// <summary>
    /// Entry point. The mod keeps no state of its own besides its settings file: see the overview
    /// above. Folders: Rules (game-free rules, unit-tested), Game (everything that touches the game),
    /// Patches (the Harmony patches, one file per feature) and Config (settings and logging).
    /// </summary>
    public sealed class ConfigurableEmployeesMod : MelonMod
    {
        /// <summary>
        /// The patch classes: every type in this assembly marked [HarmonyPatch], in name order so the
        /// log reads the same each launch. Adding a patch class to the Patches folder is enough; the
        /// startup count in the log ("N/N patches applied") shows if one is missing.
        /// </summary>
        private static List<Type> FindPatches()
        {
            Type[] types;
            try { types = typeof(ConfigurableEmployeesMod).Assembly.GetTypes(); }
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
            int skipped = 0;
            foreach (Type patch in patches)
            {
                if (PatchSkips.IsSkipped(patch.Name, ModConfig.SkipPatches))
                {
                    LoggerInstance.Warning("Patch " + patch.Name + " skipped (SkipPatches).");
                    skipped++;
                    continue;
                }

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

            // Diagnostic: shows whether the task duration stubs share native code (IL2CPP only).
            try { FoldingCheck.Log(); }
            catch (Exception ex) { LoggerInstance.Warning("Could not read the task duration addresses: " + ex.Message); }

            LoggerInstance.Msg("Configurable Employees 1.0.0 (" +
#if IL2CPP
                "IL2CPP" +
#else
                "Mono" +
#endif
                "): " + applied + "/" + (patches.Count - skipped) + " patches applied" +
                (skipped > 0 ? " (" + skipped + " skipped)." : "."));
        }

        // Properties the Awake patch could not see (see PropertyTuning) are handled once the scene is up.
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            try { PropertyTuning.ApplyAll(); }
            catch (Exception ex) { LoggerInstance.Warning("Could not set property capacities: " + ex); }
        }

        public override void OnPreferencesLoaded() => Reload();

        public override void OnPreferencesSaved() => Reload();

        private static void Reload()
        {
            ModConfig.Apply();
            try { EmployeeTuning.ReapplyAll(); }
            catch (Exception ex) { ModLog.Warning("Could not re-apply settings: " + ex); }
        }
    }
}
