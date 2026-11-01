using System;
using System.Collections.Generic;

namespace ConfigurableEmployees
{
    /// <summary>
    /// Sets each property's employee capacity from the cfg. It is reached two ways, because the
    /// Awake patch alone is not enough: the properties that are subclasses (the bungalow, the
    /// sweatshop and so on) call the base Awake from their own, and that call can be compiled into
    /// theirs, past a patch on the base method. So the patch handles whatever it sees (early enough
    /// for the listing poster and the hiring dialogue) and a sweep after the scene loads covers the
    /// rest. Either way a property is handled once.
    /// </summary>
    internal static class PropertyTuning
    {
        private static readonly HashSet<long> Handled = new HashSet<long>();

        public static void Apply(GameProperty property)
        {
            if (property == null || !Handled.Add(GameAccess.IdOf(property))) return;

            int game = property.EmployeeCapacity;
            // Always log: this is how the player finds the game's capacity, and which properties exist.
            ModLog.Info("Property '" + property.PropertyCode + "': employee capacity " + game + ", " + GameAccess.IdlePointCount(property) + " idle point(s).");

            int wanted = PropertyCapacity.Resolve(property.PropertyCode, game);
            if (wanted == game) return;

            GameAccess.EnsureIdlePoints(property, wanted);
            // If idle points could not be added, stay at what the game can place.
            property.EmployeeCapacity = Math.Min(wanted, Math.Max(game, GameAccess.IdlePointCount(property)));
            ModLog.Info("Property '" + property.PropertyCode + "': employee capacity set to " + property.EmployeeCapacity + ".");
        }

        /// <summary>After a scene has loaded: handle every property the game has, including any the patch missed.</summary>
        public static void ApplyAll()
        {
            List<GameProperty> properties = GameAccess.AllProperties();
            // Leaving a game (back to the menu) empties the list; forget the old objects so a new
            // game's properties are not mistaken for them.
            if (properties.Count == 0) Handled.Clear();
            foreach (GameProperty property in properties) Apply(property);
        }
    }
}
