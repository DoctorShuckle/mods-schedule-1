using System;
using System.Collections.Generic;
using MelonLoader;

namespace ConfigurableStorage
{
    /// <summary>
    /// The mod's settings, in UserData/MelonPreferences.cfg. [ConfigurableStorage] holds VerboseLogging; each
    /// container has its own [ConfigurableStorage_&lt;id&gt;] block with Slots and Rows. Slots defaults to 20 and Rows to 2; 0 leaves
    /// the game's value alone. The defaults are 20 slots in 2 rows.
    /// </summary>
    internal static class ModConfig
    {
        private sealed class Entries
        {
            public MelonPreferences_Entry<int> Slots, Rows;
        }

        /// <summary>What every container gets until the player changes it: 20 slots in 2 rows (10 across).</summary>
        private const int DefaultSlots = 20;
        private const int DefaultRows = 2;

        private static readonly Dictionary<string, Entries> ById = new Dictionary<string, Entries>(StringComparer.OrdinalIgnoreCase);
        private static MelonPreferences_Entry<bool> _verbose;

        public static void Init()
        {
            MelonPreferences_Category general = MelonPreferences.CreateCategory("ConfigurableStorage");
            _verbose = general.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each container the mod resizes, with the game's own size, and list every other placeable storage item the game has. Turn on to pick values or when reporting a problem.");

            foreach ((string id, string name) in Containers.All)
            {
                MelonPreferences_Category block = MelonPreferences.CreateCategory("ConfigurableStorage_" + id, "Container: " + name);
                ById[id] = new Entries
                {
                    Slots = block.CreateEntry("Slots", DefaultSlots, "Slots",
                        "Slots in a " + name + ", " + StorageSizing.MaxSlots + " at most. Default " + DefaultSlots + ". 0 = as in the game. A size below the game's own is allowed, but items saved in the slots that no longer exist are lost on the next load and save, so empty the container first. Everyone in a multiplayer game needs the same sizes, so every player must have the mod and the same settings."),
                    Rows = block.CreateEntry("Rows", DefaultRows, "Rows",
                        "Rows the storage screen lays the slots out in, 1 to " + StorageSizing.MaxRows + ". Default " + DefaultRows + ". 0 = automatic: keep the game's width and add rows as slots are added."),
                };
            }
        }

        /// <summary>Copies the current preference values into the game's prefabs. Safe to call at any time.</summary>
        public static void Apply()
        {
            if (_verbose == null) return;
            ModLog.VerboseEnabled = _verbose.Value;
            try
            {
                GameAccess.ApplyAll();
            }
            catch (Exception ex)
            {
                // Fail open: the game keeps its own sizes.
                ModLog.WarnOnce("Could not resize the storage containers: " + ex);
            }
        }

        /// <summary>The configured slots and rows for a container ID, or false when the ID is not one the mod handles.</summary>
        public static bool TryGet(string id, out int slots, out int rows)
        {
            slots = 0;
            rows = 0;
            if (id == null || !ById.TryGetValue(id, out Entries e)) return false;
            slots = e.Slots.Value;
            rows = e.Rows.Value;
            return true;
        }
    }
}
