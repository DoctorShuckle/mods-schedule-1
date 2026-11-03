using MelonLoader;

namespace BrickMixer
{
    /// <summary>
    /// The mod's settings. They live in UserData/MelonPreferences.cfg under [BrickMixer].
    /// </summary>
    internal static class ModConfig
    {
        public const float DefaultPriceMultiplier = 1.5f;

        private static MelonPreferences_Entry<float> _priceMultiplier;
        private static MelonPreferences_Entry<string> _tintColor;
        private static MelonPreferences_Entry<float> _tintStrength;
        private static MelonPreferences_Entry<bool> _verbose;
        private static bool _warnedPrice;
        private static bool _warnedTint;
        private static bool _warnedStrength;

        public static float PriceMultiplier { get; private set; } = DefaultPriceMultiplier;

        /// <summary>The tint's red, green and blue (0 to 1) and how strongly it is applied (0 to 1).</summary>
        public static float TintRed { get; private set; } = 0.878f;
        public static float TintGreen { get; private set; } = 0.149f;
        public static float TintBlue { get; private set; } = 0.149f;
        public static float TintStrength { get; private set; } = TintRules.DefaultStrength;

        public static void Init()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("BrickMixer");

            _priceMultiplier = category.CreateEntry("PriceMultiplier", DefaultPriceMultiplier, "Price multiplier",
                "The Brick Mixer costs this many times the Mixing Station Mk2's price. Applies after a restart of the game.");
            _tintColor = category.CreateEntry("TintColor", TintRules.DefaultHex, "Tint color",
                "The color the Brick Mixer's model is tinted, as #RRGGBB. Red by default. Applies after a restart of the game.");
            _tintStrength = category.CreateEntry("TintStrength", TintRules.DefaultStrength, "Tint strength",
                "How strongly the tint is applied, from 0 (the Mk2's own colors) to 1 (fully tinted). Applies after a restart of the game.");
            _verbose = category.CreateEntry("VerboseLogging", false, "Verbose logging",
                "Log each step of adding the Brick Mixer and each brick it unpacks or packs. Turn on when reporting a problem.");
        }

        /// <summary>Copies the current preference values into the places the patches read them.</summary>
        public static void Apply()
        {
            if (_priceMultiplier == null) return;

            float value = BrickRules.ValidMultiplier(_priceMultiplier.Value, DefaultPriceMultiplier);
            if (value != _priceMultiplier.Value && !_warnedPrice)
            {
                _warnedPrice = true;
                ModLog.Warning("PriceMultiplier " + _priceMultiplier.Value + " is not a number above 0 and at most 1000; using " + DefaultPriceMultiplier + ".");
            }
            PriceMultiplier = value;

            if (TintRules.TryParseHex(_tintColor.Value, out float r, out float g, out float b))
            {
                TintRed = r;
                TintGreen = g;
                TintBlue = b;
            }
            else
            {
                TintRules.TryParseHex(TintRules.DefaultHex, out r, out g, out b);
                TintRed = r;
                TintGreen = g;
                TintBlue = b;
                if (!_warnedTint)
                {
                    _warnedTint = true;
                    ModLog.Warning("TintColor \"" + _tintColor.Value + "\" is not #RRGGBB; using " + TintRules.DefaultHex + ".");
                }
            }

            float strength = TintRules.ValidStrength(_tintStrength.Value, TintRules.DefaultStrength);
            if (strength != _tintStrength.Value && !_warnedStrength)
            {
                _warnedStrength = true;
                ModLog.Warning("TintStrength " + _tintStrength.Value + " is not between 0 and 1; using " + TintRules.DefaultStrength + ".");
            }
            TintStrength = strength;
            ModLog.VerboseEnabled = _verbose.Value;
        }
    }
}
