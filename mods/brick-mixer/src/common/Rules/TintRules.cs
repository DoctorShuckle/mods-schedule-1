namespace BrickMixer
{
    /// <summary>
    /// The arithmetic of the red tint, with no game types. The model's colors are multiplied by a
    /// color: a white part becomes the tint, a black part stays black, and the strength blends between
    /// the original (0) and the full tint (1).
    /// </summary>
    public static class TintRules
    {
        public const string DefaultHex = "#E02626";
        public const float DefaultStrength = 1f;

        /// <summary>Reads "#RRGGBB" or "RRGGBB" (any case) into 0 to 1 channels.</summary>
        public static bool TryParseHex(string text, out float r, out float g, out float b)
        {
            r = g = b = 1f;
            if (text == null) return false;
            string hex = text.Trim();
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6) return false;

            int value = 0;
            foreach (char c in hex)
            {
                int digit = c >= '0' && c <= '9' ? c - '0'
                          : c >= 'a' && c <= 'f' ? c - 'a' + 10
                          : c >= 'A' && c <= 'F' ? c - 'A' + 10
                          : -1;
                if (digit < 0) return false;
                value = value * 16 + digit;
            }
            r = ((value >> 16) & 0xFF) / 255f;
            g = ((value >> 8) & 0xFF) / 255f;
            b = (value & 0xFF) / 255f;
            return true;
        }

        /// <summary>A strength from 0 to 1; anything else (including not a number) is the fallback.</summary>
        public static float ValidStrength(float value, float fallback)
            => !float.IsNaN(value) && value >= 0f && value <= 1f ? value : fallback;

        /// <summary>The factor a color channel is multiplied by: 1 (unchanged) at strength 0, <c>tint</c> at strength 1.</summary>
        public static float Factor(float tint, float strength) => 1f + (tint - 1f) * strength;
    }
}
