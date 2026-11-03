using System.Linq;
using System.Reflection;
using Xunit;

namespace TeleportToProperties.Tests
{
    /// <summary>
    /// Early warning for game updates. The mod calls and reads game members by name, which a compiler
    /// only checks against the DLLs it was built with. These tests read the game DLLs in lib/ and fail
    /// if a member the mod uses has gone, so after a game update: refresh lib/, run the tests.
    /// They check that members exist, not that the game still behaves as the mod assumes.
    /// </summary>
    public class GameApiCanaryTests
    {
        public static TheoryData<string> Flavors => new TheoryData<string> { "mono", "il2cpp" };

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_property_lists_and_fields_the_mod_reads_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type property = game.GameType("Property.Property");
            System.Type business = game.GameType("Property.Business");
            Assert.True(property.IsAssignableFrom(business) || business.BaseType?.Name == "Property");

            Assert.Equal("List`1", GameApi.ValueType(property, "OwnedProperties", isStatic: true)?.Name);
            Assert.Equal("List`1", GameApi.ValueType(business, "OwnedBusinesses", isStatic: true)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(property, "IsOwned", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(property, "PropertyName", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(property, "PropertyCode", isStatic: false)?.Name);
            Assert.Equal("Transform", GameApi.ValueType(property, "SpawnPoint", isStatic: false)?.Name);
            Assert.Equal("Transform", GameApi.ValueType(property, "InteriorSpawnPoint", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_teleport_call_and_player_members_the_mod_uses_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // Called with all three arguments, so a change to the optional one is caught too.
            System.Type movement = game.GameType("PlayerScripts.PlayerMovement");
            Assert.Contains(movement.GetMethods(GameApi.Declared),
                m => m.IsPublic && m.Name == "Teleport"
                     && m.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(new[] { "Vector3", "Quaternion", "Boolean" }));

            System.Type player = game.GameType("PlayerScripts.Player");
            Assert.Equal("Player", GameApi.ValueType(player, "Local", isStatic: true)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(player, "IsInVehicle", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_input_guards_the_mod_reads_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            Assert.Equal("Boolean", GameApi.ValueType(game.GameType("GameInput"), "IsTyping", isStatic: true)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(game.GameType("UI.PauseMenu"), "IsPaused", isStatic: false)?.Name);
        }
    }
}
