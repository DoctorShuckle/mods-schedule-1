using MelonLoader;

namespace MixPackagedDrugs
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [MixPackagedDrugs].
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _enabled;
        private static MelonPreferences_Entry<bool> _verbose;

        public static bool Enabled { get; private set; } = true;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("MixPackagedDrugs");

            _enabled = category.CreateEntry("Enabled", true, "Enabled",
                "true lets baggies, jars and bricks into a mixing station's product slot, where they are unpacked. " +
                "false turns the whole mod off.");
            _verbose = Prefs.CreateVerbose(category, "Log each package the mod lets in or unpacks. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_enabled == null) return;

            Enabled = _enabled.Value;
            Prefs.ApplyVerbose(_verbose);
        }
    }
}
