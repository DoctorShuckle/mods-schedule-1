using System;
using System.Collections.Generic;

namespace ConfigurableEmployees
{
    /// <summary>
    /// Writes the configured values onto employees and onto the prefabs new hires are cloned
    /// from. Every setting is absolute and unset ones change nothing, so applying twice is the
    /// same as applying once and the same code serves prefabs, new hires and live reloads.
    /// </summary>
    internal static class EmployeeTuning
    {
        private static readonly HashSet<EmployeeKind> LoggedDefaults = new HashSet<EmployeeKind>();

        /// <summary>
        /// Called when the game creates its employee manager, before anyone is hired or loaded.
        /// Logs what the game ships with (once) so the player knows what a setting would replace.
        /// </summary>
        public static void ApplyToPrefabs(EmployeeManager manager)
        {
            foreach (EmployeeKind kind in new[] { EmployeeKind.Botanist, EmployeeKind.Chemist, EmployeeKind.Packager, EmployeeKind.Cleaner })
            {
                Employee prefab = GameAccess.Prefab(manager, kind);
                if (prefab == null) continue;

                if (LoggedDefaults.Add(kind))
                    ModLog.Info("Game values for " + kind + ": wage " + prefab.DailyWage + ", signing fee " + prefab.SigningFee + ".");
                ApplyFields(prefab, kind);
            }
        }

        /// <summary>A newly started employee, or one being re-tuned after a config change.</summary>
        public static void ApplyToEmployee(Employee employee)
        {
            EmployeeKind kind = GameAccess.KindOf(employee);
            if (kind == EmployeeKind.Unknown) return;

            ApplyFields(employee, kind);
            ApplyLimits(employee, kind);
            ApplyWalkSpeed(employee, kind);
        }

        /// <summary>Preferences changed while the game runs: re-tune everyone already hired.</summary>
        public static void ReapplyAll()
        {
            EmployeeManager manager = GameAccess.Manager;
            if (manager == null) return;

            ApplyToPrefabs(manager);
            foreach (Employee employee in GameAccess.LiveEmployees()) ApplyToEmployee(employee);
        }

        public static void ApplyWalkSpeed(Employee employee, EmployeeKind kind)
        {
            EmployeeSettings settings = ModConfig.For(kind);
            if (settings == null) return;

            // The game's own speed is not known here, so "unset" comes back as the sentinel.
            float speed = Setting.ResolveSpeed(settings.WalkSpeed, Setting.Unset);
            if (speed <= 0f) return;

            GameAccess.SetWalkSpeed(employee, speed);
            ModLog.Verbose(kind + " walk speed set to " + speed + ".");
        }

        // Fields that clones inherit from the prefab, so they are written to both.
        private static void ApplyFields(Employee employee, EmployeeKind kind)
        {
            EmployeeSettings settings = ModConfig.For(kind);
            if (settings == null) return;

            employee.DailyWage = Setting.ResolveMoney(settings.DailyWage, employee.DailyWage);
            employee.SigningFee = Setting.ResolveMoney(settings.SigningFee, employee.SigningFee);
            ModLog.Verbose(kind + " wage " + employee.DailyWage + ", signing fee " + employee.SigningFee + ".");

            if (kind == EmployeeKind.Packager)
            {
                Packager packager = GameAccess.AsPackager(employee);
                if (packager != null)
                {
                    packager.MaxAssignedStations = Setting.ResolveCount(settings.MaxAssigned, packager.MaxAssignedStations, Setting.MaxAssigned);
                    packager.PackagingSpeedMultiplier = Setting.ResolveMultiplier(ModConfig.PackagerSpeedMultiplier, packager.PackagingSpeedMultiplier);
                }
            }
            else if (kind == EmployeeKind.Botanist)
            {
                Botanist botanist = GameAccess.AsBotanist(employee);
                if (botanist != null)
                    botanist.MaxAssignedPots = Setting.ResolveCount(settings.MaxAssigned, botanist.MaxAssignedPots, Setting.MaxAssigned);
            }
        }

        // The game's configuration objects set their own limits when they are built, from fixed
        // numbers (chemist, cleaner, routes) or from the prefab fields above (packager, botanist
        // stations). Writing the limit onto the built configuration covers all of them.
        private static void ApplyLimits(Employee employee, EmployeeKind kind)
        {
            EmployeeSettings settings = ModConfig.For(kind);
            try
            {
                ObjectListField list = GameAccess.AssignedList(employee, kind);
                if (list != null)
                {
                    list.MaxItems = Setting.ResolveCount(settings.MaxAssigned, list.MaxItems, Setting.MaxAssigned);
                    ModLog.Verbose(kind + " can be assigned " + list.MaxItems + ".");
                }

                RouteListField routes = kind == EmployeeKind.Packager ? GameAccess.Routes(employee) : null;
                if (routes != null)
                {
                    routes.MaxRoutes = Setting.ResolveCount(ModConfig.PackagerMaxRoutes, routes.MaxRoutes, RouteRows.Max);
                    ModLog.Verbose("Packager can have " + routes.MaxRoutes + " routes.");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not set limits for a " + kind + ": " + ex);
            }
        }
    }
}
