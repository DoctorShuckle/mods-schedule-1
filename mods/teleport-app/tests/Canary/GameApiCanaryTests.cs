using System.Linq;
using System.Reflection;
using Xunit;

namespace TeleportApp.Tests
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
            Assert.Equal("List`1", GameApi.ValueType(property, "Properties", isStatic: true)?.Name);
            Assert.Equal("List`1", GameApi.ValueType(business, "Businesses", isStatic: true)?.Name);
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
            Assert.Equal("Boolean", GameApi.ValueType(game.GameType("UI.GameplayMenu"), "IsOpen", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_phone_and_popup_members_the_app_uses_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // The patch target: picked by name alone, so it must be declared once with one bool.
            System.Type phone = game.GameType("UI.Phone.Phone");
            MethodInfo setIsOpen = Assert.Single(phone.GetMethods(GameApi.Declared), m => m.Name == "SetIsOpen");
            Assert.Equal("Boolean", Assert.Single(setIsOpen.GetParameters()).ParameterType.Name);

            Assert.NotNull(game.GameType("UI.Phone.HomeScreen"));
            Assert.Contains(game.GameType("UIPanel").GetMethods(GameApi.Declared),
                m => m.IsPublic && m.Name == "AddSelectable" && m.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(new[] { "UISelectable" }));
            Assert.Contains(game.GameType("UI.GameplayMenu").GetMethods(GameApi.Declared),
                m => m.IsPublic && m.Name == "Close" && m.GetParameters().Length == 0);

            // The list popup: opened by name with boxed arguments, so the shapes matter.
            Assert.Contains(game.GameType("UIScreenManager").GetMethods(GameApi.Declared),
                m => m.IsPublic && m.Name == "OpenPopupScreen" && m.GetParameters().Length == 2);
            System.Type menu = game.GameType("UIPopupScreen_ContextMenu");
            System.Type option = menu.GetNestedType("ContextMenuOption");
            Assert.NotNull(option);
            Assert.Contains(option.GetConstructors(), c => c.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(new[] { "Int32", "String", "Action" }));
            Assert.NotNull(menu.GetNestedType("AnchorType")?.GetField("Center"));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_dealer_members_the_mod_reads_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type dealer = game.GameType("Economy.Dealer");
            Assert.Equal("List`1", GameApi.ValueType(dealer, "AllPlayerDealers", isStatic: true)?.Name);
            Assert.Equal("Boolean", GameApi.ValueType(dealer, "IsRecruited", isStatic: false)?.Name);
            // Dealers are NPCs: the name and id come from the base class.
            Assert.Equal("String", GameApi.ValueType(dealer, "FullName", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(dealer, "ID", isStatic: false)?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void The_vehicle_members_the_mod_reads_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            System.Type manager = game.GameType("Vehicles.VehicleManager");
            Assert.Equal("List`1", GameApi.ValueType(manager, "AllVehicles", isStatic: false)?.Name);

            System.Type vehicle = game.GameType("Vehicles.LandVehicle");
            Assert.Equal("Boolean", GameApi.ValueType(vehicle, "IsPlayerOwned", isStatic: false)?.Name);
            Assert.Equal("String", GameApi.ValueType(vehicle, "VehicleName", isStatic: false)?.Name);
            Assert.Equal("Guid", GameApi.ValueType(vehicle, "GUID", isStatic: false)?.Name);
            Assert.Equal("BoxCollider", GameApi.ValueType(vehicle, "boundingBox", isStatic: false)?.Name);
            Assert.Equal("Vector3", GameApi.ValueType(vehicle, "BoundingBoxDimensions", isStatic: false)?.Name);
            Assert.NotNull(GameApi.ValueType(vehicle, "OwnedColor", isStatic: false));
            Assert.Contains(game.GameType("Vehicles.Modification.VehicleColors").GetMethods(GameApi.Declared),
                m => m.IsPublic && m.Name == "GetColorName" && m.ReturnType.Name == "String");
        }
    }
}
