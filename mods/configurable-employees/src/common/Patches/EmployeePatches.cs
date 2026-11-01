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
