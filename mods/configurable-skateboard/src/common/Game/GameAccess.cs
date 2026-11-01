using HarmonyLib;

namespace ConfigurableSkateboard
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
#if !IL2CPP
        // Private field on Mono; the IL2CPP proxy exposes it as a public property.
        private static readonly AccessTools.FieldRef<Skateboard, SkateboardSettings> SettingsRef =
            AccessTools.FieldRefAccess<Skateboard, SkateboardSettings>("_settings");
#endif

        /// <summary>The settings the board reads on every physics step.</summary>
        public static SkateboardSettings Settings(Skateboard board)
        {
#if IL2CPP
            return board._settings;
#else
            return SettingsRef(board);
#endif
        }

        public static void SetSettings(Skateboard board, SkateboardSettings settings)
        {
#if IL2CPP
            board._settings = settings;
#else
            SettingsRef(board) = settings;
#endif
        }

        /// <summary>
        /// True while the board still uses the game's shared default settings object. Until the
        /// first weather change a new board points straight at it, and it belongs to every board,
        /// so it must be copied before anything is changed.
        /// </summary>
        public static bool UsesSharedDefault(Skateboard board)
            => ReferenceEquals(Settings(board), board.DefaultSettings);

        public static SkateboardSettings CopyOfSettings(SkateboardSettings settings) => settings.Clone();

        /// <summary>True while the local player is standing on a board.</summary>
        public static bool IsRiding
        {
            get
            {
                Player player = Player.Local;
                return player != null && player.ActiveSkateboard != null;
            }
        }
    }
}
