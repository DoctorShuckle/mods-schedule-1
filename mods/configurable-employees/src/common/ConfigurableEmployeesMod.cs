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
//     The limits the game builds from fixed numbers (chemist, cleaner, routes) live on the
//     employee's configuration, which each employee class builds inside its own AssignProperty,
//     so they are written right after that. Employee.Start covers the rest for employees a
//     save loads.
//   * Walk speed is set by the game from NPC data every time an NPC is initialised, which would
//     overwrite an earlier change, so it is applied right after NPC.InitializeNPC.
//   * Botanist task times: the duration methods are constant stubs that cannot be detoured on
//     IL2CPP, so the botanist's work speed is changed from the start/stop hooks around the task.
//     Chemist and packager tasks work the same way, from the base Behaviour lifecycle.
//   * The Fixer's extra fee is a static method; its result is replaced.
//   * A property's employee capacity is set before the property initialises itself, and again
//     once the scene has loaded, because the special properties can skip the first hook.
//   * The packager's routes screen gets extra rows cloned from the last one. For a controller
//     the rows' buttons are added to the game's gamepad panel, and the list scrolls to follow
//     the selection (controller only). A packager's route limit is held to the rows the screen
//     has, since it cannot show a route it has no row for.
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
        public override void OnInitializeMelon()
        {
            ModLog.Init(LoggerInstance);
            ModConfig.Init();
            ModConfig.Apply();

            PatchLoader.ApplyAll(this, name => PatchSkips.IsSkipped(name, ModConfig.SkipPatches));

            // Diagnostic: shows whether the task duration stubs share native code (IL2CPP only).
            try { FoldingCheck.Log(); }
            catch (Exception ex) { LoggerInstance.Warning("Could not read the task duration addresses: " + ex.Message); }
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
