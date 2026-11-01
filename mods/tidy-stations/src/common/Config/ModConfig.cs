using MelonLoader;

namespace TidyStations
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [TidyStations]. Each
    /// trash setting is named for what it does and defaults to true: the mod removes that trash
    /// until the player sets it to false.
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<bool> _mixingStation;
        private static MelonPreferences_Entry<bool> _chemistryStation;
        private static MelonPreferences_Entry<bool> _cauldron;
        private static MelonPreferences_Entry<bool> _botanistSoil;
        private static MelonPreferences_Entry<bool> _botanistAdditive;
        private static MelonPreferences_Entry<bool> _botanistSeed;
        private static MelonPreferences_Entry<bool> _plantScrap;
        private static MelonPreferences_Entry<bool> _verbose;

        public static bool NoMixingStationTrash { get; private set; }
        public static bool NoChemistryStationTrash { get; private set; }
        public static bool NoCauldronTrash { get; private set; }
        public static bool NoBotanistSoilTrash { get; private set; }
        public static bool NoBotanistAdditiveTrash { get; private set; }
        public static bool NoBotanistSeedTrash { get; private set; }
        public static bool NoPlantScrapTrash { get; private set; }

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("TidyStations");

            _mixingStation = Trash(category, "NoMixingStationTrash", "No mixing station trash", "the packaging left by the mixing station");
            _chemistryStation = Trash(category, "NoChemistryStationTrash", "No chemistry station trash", "the packaging left by the chemistry station");
            _cauldron = Trash(category, "NoCauldronTrash", "No cauldron trash", "the packaging left by the cauldron");
            _botanistSoil = Trash(category, "NoBotanistSoilTrash", "No botanist soil trash", "the empty soil bag a botanist drops");
            _botanistAdditive = Trash(category, "NoBotanistAdditiveTrash", "No botanist additive trash", "the empty additive container a botanist drops");
            _botanistSeed = Trash(category, "NoBotanistSeedTrash", "No botanist seed trash", "the empty seed packet a botanist drops");
            _plantScrap = Trash(category, "NoPlantScrapTrash", "No plant scrap", "the scrap left when a plant's last harvest is taken");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each piece of trash the mod stops. Turn on when reporting a problem.");
        }

        private static MelonPreferences_Entry<bool> Trash(MelonPreferences_Category category, string key, string name, string what)
            => category.CreateEntry(key, true, name, "true removes " + what + "; false leaves it as in the game.");

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_mixingStation == null) return;

            NoMixingStationTrash = _mixingStation.Value;
            NoChemistryStationTrash = _chemistryStation.Value;
            NoCauldronTrash = _cauldron.Value;
            NoBotanistSoilTrash = _botanistSoil.Value;
            NoBotanistAdditiveTrash = _botanistAdditive.Value;
            NoBotanistSeedTrash = _botanistSeed.Value;
            NoPlantScrapTrash = _plantScrap.Value;
            ModLog.VerboseEnabled = _verbose.Value;
        }
    }
}
