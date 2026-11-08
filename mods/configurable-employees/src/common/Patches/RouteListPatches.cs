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
    /// Keeps the routes list scrolled to whatever a controller has selected. The game's panels scroll
    /// only a scroll area of their own, so the list made by RouteListUi has to follow the selection
    /// itself. Every selection in the game comes through here; RouteListUi ignores all but its rows.
    /// The name starts with RouteListFieldUI so that skipping that one patch name turns off the whole
    /// feature.
    /// </summary>
    [HarmonyPatch(typeof(UISelectable), "OnSelect")]
    internal static class RouteListFieldUI_ScrollToSelection_Patch
    {
        private static void Postfix(UISelectable __instance)
        {
            try { RouteListUi.ScrollIntoView(__instance); }
            catch (Exception ex) { ModLog.Warning("Could not scroll the routes list to the selection: " + ex.Message); }
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
