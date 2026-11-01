using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;

namespace ConfigurableEmployees
{
    /// <summary>What can be changed for one kind of employee. <see cref="Setting.Unset"/> means "leave it".</summary>
    internal sealed class EmployeeSettings
    {
        public float DailyWage = Setting.Unset;
        public float SigningFee = Setting.Unset;
        public float WalkSpeed = Setting.Unset;

        /// <summary>Stations (chemist, packager), bins (cleaner) or pots, racks and beds (botanist).</summary>
        public int MaxAssigned = -1;
    }

    /// <summary>
    /// The mod's settings, kept in UserData/ConfigurableEmployees.cfg. <see cref="Init"/> creates
    /// the entries; <see cref="Apply"/> copies their current values into the plain fields below,
    /// which is what the patches read. Nothing else reads the preference objects.
    /// </summary>
    internal static class ModConfig
    {
        public static readonly EmployeeSettings Botanist = new EmployeeSettings();
        public static readonly EmployeeSettings Chemist = new EmployeeSettings();
        public static readonly EmployeeSettings Packager = new EmployeeSettings();
        public static readonly EmployeeSettings Cleaner = new EmployeeSettings();

        public static int PackagerMaxRoutes = -1;
        public static float PackagerSpeedMultiplier = Setting.Unset;

        public static float FixerEarlyFee = FixerFee.VanillaEarlyHireFee;
        public static int FixerEarlyCount = FixerFee.VanillaEarlyHireCount;
        public static float FixerLaterFee = FixerFee.VanillaLaterHireFee;
        public static float FixerFeeCap = FixerFee.VanillaFeeCap;

        private static readonly List<System.Action> Readers = new List<System.Action>();
        private static MelonPreferences_Entry<int> _verbosity;

        // MelonLoader raises "preferences loaded" as each category is created, before the rest exist.
        private static bool _ready;

        private static MelonPreferences_Entry<string> _skipPatches;

        /// <summary>Patch names to leave off (troubleshooting). Read once, before the patches are applied.</summary>
        public static string SkipPatches => _skipPatches == null ? "" : _skipPatches.Value;

        public static EmployeeSettings For(EmployeeKind kind)
        {
            switch (kind)
            {
                case EmployeeKind.Botanist: return Botanist;
                case EmployeeKind.Chemist: return Chemist;
                case EmployeeKind.Packager: return Packager;
                case EmployeeKind.Cleaner: return Cleaner;
                default: return null;
            }
        }

        public static void Init()
        {
            string file = Path.Combine(MelonEnvironment.UserDataDirectory, "ConfigurableEmployees.cfg");

            MelonPreferences_Category general = Category("General", "General", file);
            _verbosity = general.CreateEntry("Verbosity", 0, "Logging detail",
                "0 = quiet. 1 = log every change the mod makes to an employee. 2 = also log the start and stop of every timed task, with who and when. Turn up when reporting a problem.");
            _skipPatches = general.CreateEntry("SkipPatches", "", "Patches to leave off",
                "Troubleshooting. Comma-separated parts of patch names to not apply, e.g. TaskDuration, MouseScrollDelta. Needs a game restart.");

            const string unset = " -1 leaves the game's value alone.";

            AddEmployee(Category("Botanist", "Botanist", file), Botanist, "Botanist", "pots, drying racks, mushroom beds and spawn stations (game: 8)");
            AddEmployee(Category("Chemist", "Chemist", file), Chemist, "Chemist", "stations (game: 4)");
            AddEmployee(Category("Packager", "Packager (Handler)", file), Packager, "Packager", "stations (game: 3)");
            AddEmployee(Category("Cleaner", "Cleaner", file), Cleaner, "Cleaner", "trash bins (game: 6)");

            MelonPreferences_Category packager = Category("Packager", "Packager (Handler)", file);
            Bind(packager, "MaxRoutes", -1, "Max routes",
                "Most routes one packager can have (game: 5). Above 5 the routes list in the management screen gets a scroll area." + unset,
                v => PackagerMaxRoutes = v);
            Bind(packager, "SpeedMultiplier", Setting.Unset, "Packaging speed multiplier",
                "How fast packagers pack, 1 = game speed, 2 = twice as fast." + unset,
                v => PackagerSpeedMultiplier = v);

            // Each worker's action times sit in that worker's own section, after its other settings.
            foreach (TaskTime time in TaskTimes.All)
            {
                TaskTime entry = time;
                Bind(Category(entry.Kind.ToString(), entry.Kind.ToString(), file), entry.Key, Setting.Unset, entry.Label, entry.Describe(), v => entry.Configured = v);
            }

            MelonPreferences_Category fixer = Category("Fixer", "Fixer hiring fee", file);
            Bind(fixer, "EarlyHireFee", FixerFee.VanillaEarlyHireFee, "Extra fee, early hires",
                "Extra on top of the signing fee for each of your first hires, counted over the whole save (game: 100).", v => FixerEarlyFee = v);
            Bind(fixer, "EarlyHireCount", FixerFee.VanillaEarlyHireCount, "How many hires are 'early'",
                "How many hires pay the early fee before the later fee starts (game: 6).", v => FixerEarlyCount = v);
            Bind(fixer, "LaterHireFee", FixerFee.VanillaLaterHireFee, "Extra fee, later hires",
                "Extra fee for every hire after the early ones (game: 250).", v => FixerLaterFee = v);
            Bind(fixer, "FeeCap", FixerFee.VanillaFeeCap, "Extra fee limit",
                "The extra fee is never more than this (game: 500). Set all four to the game values for no change.", v => FixerFeeCap = v);

            MelonPreferences_Category properties = Category("Properties", "Property employee capacity", file);
            foreach (PropertyEntry property in PropertyCatalog.All)
            {
                PropertyEntry entry = property;
                Bind(properties, entry.Key, -1, entry.Label, entry.Describe(), v => entry.Configured = v);
            }
            _ready = true;
        }

        private static MelonPreferences_Category Category(string id, string display, string file)
        {
            MelonPreferences_Category category = MelonPreferences.GetCategory("ConfigurableEmployees_" + id)
                ?? MelonPreferences.CreateCategory("ConfigurableEmployees_" + id, display);
            category.SetFilePath(file);
            return category;
        }

        private static void AddEmployee(MelonPreferences_Category category, EmployeeSettings target, string name, string assigned)
        {
            const string unset = " -1 leaves the game's value alone.";
            Bind(category, "DailyWage", Setting.Unset, "Daily wage", "What a " + name + " is paid each day." + unset, v => target.DailyWage = v);
            Bind(category, "SigningFee", Setting.Unset, "Signing fee", "What hiring a " + name + " costs, before the Fixer's extra fee." + unset, v => target.SigningFee = v);
            Bind(category, "WalkSpeed", Setting.Unset, "Walk speed", "Metres per second." + unset, v => target.WalkSpeed = v);
            Bind(category, "MaxAssigned", -1, "Max assigned", "Most " + assigned + " one " + name + " can be given." + unset, v => target.MaxAssigned = v);
        }

        private static void Bind<T>(MelonPreferences_Category category, string id, T defaultValue, string display, string description, System.Action<T> store)
        {
            MelonPreferences_Entry<T> entry = category.CreateEntry(id, defaultValue, display, description);
            Readers.Add(() => store(entry.Value));
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (!_ready) return;

            ModLog.Level = LogLevel.Clamp(_verbosity.Value);
            foreach (System.Action read in Readers) read();
        }
    }
}
