using System;
using HarmonyLib;

namespace ConfigurableStorage
{
    // The prefabs are resized when a game starts (host, client or tutorial), before any container is built or
    // loaded, and again whenever the settings change (ModConfig.Apply). Nothing runs per frame.

    /// <summary>Hosting or loading a save: resizes the prefabs before saved containers are built.</summary>
    [HarmonyPatch(typeof(LoadManager), "StartGame")]
    internal static class LoadManager_StartGame_Patch
    {
        private static void Prefix() => StoragePatchHelper.Apply();
    }

    /// <summary>Joining a game: the client builds containers from the same prefabs.</summary>
    [HarmonyPatch(typeof(LoadManager), "LoadAsClient")]
    internal static class LoadManager_LoadAsClient_Patch
    {
        private static void Prefix() => StoragePatchHelper.Apply();
    }

    /// <summary>The tutorial as a client.</summary>
    [HarmonyPatch(typeof(LoadManager), "LoadTutorialAsClient")]
    internal static class LoadManager_LoadTutorialAsClient_Patch
    {
        private static void Prefix() => StoragePatchHelper.Apply();
    }

    internal static class StoragePatchHelper
    {
        public static void Apply()
        {
            try
            {
                GameAccess.ApplyAll();
            }
            catch (Exception ex)
            {
                // Fail open: the game builds its containers at their own size.
                ModLog.WarnOnce("Could not resize the storage containers: " + ex);
            }
        }
    }
}
