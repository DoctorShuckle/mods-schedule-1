using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    /// <summary>
    /// Early warning for game updates. The mod finds game members by name at runtime (Harmony
    /// patch targets, the fields it writes), and a compiler cannot check those. These tests read
    /// the game DLLs in lib/ and fail if anything the mod depends on has gone, so after a game
    /// update: refresh lib/, run the tests, and you know before launching the game.
    /// They check that members exist, not that the game still behaves as the mod assumes.
    /// </summary>
    public class GameApiCanaryTests
    {
        public static TheoryData<string> Flavors => new TheoryData<string> { "mono", "il2cpp" };

        private static readonly string[] BotanistBehaviours =
        {
            "AddSoilToGrowContainerBehaviour", "WaterPotBehaviour", "ApplyAdditiveToGrowContainerBehaviour",
            "SowSeedInPotBehaviour", "HarvestPotBehaviour", "HarvestMushroomBedBehaviour", "ApplySpawnToMushroomBedBehaviour",
        };

        private static readonly string[] WorkerBehaviours =
        {
            "StartChemistryStationBehaviour", "StartLabOvenBehaviour", "FinishLabOvenBehaviour", "StartMixingStationBehaviour",
            "StartCauldronBehaviour", "PackagingStationBehaviour", "BrickPressBehaviour", "MoveItemBehaviour",
            "StartDryingRackBehaviour", "UseSpawnStationBehaviour",
        };

        /// <summary>The one method of that name declared on the type: Harmony picks patch targets by name alone.</summary>
        private static MethodInfo OnlyMethod(Type type, string name)
            => Assert.Single(type.GetMethods(GameApi.Declared), m => m.Name == name);

        private static string FieldType(Type type, string name, bool isStatic = false) => GameApi.ValueType(type, name, isStatic)?.Name;

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Patch_targets_with_plain_names_exist_once(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            OnlyMethod(game.GameType("Employees.EmployeeManager"), "Awake");
            OnlyMethod(game.GameType("Employees.Employee"), "Start");
            OnlyMethod(game.GameType("Property.Property"), "Awake");
            OnlyMethod(game.GameType("UI.Management.RouteListFieldUI"), "Start");
            OnlyMethod(game.GameType("Management.ManagementInterface"), "Open");
            OnlyMethod(game.GameType("Management.ManagementInterface"), "Close");

            // NPC.InitializeNPC must stay the one that applies the NPC data, and Employee must not
            // override it (the patch targets the base method).
            OnlyMethod(game.GameType("NPCs.NPC"), "InitializeNPC");
            Assert.DoesNotContain(game.GameType("Employees.Employee").GetMethods(GameApi.Declared), m => m.Name == "InitializeNPC");
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Fixer_fee_and_scroll_patch_targets_have_the_expected_shape(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            MethodInfo fee = OnlyMethod(game.GameType("NPCs.CharacterClasses.Fixer"), "GetAdditionalSigningFee");
            Assert.True(fee.IsStatic);
            Assert.Equal("Single", fee.ReturnType.Name);
            Assert.Empty(fee.GetParameters());

            // A static property: Harmony patches its getter.
            PropertyInfo scroll = game.GameType("GameInput").GetProperty("MouseScrollDelta", BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(scroll?.GetMethod);
            Assert.Equal("Single", scroll.PropertyType.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Every_botanist_task_reports_its_duration_as_a_float(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            foreach (string name in BotanistBehaviours)
            {
                MethodInfo duration = OnlyMethod(game.GameType("NPCs.Behaviour." + name), "GetActionDuration");
                Assert.True(duration.ReturnType.Name == "Single", name + ".GetActionDuration should return a float");
                Assert.Empty(duration.GetParameters());
            }
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Task_start_and_stop_hooks_exist_on_the_shared_base(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type grow = game.GameType("NPCs.Behaviour.GrowContainerBehaviour");

            // The task times patch these two (the work speed is changed between them). They must be
            // declared on the base: the seed task overrides them and calls base, the others do not
            // override them, so patching the base is what covers every task.
            foreach (string name in new[] { "OnStartPerformAction", "OnStopPerformAction" })
            {
                MethodInfo hook = OnlyMethod(grow, name);
                Assert.Empty(hook.GetParameters());
                Assert.Equal("Void", hook.ReturnType.Name);
            }

            // The trace log names the worker from these (on NPC, which Employee inherits).
            foreach (string member in new[] { "ID", "FirstName", "LastName" })
                Assert.Equal("String", FieldType(game.GameType("NPCs.NPC"), member));

            // The work speed the mod holds lives here, and the game divides each duration by it.
            Assert.Equal("FloatStack", FieldType(game.GameType("Employees.Employee"), "WorkSpeedController"));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Chemist_and_packager_task_hooks_and_behaviours_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // The worker task times patch the four lifecycle methods of the base class; the game's
            // behaviours override them but call base, so each must stay declared once on Behaviour.
            Type behaviour = game.GameType("NPCs.Behaviour.Behaviour");
            foreach (string name in new[] { "Activate", "Resume", "Deactivate", "Pause" })
            {
                MethodInfo method = OnlyMethod(behaviour, name);
                Assert.Empty(method.GetParameters());
                Assert.Equal("Void", method.ReturnType.Name);
            }

            // The behaviours the lookup recognises, and the Npc they act through.
            foreach (string name in WorkerBehaviours)
                Assert.NotNull(game.GameType("NPCs.Behaviour." + name));
            Assert.NotNull(GameApi.ValueType(behaviour, "Npc", false));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Property_values_the_mod_reads_and_writes_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type property = game.GameType("Property.Property");

            // Capacity is read and written on each property, and the sweep after the scene loads walks
            // the game's static list of them (the Awake patch cannot see every kind of property).
            Assert.Equal("Int32", FieldType(property, "EmployeeCapacity"));
            Assert.Equal("String", FieldType(property, "PropertyCode"));
            Assert.NotNull(GameApi.ValueType(property, "Properties", true));
            Assert.NotNull(GameApi.ValueType(property, "EmployeeIdlePoints", false));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Misting_still_shares_the_watering_duration(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type mist = game.GameType("NPCs.Behaviour.MistMushroomBedBehaviour");

            // The water setting is documented as covering misting; that only holds while the mist
            // behaviour inherits the water one's GetActionDuration instead of declaring its own.
            Assert.Equal("WaterPotBehaviour", mist.BaseType?.Name);
            Assert.DoesNotContain(mist.GetMethods(GameApi.Declared), m => m.Name == "GetActionDuration");
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Employee_values_the_mod_writes_exist(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type employee = game.GameType("Employees.Employee");

            Assert.Equal("Single", FieldType(employee, "DailyWage"));
            Assert.Equal("Single", FieldType(employee, "SigningFee"));

            Assert.Equal("Int32", FieldType(game.GameType("Employees.Packager"), "MaxAssignedStations"));
            Assert.Equal("Single", FieldType(game.GameType("Employees.Packager"), "PackagingSpeedMultiplier"));
            Assert.Equal("Int32", FieldType(game.GameType("Employees.Botanist"), "MaxAssignedPots"));

            string[] values = Enum.GetNames(game.GameType("Employees.EEmployeeType"));
            Assert.Contains("Botanist", values);
            Assert.Contains("Handler", values);
            Assert.Contains("Chemist", values);
            Assert.Contains("Cleaner", values);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Manager_exposes_prefabs_and_every_employee(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type manager = game.GameType("Employees.EmployeeManager");

            Type all = GameApi.ValueType(manager, "AllEmployees", isStatic: false);
            Assert.Equal("List`1", all?.Name);
            Assert.Equal("Employee", all.GetGenericArguments().Single().Name);

            MethodInfo prefab = GameApi.PublicMethod(manager, "GetEmployeePrefab", "EEmployeeType");
            Assert.Equal("Employee", prefab?.ReturnType.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Walk_speed_is_set_through_the_speed_controller(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            Type movement = GameApi.ValueType(game.GameType("NPCs.NPC"), "Movement", isStatic: false);
            Assert.Equal("NPCMovement", movement?.Name);

            Type controller = GameApi.ValueType(movement, "SpeedController", isStatic: false);
            Assert.Equal("NPCSpeedController", controller?.Name);
            Assert.NotNull(GameApi.PublicMethod(controller, "SetDefaultSpeed", "Single"));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Each_employee_has_the_configuration_list_the_mod_resizes(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // (employee class, its configuration class, the list field, the list's type)
            var lists = new[]
            {
                ("Botanist", "BotanistConfiguration", "Assigns"),
                ("Chemist", "ChemistConfiguration", "Stations"),
                ("Packager", "PackagerConfiguration", "Stations"),
                ("Cleaner", "CleanerConfiguration", "Bins"),
            };
            foreach ((string employee, string configuration, string list) in lists)
            {
                Assert.Equal("EntityConfiguration", FieldType(game.GameType("Employees." + employee), "Configuration"));
                Assert.Equal("ObjectListField", FieldType(game.GameType("Management." + configuration), list));
            }

            Assert.Equal("RouteListField", FieldType(game.GameType("Management.PackagerConfiguration"), "Routes"));
            Assert.Equal("Int32", FieldType(game.GameType("Management.ObjectListField"), "MaxItems"));
            Assert.Equal("Int32", FieldType(game.GameType("Management.RouteListField"), "MaxRoutes"));
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Property_capacity_and_idle_points_can_be_read_and_set(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type property = game.GameType("Property.Property");

            Assert.Equal("Int32", FieldType(property, "EmployeeCapacity"));
            Assert.Equal("String", FieldType(property, "PropertyCode"));

            // Transform[] on Mono, Il2CppReferenceArray<Transform> on IL2CPP.
            Type points = GameApi.ValueType(property, "EmployeeIdlePoints", isStatic: false);
            Assert.NotNull(points);
            Assert.Equal(game.IsIl2Cpp ? "Il2CppReferenceArray`1" : "Transform[]", points.Name);

            Type all = GameApi.ValueType(property, "Properties", isStatic: true);
            Assert.Equal("List`1", all?.Name);
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Route_list_screen_has_the_parts_the_mod_extends(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);
            Type ui = game.GameType("UI.Management.RouteListFieldUI");

            Type rows = GameApi.ValueType(ui, "RouteEntries", isStatic: false);
            Assert.NotNull(rows);
            Assert.Equal(game.IsIl2Cpp ? "Il2CppReferenceArray`1" : "RouteEntryUI[]", rows.Name);

            Assert.Equal("Button", FieldType(ui, "AddButton"));
            Assert.Equal("String", FieldType(ui, "FieldText"));
            game.GameType("UI.Management.RouteEntryUI");
        }

        [SkippableTheory]
        [MemberData(nameof(Flavors))]
        public void Hire_count_can_be_read(string flavor)
        {
            using GameApi game = GameApi.Open(flavor);

            // VariableDatabase.GetValue<T>(string): the Fixer's own source for the hire count.
            MethodInfo get = game.GameType("Variables.VariableDatabase").GetMethods(GameApi.Declared)
                .FirstOrDefault(m => m.Name == "GetValue" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
            Assert.NotNull(get);

            Assert.NotNull(GameApi.ValueType(game.GameType("DevUtilities.NetworkSingleton`1"), "Instance", isStatic: true));
        }
    }
}
