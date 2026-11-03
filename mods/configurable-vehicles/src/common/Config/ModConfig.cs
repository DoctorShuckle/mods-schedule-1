using System;
using System.Collections.Generic;
using MelonLoader;

namespace ConfigurableVehicles
{
    /// <summary>
    /// The mod's settings, in UserData/MelonPreferences.cfg. [ConfigurableVehicles] holds the general
    /// ones; each vehicle has its own [ConfigurableVehicles_&lt;code&gt;] block. In a vehicle block, 0
    /// (the default) leaves the game's value alone. A value that cannot be used (not a number, negative)
    /// counts as 0 and is capped when absurd, with one warning.
    /// </summary>
    internal static class ModConfig
    {
        /// <summary>
        /// The vehicles the game ships, by their codes (turn on VerboseLogging to see the game's own
        /// list). Each gets a settings block. A vehicle the game adds later has no block until listed here.
        /// </summary>
        private static readonly string[] Codes =
        {
            "shitbox", "shitbox_police", "bruiser", "bruiser_police", "hounddog", "hotbox", "dinkler", "cheetah", "veeper",
        };

        private sealed class VehicleEntries
        {
            public MelonPreferences_Entry<float> Mass, Power, TopSpeed, SteerAngle, SteerRate, Brake, Downforce, Grip;
            public MelonPreferences_Entry<int> TrunkSlots;
        }

        private static readonly Dictionary<string, VehicleEntries> Entries = new Dictionary<string, VehicleEntries>(StringComparer.OrdinalIgnoreCase);
        private static MelonPreferences_Entry<string> _allTrunkSlots;
        private static MelonPreferences_Entry<int> _trunkRows;
        private static MelonPreferences_Entry<bool> _verbose;

        /// <summary>The settings of each vehicle that has any (by code, ignoring case).</summary>
        public static Dictionary<string, VehicleOverrides> Vehicles { get; private set; } = new Dictionary<string, VehicleOverrides>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Trunk slots for every vehicle without its own setting; 0 = as in the game.</summary>
        public static int AllTrunkSlots { get; private set; }

        /// <summary>Rows of the trunk screen for a vehicle whose size is set.</summary>
        public static int TrunkRows { get; private set; } = 2;

        public static void Init()
        {
            MelonPreferences_Category general = MelonPreferences.CreateCategory("ConfigurableVehicles");

            _allTrunkSlots = general.CreateEntry("TrunkSlots", "", "Trunk slots (all vehicles)",
                "A number from 1 to 20 sets the trunk size of every vehicle that has no TrunkSlots of its own below. Empty = as in the game. Everyone in a multiplayer game needs the same sizes, so every player must have the mod and the same settings.");
            _trunkRows = general.CreateEntry("TrunkRows", 2, "Trunk rows",
                "How many rows the trunk screen lays the slots out in, for vehicles whose size is set: 20 slots in 2 rows is 10 across. 1 to 5.");
            _verbose = general.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log every vehicle as it starts (its code and the game's handling values) and what the mod changes. Turn on to pick values or when reporting a problem.");

            foreach (string code in Codes)
            {
                MelonPreferences_Category block = MelonPreferences.CreateCategory("ConfigurableVehicles_" + code, "Vehicle: " + code);
                Entries[code] = new VehicleEntries
                {
                    Mass = block.CreateEntry("Mass", 0f, "Mass (kg)",
                        "Weight of the vehicle. A heavier vehicle also brakes harder, because brake force scales with weight. 0 = as in the game. Max " + VehicleOverrides.MaxMass + "."),
                    Power = block.CreateEntry("PowerMultiplier", 0f, "Power multiplier",
                        "Multiplies the engine's torque: acceleration, and the speed the vehicle settles at (the game has no hard speed limit). 1.2 is 20% more. 0 = as in the game. Max " + VehicleOverrides.MaxMultiplier + "."),
                    TopSpeed = block.CreateEntry("TopSpeed", 0f, "Top speed (km/h)",
                        "The game uses this only to scale braking and the engine sound; it does NOT limit speed (use PowerMultiplier for that). 0 = as in the game."),
                    SteerAngle = block.CreateEntry("SteerAngle", 0f, "Steering angle (degrees)",
                        "How far the front wheels turn at full lock. 0 = as in the game. Max " + VehicleOverrides.MaxAngle + "."),
                    SteerRate = block.CreateEntry("SteerRate", 0f, "Steering speed (degrees per second)",
                        "How fast the wheels turn toward the steering input. 0 = as in the game."),
                    Brake = block.CreateEntry("BrakeMultiplier", 0f, "Brake multiplier",
                        "Multiplies the brake force. 0 = as in the game. Max " + VehicleOverrides.MaxMultiplier + "."),
                    Downforce = block.CreateEntry("Downforce", 0f, "Downforce",
                        "Pushes the vehicle onto the road harder the faster it goes, so it grips and stays stable. 0 = as in the game."),
                    Grip = block.CreateEntry("GripMultiplier", 0f, "Grip multiplier",
                        "Multiplies every wheel's grip, sideways and forward. Higher turns sharper and slides less. 0 = as in the game. Max " + VehicleOverrides.MaxMultiplier + "."),
                    TrunkSlots = block.CreateEntry("TrunkSlots", 0, "Trunk slots",
                        "Trunk size, 1 to 20. 0 = use the all-vehicles setting, else as in the game. Applies when the vehicle spawns."),
                };
            }
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_verbose == null) return;

            AllTrunkSlots = VehicleSettings.SlotsFromText(_allTrunkSlots.Value);
            if (!string.IsNullOrWhiteSpace(_allTrunkSlots.Value) && AllTrunkSlots == 0)
                ModLog.WarnOnce("TrunkSlots: '" + _allTrunkSlots.Value + "' is not a number from 1 to 20; trunks are left as in the game.");
            TrunkRows = Math.Max(VehicleSettings.MinRows, Math.Min(VehicleSettings.MaxRows, _trunkRows.Value));
            ModLog.VerboseEnabled = _verbose.Value;

            var vehicles = new Dictionary<string, VehicleOverrides>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, VehicleEntries> pair in Entries)
            {
                VehicleEntries e = pair.Value;
                var overrides = new VehicleOverrides(
                    Read(pair.Key, e.Mass), Read(pair.Key, e.Power), Read(pair.Key, e.TopSpeed), Read(pair.Key, e.SteerAngle),
                    Read(pair.Key, e.SteerRate), Read(pair.Key, e.Brake), Read(pair.Key, e.Downforce), Read(pair.Key, e.Grip),
                    e.TrunkSlots.Value);
                if (!overrides.HasNoHandling || overrides.TrunkSlots > 0) vehicles[pair.Key] = overrides;
            }
            Vehicles = vehicles;
        }

        private static float Read(string code, MelonPreferences_Entry<float> entry)
        {
            float value = entry.Value;
            if (value != 0f && (float.IsNaN(value) || float.IsInfinity(value) || value < 0f))
                ModLog.WarnOnce(code + " " + entry.Identifier + " = " + value + " is not usable; the game's value is kept.");
            return value;
        }

        /// <summary>The settings for a vehicle, or null when it has none.</summary>
        public static VehicleOverrides For(string code)
            => code != null && Vehicles.TryGetValue(code, out VehicleOverrides overrides) ? overrides : null;

        /// <summary>The trunk size to build for a vehicle: its own, else the all-vehicles one, else 0 (as in the game).</summary>
        public static int TrunkSlotsFor(string code)
            => VehicleSettings.SlotsFor(For(code)?.TrunkSlots ?? 0, AllTrunkSlots);
    }
}
