using System;
using HarmonyLib;

namespace ConfigurableEmployees
{
    /// <summary>
    /// Botanist task times. A task's duration comes from GetActionDuration, a stub that returns a
    /// constant and cannot be detoured safely on IL2CPP (see GameAccess.LogDurationAddresses). The
    /// game calls OnStartPerformAction just before it reads that duration and divides it by the
    /// botanist's work speed, so this postfix sets the work speed for the task and the stop hook
    /// below puts it back. The method is overridden by SowSeedInPotBehaviour, which calls base, so
    /// patching the base covers every task. The patch names contain "TaskDuration" so that
    /// SkipPatches = "TaskDuration" switches both off.
    /// </summary>
    [HarmonyPatch(typeof(Behaviours.GrowContainerBehaviour), "OnStartPerformAction")]
    internal static class Botanist_TaskDuration_Start_Patch
    {
        private static void Postfix(Behaviours.GrowContainerBehaviour __instance)
        {
            try { TaskHold.BeginTask(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not set a botanist's task time: " + ex); }
        }
    }

    /// <summary>Ends the work speed change above. The game also calls this when it cancels a task.</summary>
    [HarmonyPatch(typeof(Behaviours.GrowContainerBehaviour), "OnStopPerformAction")]
    internal static class Botanist_TaskDuration_Stop_Patch
    {
        private static void Postfix(Behaviours.GrowContainerBehaviour __instance)
        {
            try { TaskHold.EndTask(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not restore a botanist's work speed: " + ex); }
        }
    }

    /// <summary>
    /// Chemist and packager task times. These behaviours have no start and stop hook of their own,
    /// so the mod holds a work speed multiplier while one is active, from the base Behaviour class's
    /// lifecycle. Activate and Resume begin the hold, Deactivate and Pause end it (a pause is not an
    /// end, but the multiplier must not outlive the task; Resume starts it again). Every behaviour
    /// override calls base, so patching the base covers them all, and the work is a quick type check
    /// for anything that is not a known task. The patch names contain "TaskDuration" so
    /// SkipPatches = "TaskDuration" switches these off too ("Worker_TaskDuration" only these).
    /// </summary>
    [HarmonyPatch(typeof(Behaviours.Behaviour), "Activate")]
    internal static class Worker_TaskDuration_Activate_Patch
    {
        private static void Postfix(Behaviours.Behaviour __instance)
        {
            try { TaskHold.BeginBehaviour(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not set a worker's task time: " + ex); }
        }
    }

    [HarmonyPatch(typeof(Behaviours.Behaviour), "Resume")]
    internal static class Worker_TaskDuration_Resume_Patch
    {
        private static void Postfix(Behaviours.Behaviour __instance)
        {
            try { TaskHold.BeginBehaviour(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not set a worker's task time: " + ex); }
        }
    }

    [HarmonyPatch(typeof(Behaviours.Behaviour), "Deactivate")]
    internal static class Worker_TaskDuration_Deactivate_Patch
    {
        private static void Postfix(Behaviours.Behaviour __instance)
        {
            try { TaskHold.EndBehaviour(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not restore a worker's work speed: " + ex); }
        }
    }

    [HarmonyPatch(typeof(Behaviours.Behaviour), "Pause")]
    internal static class Worker_TaskDuration_Pause_Patch
    {
        private static void Postfix(Behaviours.Behaviour __instance)
        {
            try { TaskHold.EndBehaviour(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not restore a worker's work speed: " + ex); }
        }
    }
}
