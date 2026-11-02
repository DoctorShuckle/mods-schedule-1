namespace ConfigurableAtm
{
    /// <summary>
    /// What the Mono transpilers call in place of the game's inlined 10000. They are in the game's
    /// IL, so the class and its methods are public: nothing about them depends on the call site
    /// being allowed to see the mod's internals.
    /// </summary>
    public static class LimitHooks
    {
        /// <summary>The weekly deposit limit the ATM enforces.</summary>
        public static float Limit() => ModConfig.EffectiveLimit;

        /// <summary>The weekly total at which the Clean Cash quest starts.</summary>
        public static float QuestThreshold() => ModConfig.QuestThreshold;
    }
}
