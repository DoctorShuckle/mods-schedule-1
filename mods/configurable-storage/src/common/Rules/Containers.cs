namespace ConfigurableStorage
{
    /// <summary>The player-placeable storage items this mod can resize, by the game's item ID.</summary>
    internal static class Containers
    {
        /// <summary>Item ID and the name the game shows for it (used in the settings' descriptions).</summary>
        public static readonly (string Id, string Name)[] All =
        {
            ("smallstoragerack", "Small Storage Rack"),
            ("mediumstoragerack", "Medium Storage Rack"),
            ("largestoragerack", "Large Storage Rack"),
            ("smallstoragecloset", "Small Storage Closet"),
            ("mediumstoragecloset", "Medium Storage Closet"),
            ("largestoragecloset", "Large Storage Closet"),
            ("hugestoragecloset", "Huge Storage Closet"),
            ("safe", "Safe"),
        };
    }
}
