namespace TeleportApp
{
    public enum Refusal
    {
        None,
        NotInGame,
        InVehicle,
        NoSpawnPoint
    }

    /// <summary>When a teleport must not happen. There is no cost or cooldown: only states where it cannot work.</summary>
    internal static class Guard
    {
        public static Refusal Check(bool playerReady, bool inVehicle, bool hasTarget)
        {
            if (!playerReady) return Refusal.NotInGame;
            if (inVehicle) return Refusal.InVehicle;
            if (!hasTarget) return Refusal.NoSpawnPoint;
            return Refusal.None;
        }

        public static string Describe(Refusal refusal)
        {
            switch (refusal)
            {
                case Refusal.NotInGame: return "Not in a game yet";
                case Refusal.InVehicle: return "Leave the vehicle first";
                case Refusal.NoSpawnPoint: return "Cannot find that place right now";
                default: return "";
            }
        }
    }
}
