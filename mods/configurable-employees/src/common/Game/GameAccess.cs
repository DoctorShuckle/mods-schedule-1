using System.Collections.Generic;
using UnityEngine;
#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

namespace ConfigurableEmployees
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
        // IL2CPP proxies are wrappers: `is` and `as` only see the wrapper's own class, so a base-type
        // reference to a Chemist must be asked with TryCast. On Mono these are ordinary casts.
#if IL2CPP
        internal static T As<T>(object value) where T : Il2CppObjectBase => (value as Il2CppObjectBase)?.TryCast<T>();
#else
        internal static T As<T>(object value) where T : class => value as T;
#endif

        /// <summary>A stable identity for a game object: its native address on IL2CPP, its reference hash on Mono.</summary>
        internal static long IdOf(object value)
        {
#if IL2CPP
            return ((Il2CppObjectBase)value).Pointer.ToInt64();
#else
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
#endif
        }

        public static Employee AsEmployee(NPC npc) => As<Employee>(npc);

        public static EmployeeKind KindOf(Employee employee)
        {
            if (employee == null) return EmployeeKind.Unknown;
            if (As<Botanist>(employee) != null) return EmployeeKind.Botanist;
            if (As<Chemist>(employee) != null) return EmployeeKind.Chemist;
            if (As<Packager>(employee) != null) return EmployeeKind.Packager;
            if (As<Cleaner>(employee) != null) return EmployeeKind.Cleaner;
            return EmployeeKind.Unknown;
        }

        public static EEmployeeType TypeOf(EmployeeKind kind)
        {
            switch (kind)
            {
                case EmployeeKind.Botanist: return EEmployeeType.Botanist;
                case EmployeeKind.Chemist: return EEmployeeType.Chemist;
                case EmployeeKind.Packager: return EEmployeeType.Handler;
                default: return EEmployeeType.Cleaner;
            }
        }

        /// <summary>The template every new hire is cloned from; the Fixer also reads the signing fee from it.</summary>
        public static Employee Prefab(EmployeeManager manager, EmployeeKind kind) => manager.GetEmployeePrefab(TypeOf(kind));

        /// <summary>A copy of every employee in the world, so callers may change the game's list while looping.</summary>
        public static List<Employee> LiveEmployees()
        {
            var result = new List<Employee>();
            EmployeeManager manager = NetworkSingleton<EmployeeManager>.Instance;
            if (manager == null || manager.AllEmployees == null) return result;

            for (int i = 0; i < manager.AllEmployees.Count; i++)
            {
                Employee employee = manager.AllEmployees[i];
                if (employee != null) result.Add(employee);
            }
            return result;
        }

        public static EmployeeManager Manager => NetworkSingleton<EmployeeManager>.Instance;

        // ---- limits that live on an employee's management configuration -------------------------

        // Each employee class declares its own Configuration (the shared base type has none), and each
        // returns the general EntityConfiguration, so it is cast to the specific one.

        /// <summary>The "how many things can I be assigned" list of this employee's configuration, if it exists yet.</summary>
        public static ObjectListField AssignedList(Employee employee, EmployeeKind kind)
        {
            switch (kind)
            {
                case EmployeeKind.Botanist: return As<BotanistConfiguration>(As<Botanist>(employee)?.Configuration)?.Assigns;
                case EmployeeKind.Chemist: return As<ChemistConfiguration>(As<Chemist>(employee)?.Configuration)?.Stations;
                case EmployeeKind.Packager: return As<PackagerConfiguration>(As<Packager>(employee)?.Configuration)?.Stations;
                case EmployeeKind.Cleaner: return As<CleanerConfiguration>(As<Cleaner>(employee)?.Configuration)?.Bins;
                default: return null;
            }
        }

        public static RouteListField Routes(Employee employee)
            => As<PackagerConfiguration>(As<Packager>(employee)?.Configuration)?.Routes;

        public static Packager AsPackager(Employee employee) => As<Packager>(employee);

        public static Botanist AsBotanist(Employee employee) => As<Botanist>(employee);

        public static void SetWalkSpeed(Employee employee, float speed)
        {
            if (employee.Movement == null) return;
            employee.Movement.SpeedController.SetDefaultSpeed(speed);
        }

        // ---- fixer -----------------------------------------------------------------------------

        public static int LifetimeEmployeesRecruited()
            => Mathf.RoundToInt(NetworkSingleton<VariableDatabase>.Instance.GetValue<float>("LifetimeEmployeesRecruited"));

        // ---- properties ------------------------------------------------------------------------

        /// <summary>A copy of every property the game has loaded, so callers may change the game's list while looping.</summary>
        public static List<GameProperty> AllProperties()
        {
            var result = new List<GameProperty>();
            for (int i = 0; i < GameProperty.Properties.Count; i++)
            {
                GameProperty property = GameProperty.Properties[i];
                if (property != null) result.Add(property);
            }
            return result;
        }

        public static int IdlePointCount(GameProperty property) => property.EmployeeIdlePoints == null ? 0 : property.EmployeeIdlePoints.Length;

        /// <summary>
        /// Adds idle points until the property has <paramref name="capacity"/> of them, each a new
        /// empty object beside an existing one. They are staggered sideways so employees do not all
        /// stand on one spot. With no idle point to copy from there is nothing safe to do.
        /// </summary>
        public static void EnsureIdlePoints(GameProperty property, int capacity)
        {
            int have = IdlePointCount(property);
            int toAdd = PropertyCapacity.IdlePointsToAdd(capacity, have);
            if (toAdd == 0 || have == 0) return;

            var points = new Transform[capacity];
            for (int i = 0; i < have; i++) points[i] = property.EmployeeIdlePoints[i];

            for (int i = have; i < capacity; i++)
            {
                Transform source = points[i % have];
                var created = new GameObject(property.PropertyCode + " idle point " + i);
                created.transform.SetParent(source.parent, true);
                created.transform.SetPositionAndRotation(source.position + source.right * (0.8f * (1 + i / have)), source.rotation);
                points[i] = created.transform;
            }

#if IL2CPP
            property.EmployeeIdlePoints = new Il2CppReferenceArray<Transform>(points);
#else
            property.EmployeeIdlePoints = points;
#endif
        }
    }
}
