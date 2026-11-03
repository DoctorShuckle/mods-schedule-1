using HarmonyLib;

namespace ConfigurableVehicles
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
#if !IL2CPP
        // Protected fields on Mono; the IL2CPP proxy exposes them as public properties.
        private static readonly AccessTools.FieldRef<LandVehicle, float> MaxSteeringAngle = AccessTools.FieldRefAccess<LandVehicle, float>("maxSteeringAngle");
        private static readonly AccessTools.FieldRef<LandVehicle, float> SteerRate = AccessTools.FieldRefAccess<LandVehicle, float>("steerRate");
        private static readonly AccessTools.FieldRef<LandVehicle, AnimationCurve> MotorTorque = AccessTools.FieldRefAccess<LandVehicle, AnimationCurve>("motorTorque");
        private static readonly AccessTools.FieldRef<LandVehicle, float> DiffGearing = AccessTools.FieldRefAccess<LandVehicle, float>("diffGearing");
        private static readonly AccessTools.FieldRef<LandVehicle, float> HandBrakeForce = AccessTools.FieldRefAccess<LandVehicle, float>("handBrakeForce");
        private static readonly AccessTools.FieldRef<LandVehicle, AnimationCurve> BrakeForce = AccessTools.FieldRefAccess<LandVehicle, AnimationCurve>("brakeForce");
        private static readonly AccessTools.FieldRef<LandVehicle, float> BrakeForceMultiplier = AccessTools.FieldRefAccess<LandVehicle, float>("BrakeForceMultiplier");
        private static readonly AccessTools.FieldRef<LandVehicle, float> Downforce = AccessTools.FieldRefAccess<LandVehicle, float>("downforce");
        private static readonly AccessTools.FieldRef<LandVehicle, float> ReverseMultiplier = AccessTools.FieldRefAccess<LandVehicle, float>("reverseMultiplier");
#endif

        // The handling fields the mod changes, one getter and setter each.

        public static float GetSteerAngle(LandVehicle v)
#if IL2CPP
            => v.maxSteeringAngle;
#else
            => MaxSteeringAngle(v);
#endif

        public static void SetSteerAngle(LandVehicle v, float value)
        {
#if IL2CPP
            v.maxSteeringAngle = value;
#else
            MaxSteeringAngle(v) = value;
#endif
        }

        public static float GetSteerRate(LandVehicle v)
#if IL2CPP
            => v.steerRate;
#else
            => SteerRate(v);
#endif

        public static void SetSteerRate(LandVehicle v, float value)
        {
#if IL2CPP
            v.steerRate = value;
#else
            SteerRate(v) = value;
#endif
        }

        public static AnimationCurve GetTorque(LandVehicle v)
#if IL2CPP
            => v.motorTorque;
#else
            => MotorTorque(v);
#endif

        public static void SetTorque(LandVehicle v, AnimationCurve value)
        {
#if IL2CPP
            v.motorTorque = value;
#else
            MotorTorque(v) = value;
#endif
        }

        public static float GetBrake(LandVehicle v)
#if IL2CPP
            => v.BrakeForceMultiplier;
#else
            => BrakeForceMultiplier(v);
#endif

        public static void SetBrake(LandVehicle v, float value)
        {
#if IL2CPP
            v.BrakeForceMultiplier = value;
#else
            BrakeForceMultiplier(v) = value;
#endif
        }

        public static float GetDownforce(LandVehicle v)
#if IL2CPP
            => v.downforce;
#else
            => Downforce(v);
#endif

        public static void SetDownforce(LandVehicle v, float value)
        {
#if IL2CPP
            v.downforce = value;
#else
            Downforce(v) = value;
#endif
        }

        /// <summary>The codes of every vehicle the game can spawn, for the log.</summary>
        public static string KnownCodes()
        {
            if (!NetworkSingleton<VehicleManager>.InstanceExists) return "(vehicle manager not ready)";
            var codes = new System.Collections.Generic.List<string>();
            foreach (LandVehicle prefab in NetworkSingleton<VehicleManager>.Instance.VehiclePrefabs) codes.Add(prefab.VehicleCode);
            return string.Join(", ", codes);
        }

        /// <summary>Every vehicle in the world right now.</summary>
        public static LandVehicle[] AllVehicles()
        {
            var found = UnityEngine.Object.FindObjectsOfType<LandVehicle>();
            var list = new System.Collections.Generic.List<LandVehicle>();
            foreach (LandVehicle v in found) list.Add(v);
            return list.ToArray();
        }

        /// <summary>A vehicle's handling and body on one line, for the verbose log.</summary>
        public static string Describe(LandVehicle v)
        {
            WheelCollider wheel = v.wheels.Count > 0 ? v.wheels[0].wheelCollider : null;
            string wheels = wheel == null ? "no wheels" : v.wheels.Count + " wheels, side grip " + wheel.sidewaysFriction.stiffness
                + ", fwd grip " + wheel.forwardFriction.stiffness + ", spring " + wheel.suspensionSpring.spring + "/" + wheel.suspensionSpring.damper;
#if IL2CPP
            string extra = ", gearing " + v.diffGearing + ", brake " + Curve(v.brakeForce) + ", handbrake " + v.handBrakeForce + ", reverse " + v.reverseMultiplier;
#else
            string extra = ", gearing " + DiffGearing(v) + ", brake " + Curve(BrakeForce(v)) + ", handbrake " + HandBrakeForce(v) + ", reverse " + ReverseMultiplier(v);
#endif
            return v.VehicleCode + ": mass " + v.Rb.mass + ", torque " + Curve(GetTorque(v)) + ", top speed " + v.TopSpeed
                + ", steer " + GetSteerAngle(v) + " @ " + GetSteerRate(v) + "/s, brake x" + GetBrake(v) + ", downforce " + GetDownforce(v)
                + ", drag " + v.Rb.drag + extra + ", " + wheels;
        }

        private static string Curve(AnimationCurve curve)
        {
            if (curve == null) return "none";
            var text = new System.Text.StringBuilder("[");
            foreach (Keyframe key in curve.keys) text.Append(key.time).Append(':').Append(key.value).Append(' ');
            return text.Append(']').ToString();
        }

        /// <summary>The vehicle this storage belongs to (the trunk), or null for any other storage.</summary>
        public static LandVehicle VehicleOf(StorageEntity storage) => storage.GetComponentInParent<LandVehicle>();
    }
}
