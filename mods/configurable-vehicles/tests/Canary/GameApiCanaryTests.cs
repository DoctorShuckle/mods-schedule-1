using System.Linq;
using System.Reflection;
using Xunit;

namespace ConfigurableVehicles.Tests
{
    /// <summary>
    /// Early warning for game updates. The mod patches game methods by name and reaches fields by
    /// name, which a compiler cannot check. These tests read the game DLLs in lib/ and fail if
    /// a patch target or field has gone, so after a game update: refresh lib/, run the tests, and
    /// you know before launching the game. They check that members exist, not that the game still
    /// behaves as the mod assumes.
    /// </summary>
    public class GameApiCanaryTests
    {
        public static TheoryData<string> Flavors => new TheoryData<string> { "mono", "il2cpp" };

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Patch_targets_are_declared_once_without_parameters(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // [HarmonyPatch(typeof(X), "Name") picks the method by name alone.
            foreach (var (type, method) in new[] { ("Vehicles.LandVehicle", "Start"), ("Storage.StorageEntity", "Awake") })
            {
                MethodInfo target = Assert.Single(game.GameType(type).GetMethods(GameApi.Declared), m => m.Name == method);
                Assert.Empty(target.GetParameters());
            }
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Handling_fields_exist_with_the_expected_types(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type vehicle = game.GameType("Vehicles.LandVehicle");

            foreach (string name in new[] { "maxSteeringAngle", "steerRate", "diffGearing", "handBrakeForce", "BrakeForceMultiplier", "downforce", "reverseMultiplier", "TopSpeed" })
                Assert.Equal("Single", GameApi.ValueType(vehicle, name, isStatic: false)?.Name);
            foreach (string name in new[] { "motorTorque", "brakeForce" })
                Assert.Equal("AnimationCurve", GameApi.ValueType(vehicle, name, isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Trunk_and_prefab_lookups_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type vehicle = game.GameType("Vehicles.LandVehicle");

            Assert.Equal("StorageEntity", GameApi.ValueType(vehicle, "Storage", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(vehicle, "VehicleCode", isStatic: false)?.Name);
            Assert.Equal("Int32", GameApi.ValueType(game.GameType("Storage.StorageEntity"), "SlotCount", isStatic: false)?.Name);
            Assert.Equal("Int32", GameApi.ValueType(game.GameType("Storage.StorageEntity"), "DisplayRowCount", isStatic: false)?.Name);
            Assert.Equal("Rigidbody", GameApi.ValueType(vehicle, "Rb", isStatic: false)?.Name);
            Assert.NotNull(GameApi.ValueType(vehicle, "wheels", isStatic: false));
            Assert.Equal("WheelCollider", GameApi.ValueType(game.GameType("Vehicles.Wheel"), "wheelCollider", isStatic: false)?.Name);

            System.Type manager = game.GameType("Vehicles.VehicleManager");
            Assert.NotNull(GameApi.ValueType(manager, "VehiclePrefabs", isStatic: false));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Handling_fields_are_protected_on_mono_and_public_on_il2cpp(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            System.Type vehicle = game.GameType("Vehicles.LandVehicle");

            if (game.IsIl2Cpp)
                Assert.NotNull(vehicle.GetProperty("maxSteeringAngle", BindingFlags.Instance | BindingFlags.Public));
            else
                Assert.False(vehicle.GetField("maxSteeringAngle", BindingFlags.Instance | BindingFlags.NonPublic).IsPublic);
        }
    }
}
