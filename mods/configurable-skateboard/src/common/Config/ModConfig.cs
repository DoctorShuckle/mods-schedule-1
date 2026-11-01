using MelonLoader;

namespace ConfigurableSkateboard
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [ConfigurableSkateboard]. Each
    /// is a multiplier on the game's own value: 1 leaves it as in the game. Unusable values (zero
    /// speed, negative, not a number) are treated as 1, and everything is capped at 10.
    /// </summary>
    internal static class ModConfig
    {
        private static MelonPreferences_Entry<float> _topSpeed;
        private static MelonPreferences_Entry<float> _pushForce;
        private static MelonPreferences_Entry<float> _jumpForce;
        private static MelonPreferences_Entry<float> _pushStamina;
        private static MelonPreferences_Entry<bool> _verbose;

        public static float TopSpeed { get; private set; } = 1.5f;
        public static float PushForce { get; private set; } = 1.5f;
        public static float JumpForce { get; private set; } = 1.5f;
        public static float PushStamina { get; private set; } = 0.5f;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("ConfigurableSkateboard");

            _topSpeed = category.CreateEntry("TopSpeedMultiplier", 1.5f, "Top speed multiplier",
                "Multiplies the board's top speed (10 km/h in the game). The board has no hard cap: this also shifts the curves that fade out the push as you speed up, so it is the speed the board settles near. 1 = as in the game.");
            _pushForce = category.CreateEntry("PushForceMultiplier", 1.5f, "Push force multiplier",
                "Multiplies how hard each push accelerates the board. Raise it with the top speed, or the board reaches the old top speed sooner and gains little. 1 = as in the game.");
            _jumpForce = category.CreateEntry("JumpForceMultiplier", 1.5f, "Jump force multiplier",
                "Multiplies the upward force of a jump, so jump height. 1 = as in the game.");
            _pushStamina = category.CreateEntry("PushStaminaMultiplier", 0.5f, "Push stamina cost multiplier",
                "Multiplies the stamina a push spends (12.5 in the game); 0 makes pushing free. You still need 12.5 stamina to start a push: that check is fixed in the game. 1 = as in the game.");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each board the mod tunes. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_topSpeed == null) return;

            TopSpeed = Tuning.Multiplier(_topSpeed.Value);
            PushForce = Tuning.Multiplier(_pushForce.Value);
            JumpForce = Tuning.Multiplier(_jumpForce.Value);
            PushStamina = Tuning.CostMultiplier(_pushStamina.Value);
            ModLog.VerboseEnabled = _verbose.Value;
        }
    }
}
