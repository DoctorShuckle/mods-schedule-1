#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes;
#endif

namespace ModShared
{
    /// <summary>
    /// Type tests and identity for game objects that work the same on both runtimes. IL2CPP proxies
    /// are wrappers: `is` and `as` only see the wrapper's own class, so a reference typed as a base
    /// class or an interface must be asked with TryCast, and two wrappers of one game object are
    /// told apart by their native pointer. On Mono these are ordinary casts and reference checks.
    /// Each mod's GlobalUsings.cs has `global using static ModShared.Interop;`, so call As, Same
    /// and IdOf directly.
    /// </summary>
    internal static class Interop
    {
#if IL2CPP
        /// <summary>The object as a T, or null when it is not one.</summary>
        public static T As<T>(object value) where T : Il2CppObjectBase => (value as Il2CppObjectBase)?.TryCast<T>();

        /// <summary>True when both are the same game object (both non-null).</summary>
        public static bool Same(object a, object b)
            => a != null && b != null && ((Il2CppObjectBase)a).Pointer == ((Il2CppObjectBase)b).Pointer;

        /// <summary>A stable identity for a game object: its native address on IL2CPP, its reference hash on Mono.</summary>
        public static long IdOf(object value) => ((Il2CppObjectBase)value).Pointer.ToInt64();
#else
        /// <summary>The object as a T, or null when it is not one.</summary>
        public static T As<T>(object value) where T : class => value as T;

        /// <summary>True when both are the same game object (both non-null).</summary>
        public static bool Same(object a, object b) => a != null && b != null && ReferenceEquals(a, b);

        /// <summary>A stable identity for a game object: its native address on IL2CPP, its reference hash on Mono.</summary>
        public static long IdOf(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
#endif
    }
}
