namespace TeleportApp
{
    /// <summary>
    /// The stores. The game has no list of store exteriors (a shop is a menu, run by a clerk), so these
    /// are fixed points in front of each store, as plain numbers. The height is only a hint: the mod
    /// snaps each point to the ground when it teleports. Positions follow the list in the Teleport Menu
    /// mod, checked against the doors of the buildings the game does list (Pawn Shop, Boutique Store).
    /// </summary>
    internal static class Stores
    {
        internal readonly struct Store
        {
            public readonly string Name;
            public readonly float X, Y, Z;

            public Store(string name, float x, float y, float z)
            {
                Name = name;
                X = x;
                Y = y;
                Z = z;
            }
        }

        public static readonly Store[] All =
        {
            new Store("Dan's Hardware", -21f, -3f, 137f),
            new Store("Handy Hank's", 104f, 1f, 25f),
            new Store("Gas-Mart", -113f, -2.9f, 68f),
            new Store("Gas-Mart / Auto Shop", 16f, 1f, -16.5f),
            new Store("Pawn Shop", -61.3f, 1f, 53f),
            new Store("Shred Shack", -39f, -2.9f, 121f),
            new Store("Ray's Real Estate", 81.5f, 1f, -7f),
            new Store("Blueball's Boutique", 71f, 1f, -8f),
            new Store("Thrifty Threads", -22.5f, 1f, 12f),
            new Store("Top Tattoo", -130f, -2.9f, 67.4f),
        };
    }
}
