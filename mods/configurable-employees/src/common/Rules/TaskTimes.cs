using System.Globalization;

namespace ConfigurableEmployees
{
    /// <summary>
    /// One timed action of one kind of worker: its setting in the cfg, the game's own time for it,
    /// and the value the player configured. Like everything in the Rules folder this file must not reference any game,
    /// Unity or MelonLoader type: it is compiled into the unit-test project too.
    ///
    /// <see cref="GameSeconds"/> is the number the game's code divides by the worker's work speed
    /// (the base time of the action). The mod changes the action's time by multiplying the work
    /// speed by <see cref="Factor"/>, so the configured value is a base time like the game's own and
    /// anything else that changes work speed still applies on top.
    /// </summary>
    internal sealed class TaskTime
    {
        /// <summary>The worker whose cfg section holds the setting, and who the action belongs to.</summary>
        public readonly EmployeeKind Kind;

        /// <summary>The key in the cfg.</summary>
        public readonly string Key;

        /// <summary>Short name used in the log.</summary>
        public readonly string Name;

        /// <summary>What the cfg shows next to the setting.</summary>
        public readonly string Label;

        /// <summary>The game's base seconds for one unit of the action (per item or per plant where it repeats).</summary>
        public readonly float GameSeconds;

        private readonly string _description;

        /// <summary>The configured seconds; <see cref="Setting.Unset"/> leaves the game's time alone.</summary>
        public float Configured = Setting.Unset;

        public TaskTime(EmployeeKind kind, string key, string name, string label, float gameSeconds, string description)
        {
            Kind = kind;
            Key = key;
            Name = name;
            Label = label;
            GameSeconds = gameSeconds;
            _description = description;
        }

        /// <summary>True when the player gave this action a time.</summary>
        public bool IsSet => Configured > 0f;

        /// <summary>The work speed multiplier that makes the action take the configured time (1 = no change).</summary>
        public float Factor => TaskTiming.TaskSpeedFactor(GameSeconds, Configured);

        /// <summary>The cfg comment: what the setting does, the game's value, and what -1 means.</summary>
        public string Describe()
            => _description + " (game: " + GameSeconds.ToString("0.##", CultureInfo.InvariantCulture) + " s). -1 leaves the game's value alone.";
    }

    /// <summary>
    /// Every action whose time the mod can change. This table drives the cfg entries and the lookup
    /// from a game behaviour to its entry (GameAccess), so a new action is one row here plus one line
    /// in that lookup.
    /// </summary>
    internal static class TaskTimes
    {
        // ---- botanist --------------------------------------------------------------------------
        public static readonly TaskTime Soil = new TaskTime(EmployeeKind.Botanist, "SoilSeconds", "Soil", "Pour soil (seconds)", 10f, "Time to put soil in a pot");
        public static readonly TaskTime Water = new TaskTime(EmployeeKind.Botanist, "WaterSeconds", "Water", "Water (seconds)", 10f, "Time to water a pot or mist a mushroom bed");
        public static readonly TaskTime Additive = new TaskTime(EmployeeKind.Botanist, "AdditiveSeconds", "Additive", "Add additive (seconds)", 10f, "Time to apply one additive");
        public static readonly TaskTime Sow = new TaskTime(EmployeeKind.Botanist, "SowSeconds", "Sow", "Sow seed (seconds)", 15f, "Time to sow one seed");
        public static readonly TaskTime Harvest = new TaskTime(EmployeeKind.Botanist, "HarvestSecondsPerPlant", "Harvest", "Harvest (seconds per plant)", 1f, "Harvest time for each plant or mushroom");
        public static readonly TaskTime Spawn = new TaskTime(EmployeeKind.Botanist, "SpawnSeconds", "Spawn", "Apply mushroom spawn (seconds)", 15f, "Time to spawn onto a mushroom bed");
        public static readonly TaskTime DryingRack = new TaskTime(EmployeeKind.Botanist, "DryingRackSecondsPerItem", "DryingRack", "Drying rack (seconds per item)", 1f, "Time to put each item on a drying rack");
        public static readonly TaskTime SpawnStation = new TaskTime(EmployeeKind.Botanist, "SpawnStationSeconds", "SpawnStation", "Spawn station (seconds)", 6f, "Time to make spawn at a spawn station");
        public static readonly TaskTime BotanistMoveItem = MoveItem(EmployeeKind.Botanist);

        // ---- chemist ---------------------------------------------------------------------------
        public static readonly TaskTime ChemistryStation = new TaskTime(EmployeeKind.Chemist, "ChemistryStationSeconds", "ChemistryStation", "Chemistry station (seconds)", 20f, "Time spent mixing at a chemistry station (its three stages, 8 + 6 + 6, are scaled together)");
        public static readonly TaskTime LabOvenLoad = new TaskTime(EmployeeKind.Chemist, "LabOvenLoadSeconds", "LabOvenLoad", "Lab oven, load (seconds)", 5f, "Time to load a lab oven");
        public static readonly TaskTime LabOvenUnload = new TaskTime(EmployeeKind.Chemist, "LabOvenUnloadSeconds", "LabOvenUnload", "Lab oven, empty (seconds)", 10f, "Time to empty a finished lab oven");
        public static readonly TaskTime MixingStation = new TaskTime(EmployeeKind.Chemist, "MixingSecondsPerItem", "MixingStation", "Mixing station (seconds per item)", 1f, "Time to mix each item at a mixing station");
        public static readonly TaskTime Cauldron = new TaskTime(EmployeeKind.Chemist, "CauldronSeconds", "Cauldron", "Cauldron (seconds)", 15f, "Time to start a cauldron");
        public static readonly TaskTime ChemistMoveItem = MoveItem(EmployeeKind.Chemist);

        // ---- packager --------------------------------------------------------------------------
        public static readonly TaskTime PackagingStation = new TaskTime(EmployeeKind.Packager, "PackagingStationSeconds", "PackagingStation", "Packaging station (seconds)", 5f, "Base time to pack one item, before the packager and station speed multipliers");
        public static readonly TaskTime BrickPress = new TaskTime(EmployeeKind.Packager, "BrickPressSeconds", "BrickPress", "Brick press (seconds)", 15f, "Base time to press one brick, before the packager speed multiplier");
        public static readonly TaskTime PackagerMoveItem = MoveItem(EmployeeKind.Packager);

        /// <summary>Every entry, in the order the cfg lists them.</summary>
        public static readonly TaskTime[] All =
        {
            Soil, Water, Additive, Sow, Harvest, Spawn, DryingRack, SpawnStation, BotanistMoveItem,
            ChemistryStation, LabOvenLoad, LabOvenUnload, MixingStation, Cauldron, ChemistMoveItem,
            PackagingStation, BrickPress, PackagerMoveItem,
        };

        /// <summary>
        /// Moving an item along a route is one game behaviour that several kinds of worker run, so each
        /// of them has an entry of their own for it. Null for a worker with none (the cleaner).
        /// </summary>
        public static TaskTime MoveItemFor(EmployeeKind kind)
        {
            switch (kind)
            {
                case EmployeeKind.Botanist: return BotanistMoveItem;
                case EmployeeKind.Chemist: return ChemistMoveItem;
                case EmployeeKind.Packager: return PackagerMoveItem;
                default: return null;
            }
        }

        private static TaskTime MoveItem(EmployeeKind kind)
            => new TaskTime(kind, "MoveItemSeconds", "MoveItem", "Moving items (seconds per step)", 0.5f, "Time for each of the two steps (pick up, put down) of moving an item along a route");
    }
}
