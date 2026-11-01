using System;
using HarmonyLib;

namespace ConfigurableEmployees
{
    /// <summary>
    /// Grows the packager's routes list before the screen first wires its rows, so the extra rows
    /// get the same buttons as the originals. See RouteListUi.
    /// </summary>
    [HarmonyPatch(typeof(RouteListFieldUI), "Start")]
    internal static class RouteListFieldUI_Start_Patch
    {
        private static void Prefix(RouteListFieldUI __instance)
        {
            try { RouteListUi.Extend(__instance, ModConfig.PackagerMaxRoutes); }
            catch (Exception ex) { ModLog.Warning("Could not extend the routes list: " + ex); }
        }
    }

    /// <summary>
    /// While the management screen is open the mouse wheel must scroll the routes list and
    /// nothing else (it would otherwise also change the held item and close the screen).
    /// </summary>
    [HarmonyPatch(typeof(ManagementInterface), "Open")]
    internal static class ManagementInterface_Open_Patch
    {
        private static void Postfix() => RouteListUi.ScreenOpen = true;
    }

    [HarmonyPatch(typeof(ManagementInterface), "Close")]
    internal static class ManagementInterface_Close_Patch
    {
        private static void Postfix() => RouteListUi.ScreenOpen = false;
    }

    [HarmonyPatch(typeof(GameInput), "MouseScrollDelta", MethodType.Getter)]
    internal static class GameInput_MouseScrollDelta_Patch
    {
        private static void Postfix(ref float __result)
        {
            if (RouteListUi.ScreenOpen && RouteListUi.Extended) __result = 0f;
        }
    }
}
