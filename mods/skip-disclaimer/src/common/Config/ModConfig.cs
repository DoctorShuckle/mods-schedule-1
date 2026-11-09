using MelonLoader;

namespace SkipDisclaimer
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [SkipDisclaimer].
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _skip;
        private static MelonPreferences_Entry<bool> _verbose;

        public static bool Skip { get; private set; } = true;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("SkipDisclaimer");

            _skip = category.CreateEntry("SkipDisclaimer", true, "Skip the disclaimer",
                "true skips the disclaimer screen shown when the game starts. false leaves the game as it is.");
            _verbose = Prefs.CreateVerbose(category, "Log each time the mod skips the disclaimer. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_skip == null) return;

            Skip = _skip.Value;
            Prefs.ApplyVerbose(_verbose);
        }
    }
}
