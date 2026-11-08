using System;
using HarmonyLib;

namespace ConfigurableEmployees
{
    [HarmonyPatch(typeof(EmployeeManager), "Awake")]
    internal static class EmployeeManager_Awake_Patch
    {
        private static void Postfix(EmployeeManager __instance)
        {
            try { EmployeeTuning.ApplyToPrefabs(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not tune the employee prefabs: " + ex); }
        }
    }

    [HarmonyPatch(typeof(Employee), "Start")]
    internal static class Employee_Start_Patch
    {
        private static void Postfix(Employee __instance)
        {
            try { EmployeeTuning.ApplyToEmployee(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not tune an employee: " + ex); }
        }
    }

    // The configuration that holds an employee's limits (stations, bins, routes) is built inside each
    // employee class's AssignProperty, after it calls the base one, so the limits are written right
    // after it. Employee.Start can run earlier, when there is no configuration to write to yet.
    // Each class overrides the method, so each needs a patch of its own.

    [HarmonyPatch(typeof(Botanist), "AssignProperty")]
    internal static class Botanist_AssignProperty_Patch
    {
        private static void Postfix(Botanist __instance) => AssignedProperty.Apply(__instance);
    }

    [HarmonyPatch(typeof(Chemist), "AssignProperty")]
    internal static class Chemist_AssignProperty_Patch
    {
        private static void Postfix(Chemist __instance) => AssignedProperty.Apply(__instance);
    }

    [HarmonyPatch(typeof(Packager), "AssignProperty")]
    internal static class Packager_AssignProperty_Patch
    {
        private static void Postfix(Packager __instance) => AssignedProperty.Apply(__instance);
    }

    [HarmonyPatch(typeof(Cleaner), "AssignProperty")]
    internal static class Cleaner_AssignProperty_Patch
    {
        private static void Postfix(Cleaner __instance) => AssignedProperty.Apply(__instance);
    }

    /// <summary>What the four AssignProperty patches above do, kept in one place.</summary>
    internal static class AssignedProperty
    {
        public static void Apply(Employee employee)
        {
            try { EmployeeTuning.ApplyConfigurationLimits(employee); }
            catch (Exception ex) { ModLog.Warning("Could not set an employee's limits: " + ex); }
        }
    }

    /// <summary>
    /// Every NPC passes through here; only employees are touched. Employee does not override the
    /// method, so this is the one that sets their walk speed from the prefab's NPC data.
    /// </summary>
    [HarmonyPatch(typeof(NPC), "InitializeNPC")]
    internal static class NPC_InitializeNPC_Patch
    {
        private static void Postfix(NPC __instance)
        {
            try
            {
                Employee employee = GameAccess.AsEmployee(__instance);
                if (employee != null) EmployeeTuning.ApplyWalkSpeed(employee, GameAccess.KindOf(employee));
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not set an employee's walk speed: " + ex);
            }
        }
    }
}
