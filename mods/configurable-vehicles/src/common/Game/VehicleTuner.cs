using System;
using System.Collections.Generic;

namespace ConfigurableVehicles
{
    /// <summary>
    /// Applies a vehicle's settings to the running vehicle. The game's own values are remembered the
    /// first time a vehicle is seen, and every apply computes from those, so applying twice (a spawn,
    /// then a saved config) never compounds, and clearing a setting puts the game's value back.
    /// </summary>
    internal static class VehicleTuner
    {
        private sealed class Original
        {
            public float Mass, SteerAngle, SteerRate, Brake, Downforce, TopSpeed;
            public Keyframe[] Torque;
            public float[] ForwardGrip, SideGrip;
        }

        private static readonly Dictionary<int, Original> Originals = new Dictionary<int, Original>();

        /// <summary>Called as a vehicle starts, and again for every vehicle when the config is saved.</summary>
        public static void Apply(LandVehicle v)
        {
            VehicleOverrides settings = ModConfig.For(v.VehicleCode);
            int id = v.GetInstanceID();
            bool known = Originals.TryGetValue(id, out Original original);

            // A vehicle that was never touched and has nothing set stays entirely as the game made it.
            if (!known && (settings == null || settings.HasNoHandling)) return;

            if (!known)
            {
                original = Remember(v);
                Originals[id] = original;
                ModLog.Verbose("Game values " + GameAccess.Describe(v));
            }
            settings = settings ?? new VehicleOverrides(0, 0, 0, 0, 0, 0, 0, 0, 0);

            v.Rb.mass = settings.Mass > 0f ? settings.Mass : original.Mass;
            v.TopSpeed = settings.TopSpeed > 0f ? settings.TopSpeed : original.TopSpeed;
            GameAccess.SetSteerAngle(v, settings.SteerAngle > 0f ? settings.SteerAngle : original.SteerAngle);
            GameAccess.SetSteerRate(v, settings.SteerRate > 0f ? settings.SteerRate : original.SteerRate);
            GameAccess.SetBrake(v, settings.Brake > 0f ? original.Brake * settings.Brake : original.Brake);
            GameAccess.SetDownforce(v, settings.Downforce > 0f ? settings.Downforce : original.Downforce);
            GameAccess.SetTorque(v, ScaledTorque(original.Torque, settings.Power > 0f ? settings.Power : 1f));

            float grip = settings.Grip > 0f ? settings.Grip : 1f;
            for (int i = 0; i < v.wheels.Count && i < original.ForwardGrip.Length; i++)
            {
                WheelCollider wheel = v.wheels[i].wheelCollider;
                WheelFrictionCurve forward = wheel.forwardFriction;
                forward.stiffness = original.ForwardGrip[i] * grip;
                wheel.forwardFriction = forward;
                WheelFrictionCurve side = wheel.sidewaysFriction;
                side.stiffness = original.SideGrip[i] * grip;
                wheel.sidewaysFriction = side;
            }

            ModLog.Verbose("Tuned " + GameAccess.Describe(v));
        }

        /// <summary>Re-applies the settings to every vehicle already in the world (after the config changed).</summary>
        public static void ReapplyAll()
        {
            try
            {
                foreach (LandVehicle v in GameAccess.AllVehicles())
                {
                    try { Apply(v); }
                    catch (Exception ex) { ModLog.WarnOnce("Could not tune a vehicle: " + ex); }
                }
            }
            catch (Exception ex)
            {
                ModLog.WarnOnce("Could not look up the vehicles: " + ex);
            }
        }

        private static Original Remember(LandVehicle v)
        {
            var original = new Original
            {
                Mass = v.Rb.mass,
                TopSpeed = v.TopSpeed,
                SteerAngle = GameAccess.GetSteerAngle(v),
                SteerRate = GameAccess.GetSteerRate(v),
                Brake = GameAccess.GetBrake(v),
                Downforce = GameAccess.GetDownforce(v),
                Torque = GameAccess.GetTorque(v).keys,
                ForwardGrip = new float[v.wheels.Count],
                SideGrip = new float[v.wheels.Count],
            };
            for (int i = 0; i < v.wheels.Count; i++)
            {
                WheelCollider wheel = v.wheels[i].wheelCollider;
                original.ForwardGrip[i] = wheel.forwardFriction.stiffness;
                original.SideGrip[i] = wheel.sidewaysFriction.stiffness;
            }
            return original;
        }

        /// <summary>A new torque curve with every value and slope multiplied, so the shape is kept.</summary>
        private static AnimationCurve ScaledTorque(Keyframe[] keys, float factor)
        {
            var scaled = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.value *= factor;
                key.inTangent *= factor;
                key.outTangent *= factor;
                scaled[i] = key;
            }
            return new AnimationCurve(scaled);
        }
    }
}
