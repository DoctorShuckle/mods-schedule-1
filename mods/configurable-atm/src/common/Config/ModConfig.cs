using System;
using System.Linq;
using MelonLoader;

namespace ConfigurableAtm
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [ConfigurableAtm]. The
    /// defaults leave the game as it is. Unusable values are not obeyed: see <see cref="DepositRules"/>.
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<float> _limit;
        private static MelonPreferences_Entry<bool> _limitEnabled;
        private static MelonPreferences_Entry<string> _presets;
        private static MelonPreferences_Entry<bool> _verbose;
        private static string _lastSummary;
        private static string _limitProblem;
        private static string _presetsProblem;

        /// <summary>The weekly limit the ATM enforces: the game's own arithmetic runs against this.</summary>
        public static float EffectiveLimit { get; private set; } = DepositRules.GameLimit;

        /// <summary>The weekly total at which the Clean Cash quest starts.</summary>
        public static float QuestThreshold { get; private set; } = DepositRules.GameLimit;

        public static bool LimitEnabled { get; private set; } = true;

        public static int[] Presets { get; private set; } = (int[])DepositRules.DefaultPresets.Clone();

        /// <summary>Raised when the amount buttons setting changes to a different row; the argument is the new row.</summary>
        public static event Action<int[]> PresetsChanged;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("ConfigurableAtm");

            _limit = category.CreateEntry("WeeklyDepositLimit", DepositRules.GameLimit, "Weekly deposit limit",
                "How much cash you can deposit at an ATM each week (10000 in the game). Above 0 and at most 1000000000: zero, negative or not-a-number counts as 10000, and larger values are capped.");
            _limitEnabled = category.CreateEntry("DepositLimitEnabled", true, "Deposit limit enabled",
                "false removes the weekly limit altogether: the ATM takes any amount, and the limit display is hidden.");
            _presets = category.CreateEntry("DepositAmounts", "20, 50, 100, 500, 1000, 5000", "Amount buttons",
                "The six amount buttons on the ATM, as six whole numbers above 0 separated by commas. The last button is MAX when depositing and holds its amount only when withdrawing. Anything else is ignored and the game's buttons are used.");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log what the mod patches and changes. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_limit == null) return;

            ModLog.VerboseEnabled = _verbose.Value;

            LimitEnabled = _limitEnabled.Value;
            EffectiveLimit = DepositRules.EffectiveLimit(LimitEnabled, _limit.Value);
            QuestThreshold = DepositRules.QuestThreshold(LimitEnabled, _limit.Value);

            // Preferences events arrive in bursts (one per category): each problem is said once.
            WarnOnce(ref _limitProblem,
                LimitEnabled && DepositRules.Limit(_limit.Value) != _limit.Value
                    ? "WeeklyDepositLimit " + _limit.Value + " is not usable as it is; using " + EffectiveLimit + "."
                    : null);

            bool presetsUsable = DepositRules.TryParsePresets(_presets.Value, out int[] amounts);
            WarnOnce(ref _presetsProblem,
                presetsUsable ? null : "DepositAmounts \"" + _presets.Value + "\" is not six whole numbers above 0; using the game's amounts.");

            int[] previous = Presets;
            Presets = amounts ?? (int[])DepositRules.DefaultPresets.Clone();
            if (!previous.SequenceEqual(Presets)) PresetsChanged?.Invoke(Presets);

            string summary = "limit " + (LimitEnabled ? EffectiveLimit.ToString() : "off") +
                ", quest at " + QuestThreshold + ", buttons " + string.Join(", ", Presets);
            if (summary != _lastSummary)
            {
                _lastSummary = summary;
                ModLog.Verbose("Settings: " + summary + ".");
            }
        }

        /// <summary>Logs a problem when it first appears or changes; a cleared problem is forgotten, so it can be said again.</summary>
        private static void WarnOnce(ref string last, string problem)
        {
            if (problem == last) return;
            last = problem;
            if (problem != null) ModLog.Warning(problem);
        }
    }
}
