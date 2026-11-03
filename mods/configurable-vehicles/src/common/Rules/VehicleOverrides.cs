namespace ConfigurableVehicles
{
    /// <summary>
    /// What the player asked for on one vehicle. Every value is already cleaned: zero means "leave the
    /// game's value" (see <see cref="VehicleSettings.Positive"/>). Mass, speeds, angles and downforce
    /// are absolute; Power, Brake and Grip multiply the game's own value.
    /// </summary>
    internal sealed class VehicleOverrides
    {
        public const float MaxMass = 20000f;
        public const float MaxSpeed = 500f;
        public const float MaxAngle = 80f;
        public const float MaxSteerRate = 1000f;
        public const float MaxDownforce = 100f;
        public const float MaxMultiplier = 10f;

        /// <summary>Rigidbody mass in kg. Heavier brakes harder, because braking force scales with mass.</summary>
        public float Mass { get; }

        /// <summary>Multiplies the engine's torque curve: acceleration, and the speed the car settles at.</summary>
        public float Power { get; }

        /// <summary>km/h. The game uses it only to scale braking and the engine sound, not as a speed limit.</summary>
        public float TopSpeed { get; }

        /// <summary>Degrees the front wheels turn at full lock.</summary>
        public float SteerAngle { get; }

        /// <summary>Degrees per second the wheels turn toward the steering input.</summary>
        public float SteerRate { get; }

        /// <summary>Multiplies the brake force.</summary>
        public float Brake { get; }

        public float Downforce { get; }

        /// <summary>Multiplies the grip (friction stiffness) of every wheel, sideways and forward.</summary>
        public float Grip { get; }

        /// <summary>Trunk slots, 0 = leave as set by the all-vehicles setting or the game.</summary>
        public int TrunkSlots { get; }

        public VehicleOverrides(float mass, float power, float topSpeed, float steerAngle, float steerRate,
            float brake, float downforce, float grip, int trunkSlots)
        {
            Mass = VehicleSettings.Positive(mass, MaxMass);
            Power = VehicleSettings.Positive(power, MaxMultiplier);
            TopSpeed = VehicleSettings.Positive(topSpeed, MaxSpeed);
            SteerAngle = VehicleSettings.Positive(steerAngle, MaxAngle);
            SteerRate = VehicleSettings.Positive(steerRate, MaxSteerRate);
            Brake = VehicleSettings.Positive(brake, MaxMultiplier);
            Downforce = VehicleSettings.Positive(downforce, MaxDownforce);
            Grip = VehicleSettings.Positive(grip, MaxMultiplier);
            TrunkSlots = VehicleSettings.Slots(trunkSlots);
        }

        /// <summary>True when nothing that acts on a running vehicle is set (the trunk size acts at wake-up and is separate).</summary>
        public bool HasNoHandling => Mass == 0f && Power == 0f && TopSpeed == 0f && SteerAngle == 0f
            && SteerRate == 0f && Brake == 0f && Downforce == 0f && Grip == 0f;
    }
}
