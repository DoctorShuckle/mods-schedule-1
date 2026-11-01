using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace ConfigurableSkateboard.Tests
{
    /// <summary>
    /// A read-only view of the game's DLLs from lib/. Nothing in them is loaded into this process
    /// or run: MetadataLoadContext only reads the type and member tables, so no game, Unity or
    /// Windows is needed. Each test opens one flavor and asks whether a member still exists.
    /// </summary>
    internal sealed class GameApi : IDisposable
    {
        public const BindingFlags Declared =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly MetadataLoadContext _context;
        private readonly Assembly _gameLogic;
        private readonly Assembly _fishNet;

        public string Flavor { get; }

        /// <summary>"ScheduleOne" on Mono, "Il2CppScheduleOne" on IL2CPP (the proxy assemblies add a prefix).</summary>
        public string GameNamespace { get; }

        /// <summary>"FishNet" on Mono, "Il2CppFishNet" on IL2CPP.</summary>
        public string FishNetNamespace { get; }

        public bool IsIl2Cpp => Flavor == "il2cpp";

        private GameApi(string flavor, string libDir, string loaderDir)
        {
            Flavor = flavor;
            string prefix = flavor == "il2cpp" ? "Il2Cpp" : "";
            GameNamespace = prefix + "ScheduleOne";
            FishNetNamespace = prefix + "FishNet";

            // Earlier folders win when two have a DLL of the same name: the game's own first
            // (Mono's mscorlib is a different assembly from .NET's), then MelonLoader's (the IL2CPP
            // proxies derive from classes in Il2CppInterop.Runtime), then the test runtime's.
            var paths = new List<string>();
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string dir in new[] { libDir, loaderDir, RuntimeEnvironment.GetRuntimeDirectory() })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string path in Directory.GetFiles(dir, "*.dll"))
                {
                    if (known.Add(Path.GetFileNameWithoutExtension(path))) paths.Add(path);
                }
            }

            // IL2CPP proxies use .NET primitives (System.String), Mono's DLLs use Unity's mscorlib.
            string core = flavor == "il2cpp" ? "System.Private.CoreLib" : "mscorlib";
            _context = new MetadataLoadContext(new PathAssemblyResolver(paths), core);
            _gameLogic = _context.LoadFromAssemblyPath(Path.Combine(libDir, "Assembly-CSharp.dll"));
            _fishNet = _context.LoadFromAssemblyPath(Path.Combine(libDir, FishNetNamespace.Replace("FishNet", "FishNet.Runtime") + ".dll"));
        }

        /// <summary>Opens a flavor, or skips the test when lib/ has not been filled in.</summary>
        public static GameApi Open(string flavor)
        {
            string libRoot = typeof(GameApi).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(a => a.Key == "LibDir").Value;
            string libDir = Path.Combine(libRoot, flavor);
            Skip.IfNot(File.Exists(Path.Combine(libDir, "Assembly-CSharp.dll")),
                $"lib/{flavor}/ is empty; copy the game's DLLs there (see lib/{flavor}/README.md).");
            return new GameApi(flavor, libDir, Path.Combine(libRoot, "melonloader-" + flavor));
        }

        public Type GameType(string name) => Find(_gameLogic, GameNamespace + "." + name);

        public Type FishNetType(string name) => Find(_fishNet, FishNetNamespace + "." + name);

        private static Type Find(Assembly assembly, string fullName)
            => assembly.GetType(fullName) ?? throw new Xunit.Sdk.XunitException($"Type {fullName} is gone from {assembly.GetName().Name}.");

        /// <summary>The CLR type of a field or property, searching base classes too; null if neither exists.</summary>
        public static Type ValueType(Type type, string name, bool isStatic)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
                | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            for (Type t = type; t != null; t = t.BaseType)
            {
                PropertyInfo property = t.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetMethod != null) return property.PropertyType;
                FieldInfo field = t.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.FieldType;
            }
            return null;
        }

        /// <summary>A public instance method found on the type or any base class, matched by parameter types.</summary>
        public static MethodInfo PublicMethod(Type type, string name, params string[] parameterTypeNames)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                foreach (MethodInfo method in t.GetMethods(Declared).Where(m => m.IsPublic && !m.IsStatic && m.Name == name))
                {
                    if (method.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(parameterTypeNames)) return method;
                }
            }
            return null;
        }

        public void Dispose() => _context.Dispose();
    }
}
