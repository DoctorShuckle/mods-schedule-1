using System;

namespace ConfigurableSkateboard
{
    /// <summary>
    /// Scales a board's settings by the player's multipliers. The game rebuilds a board's settings
    /// from its shared defaults on every weather change (blending in the rain), so the scaling has
    /// to follow each rebuild, and must run exactly once on each fresh copy.
    /// </summary>
    internal static class BoardTuner
    {
        /// <summary>
        /// After the game rebuilt the board's settings: they are a fresh private copy, so scale them.
        /// </summary>
        public static void AfterWeatherChange(Skateboard board)
        {
            try
            {
                if (board == null) return;
                Scale(GameAccess.Settings(board));
                ModLog.Verbose("Tuned a board after a weather change.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not tune the board: " + ex);
            }
        }

        /// <summary>
        /// When the board starts: until the first weather change it uses the shared defaults, so give
        /// it its own copy and scale that. If the weather already rebuilt (and we already scaled) the
        /// settings, leave them alone.
        /// </summary>
        public static void AtStart(Skateboard board)
        {
            try
            {
                if (board == null || !GameAccess.UsesSharedDefault(board)) return;

                SkateboardSettings own = GameAccess.CopyOfSettings(GameAccess.Settings(board));
                Scale(own);
                GameAccess.SetSettings(board, own);
                ModLog.Verbose("Tuned a new board.");
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not tune the new board: " + ex);
            }
        }

        private static void Scale(SkateboardSettings settings)
        {
            settings.TopSpeed_Kmh *= ModConfig.TopSpeed;
            settings.PushForceMultiplier *= ModConfig.PushForce;
            settings.JumpForce *= ModConfig.JumpForce;
        }
    }
}
