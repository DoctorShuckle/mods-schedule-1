namespace TeleportApp
{
    /// <summary>A line of text near the bottom of the screen that fades after a moment.</summary>
    internal static class Toast
    {
        private const float Seconds = 2.5f;

        private static string _text = "";
        private static float _until;
        private static GUIStyle _style;

        public static void Show(string text)
        {
            _text = text;
            _until = Time.unscaledTime + Seconds;
        }

        public static void Draw()
        {
            if (Time.unscaledTime >= _until || string.IsNullOrEmpty(_text)) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
                _style.normal.textColor = Color.white;
            }

            var area = new Rect(0f, Screen.height * 0.78f, Screen.width, 40f);
            // A dark copy one pixel off keeps the text readable on any background.
            GUI.color = Color.black;
            GUI.Label(new Rect(area.x + 1f, area.y + 1f, area.width, area.height), _text, _style);
            GUI.color = Color.white;
            GUI.Label(area, _text, _style);
        }
    }
}
