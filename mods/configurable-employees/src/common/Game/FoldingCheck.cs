using System.Collections.Generic;

namespace ConfigurableEmployees
{
    /// <summary>
    /// A startup diagnostic for the botanist's task times. See <see cref="Log"/>.
    /// </summary>
    internal static class FoldingCheck
    {
        /// <summary>
        /// IL2CPP only: logs the native address each task's GetActionDuration runs at. Several tasks
        /// returning the same constant are compiled to one shared function ("identical code
        /// folding"); a detour on that function hits all of them and, being tiny, can overrun into its
        /// neighbour. Equal addresses in this log are what confirms it, and is why the mod does not
        /// patch these methods. On Mono there is nothing to see (the JIT compiles each separately).
        /// </summary>
        public static void Log()
        {
#if IL2CPP
            string[] names =
            {
                "AddSoilToGrowContainerBehaviour", "WaterPotBehaviour", "ApplyAdditiveToGrowContainerBehaviour",
                "SowSeedInPotBehaviour", "HarvestPotBehaviour", "HarvestMushroomBedBehaviour", "ApplySpawnToMushroomBedBehaviour",
            };
            var distinct = new HashSet<long>();
            foreach (string name in names)
            {
                System.Type type = typeof(Behaviours.GrowContainerBehaviour).Assembly.GetType(typeof(Behaviours.GrowContainerBehaviour).Namespace + "." + name);
                System.Reflection.FieldInfo field = null;
                if (type != null)
                    foreach (System.Reflection.FieldInfo candidate in type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                        if (candidate.Name.StartsWith("NativeMethodInfoPtr_GetActionDuration")) { field = candidate; break; }
                if (field == null) { ModLog.Info("GetActionDuration address: " + name + " not found."); continue; }

                // An Il2CppMethodInfo starts with the pointer to the method's compiled code.
                System.IntPtr methodInfo = (System.IntPtr)field.GetValue(null);
                long code = System.Runtime.InteropServices.Marshal.ReadIntPtr(methodInfo).ToInt64();
                distinct.Add(code);
                ModLog.Info("GetActionDuration address: " + name + " = 0x" + code.ToString("X"));
            }
            ModLog.Info("GetActionDuration: " + names.Length + " tasks run at " + distinct.Count + " distinct native function(s)" +
                (distinct.Count < names.Length ? " (folded: detouring them is unsafe)." : "."));
#endif
        }
    }
}
