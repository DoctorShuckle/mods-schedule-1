using MelonLoader;

namespace TidyStreets
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [TidyStreets]. Each
    /// setting is named for what it does and defaults to true: the mod keeps the streets clean
    /// until the player sets one to false.
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _generator;
        private static MelonPreferences_Entry<bool> _spawnVolume;
        private static MelonPreferences_Entry<bool> _generationVolume;
        private static MelonPreferences_Entry<bool> _clearOnLoad;
        private static MelonPreferences_Entry<bool> _verbose;

        public static bool NoGeneratorTrash { get; private set; } = true;
        public static bool NoSpawnVolumeTrash { get; private set; } = true;
        public static bool NoGenerationVolumeTrash { get; private set; } = true;
        public static bool ClearTrashOnLoad { get; private set; } = true;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("TidyStreets");

            _generator = category.CreateEntry("NoGeneratorTrash", true, "No generator trash",
                "true stops the trash that fills the map's areas while you sleep; false leaves it as in the game.");
            _spawnVolume = category.CreateEntry("NoSpawnVolumeTrash", true, "No spawn volume trash",
                "true stops the trash that appears in set places when you wake up; false leaves it as in the game.");
            _generationVolume = category.CreateEntry("NoGenerationVolumeTrash", true, "No generation volume trash",
                "true stops trash from the game's other generation volumes; false leaves it as in the game.");
            _clearOnLoad = category.CreateEntry("ClearTrashOnLoad", true, "Clear trash on load",
                "true removes all trash in the world each time a save finishes loading (the same as the cleartrash console command), including trash you left on the ground. Only the host clears.");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each spawn the mod stops and each clear. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_generator == null) return;

            NoGeneratorTrash = _generator.Value;
            NoSpawnVolumeTrash = _spawnVolume.Value;
            NoGenerationVolumeTrash = _generationVolume.Value;
            ClearTrashOnLoad = _clearOnLoad.Value;
            ModLog.VerboseEnabled = _verbose.Value;
        }
    }
}
