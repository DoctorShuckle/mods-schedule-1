using System;
using UnityEngine.Events;

namespace TidyStreets
{
    /// <summary>
    /// Removes all trash once a save has finished loading, by running what the game's own
    /// "cleartrash" console command runs (TrashManager.DestroyAllTrash). LoadManager outlives the
    /// scenes and raises onLoadComplete after the trash has been loaded, every time a save loads,
    /// so one listener covers every load. DestroyAllTrash does nothing on a client.
    /// </summary>
    internal static class TrashClearing
    {
        private static object _hookedOn;

        /// <summary>Adds the listener if the load manager exists and is not already hooked.</summary>
        public static void Hook()
        {
            if (!LoadManager.InstanceExists) return;

            LoadManager manager = LoadManager.Instance;
            if (ReferenceEquals(_hookedOn, manager)) return;

            manager.onLoadComplete.AddListener(
#if IL2CPP
                new Action(OnLoadComplete));
#else
                new UnityAction(OnLoadComplete));
#endif
            _hookedOn = manager;
            ModLog.Verbose("Listening for the end of a save load.");
        }

        private static void OnLoadComplete()
        {
            try
            {
                if (!ModConfig.ClearTrashOnLoad) return;
                if (!NetworkSingleton<TrashManager>.InstanceExists) return;

                NetworkSingleton<TrashManager>.Instance.DestroyAllTrash();
                ModLog.Info("Save loaded: cleared the world's trash.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not clear the trash after loading: " + ex);
            }
        }
    }
}
