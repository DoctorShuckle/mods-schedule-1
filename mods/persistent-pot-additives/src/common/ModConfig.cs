using MelonLoader;

namespace PersistentPotAdditives
{
    /// <summary>
    /// The mod's only setting. Behaviour itself is not configurable: additives always persist for
    /// the soil's lifetime, and Speed Grow's growth boost always repeats on every replant.
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _verbose;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("PersistentPotAdditives");

            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log every harvest-restore and replant-boost decision. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_verbose == null) return;

            ModLog.VerboseEnabled = _verbose.Value;
        }
    }
}
