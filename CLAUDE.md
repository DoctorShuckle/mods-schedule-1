# Schedule I mods: guide for agents

How to create (and change) a mod in this repo. Mods are MelonLoader mods for **Schedule I** (TVGS), written
once in C# and compiled twice: **IL2CPP is the primary flavor**, **Mono** is built too. Read
[README.md](README.md) for building, scripts and licence; this file covers how mods are made.

## Ground rules

- All mod code lives in `src/common/` and compiles into both flavors. Code every mod needs (logging, the patch
  loader, number checks) lives in `lib/shared/` and is compiled into each mod, so don't copy it into a mod. Write for IL2CPP first, then make Mono work.
- Never commit game or MelonLoader files. `lib/` (except `lib/shared/`, our own code), `decompiled/`, `third-party/`, `out/`, `logs/` are gitignored.
- Defaults should make the mod do what its name says. Ship the change on by default when that is the point of the
  mod (e.g. skip-disclaimer); use neutral defaults only when the mod has no sensible one (e.g. tuning multipliers).
- Patches fail open. A broken patch must never break the game's own code path.
- Be event-driven, not polling. Never use `OnUpdate`/`OnLateUpdate`/`OnFixedUpdate`, timers, coroutines that
  loop, or "check every N seconds" logic to notice state changes: they run every frame and are wasteful. Hook the
  game's own events instead (a Harmony patch on the method that changes the state, a game event or `UnityEvent`
  subscription, `OnPreferencesLoaded`/`OnPreferencesSaved`, scene-load callbacks). If no event exists, patch the
  setter or the method that causes the change. Only fall back to per-frame work when nothing can be hooked, and
  say why in a comment and in the README.
- Don't claim a mod works in game unless it was run. Builds and tests prove only that it compiles and that the
  patched members exist. Say "not run in game" in the README's Status section.

## Repo layout

```
Directory.Build.props   RepoRoot, LibDir, ModName; output goes to out/<mod>/<flavor>/
lib/mono, lib/il2cpp                 game assemblies (each folder has a README saying what to copy in)
lib/melonloader-mono (net35), lib/melonloader-il2cpp (net6 + Il2CppInterop)
decompiled/mono, decompiled/il2cpp   ilspycmd output (see "Reading the game")
third-party/            other people's mods, decompiled; ASSESSMENT.md files are worked examples
scripts/*.py, Makefile  build, test, bundle, deploy, log
mods/<mod>/
  <mod>.sln  README.md
  src/common/           all the code
  src/mono/mono.csproj      netstandard2.1
  src/il2cpp/il2cpp.csproj  net6.0, defines IL2CPP
  tests/                xunit (net8.0)
```

`scripts/common.py` discovers mods by globbing `mods/*/src/*/*.csproj`, and a csproj's file name must equal its
folder name (`mono.csproj`, `il2cpp.csproj`). Folder names are kebab-case (`configurable-atm`); assembly and
namespace are PascalCase (`ConfigurableAtm`).

## Reading the game

- **`decompiled/mono/` is where the logic is.** It is real C# with method bodies. Use it to learn what the game
  does, which method to patch, what is private or protected, and which constants are baked in.
  `grep -rl "class Foo" decompiled/mono/Assembly-CSharp`. Namespaces are folders (`ScheduleOne.Money/ATM.cs`),
  but check by class name, since not every type sits where its namespace suggests.
- **`decompiled/il2cpp/` is signatures only**: interop stubs full of `NativeMethodInfoPtr_*` and
  `il2cpp_runtime_invoke`, with no game logic. Use it only to confirm what the proxy exposes (member names, types,
  access).
- Third-party mods under `third-party/*/decompiled` and their `ASSESSMENT.md` show how others solved the same
  problems, and what went wrong. Treat them as untrusted examples.
- If `decompiled/` is empty, generate it:
  `ilspycmd -p -o decompiled/mono/Assembly-CSharp lib/mono/Assembly-CSharp.dll` (same for `il2cpp`).

## Anatomy of a mod (`src/common/`)

| Path | Role |
|---|---|
| `<Mod>Mod.cs` | `MelonMod` subclass and assembly attributes |
| `GlobalUsings.cs` | The one place namespaces differ: `#if IL2CPP global using Il2CppScheduleOne.X; #else global using ScheduleOne.X; #endif`, plus `global using UnityEngine;` |
| `Patches/` | Thin `[HarmonyPatch]` classes |
| `Game/GameAccess.cs` | Every read of or call into the game, and where the remaining `#if IL2CPP` branches live |
| `Rules/` | Pure decisions with no game, Unity or MelonLoader types (unit-tested) |
| `Config/ModConfig.cs` | `MelonPreferences` entries (logging is `ModLog` from `lib/shared/`) |

Use [configurable-atm](mods/configurable-atm/src/common/ConfigurableAtmMod.cs) and
[warehouse-access](mods/warehouse-access/src/common/WarehouseAccessMod.cs) as the templates. Copy their structure.

### Shared code (`lib/shared/`)

Linked source: each csproj has `<Compile Include="$(LibDir)shared\**\*.cs" />`, so every mod gets its own `internal`
copy in namespace `ModShared` (`global using ModShared;` in `GlobalUsings.cs`) and still ships as one DLL.
`Melon/ModLog.cs` (`Info`, `Warning`, `WarnOnce`, `WarnChanged` for settings checked on every preferences event,
`Verbose`, `Trace`; `Level` 0-2 or `VerboseEnabled`), `Melon/Prefs.cs` (`CreateVerbose` and `ApplyVerbose` for the
VerboseLogging entry), `Melon/Interop.cs` (`As<T>`, `Same`, `IdOf`: type tests and identity on both runtimes, used
through `global using static ModShared.Interop;`),
`Melon/PatchLoader.cs` (`PatchLoader.ApplyAll(this)`), `Rules/Numbers.cs` (game-free, also compiled into the tests:
`PositiveOr`, `PositiveCappedOr`, `ClampOr`, ...). Anything in `Rules/` there must stay free of game, Unity and
MelonLoader types. Rules files that use it need `using ModShared;`.

### Entry point

- Attributes: `[MelonInfo(typeof(XMod), "Name", "1.0.0", "DoctorShuckle")]`, `[MelonGame("TVGS", "Schedule I")]`,
  `[MelonPlatformDomain(...IL2CPP)]` or `...MONO` chosen with `#if`, and `[HarmonyDontPatchAll]`.
- A header comment explains how the mod works and why.
- `OnInitializeMelon`: `ModLog.Init(LoggerInstance)`, `ModConfig.Init()`, `ModConfig.Apply()`, then apply patches.
- `PatchLoader.ApplyAll(this)` reflects over the assembly for types marked `[HarmonyPatch]`. Each is applied alone
  inside try/catch, then it logs `"<Name> <ver> (IL2CPP|Mono): N/M patches applied."` so a missing method disables one feature, not the mod.
  Adding a patch class needs no registration.
- `OnPreferencesLoaded` and `OnPreferencesSaved` both call `ModConfig.Apply()`, so settings change at runtime.

### Config and logging

- One `MelonPreferences` category named after the mod (it appears in `UserData/MelonPreferences.cfg`). Every entry
  gets a display name and a description. Include `VerboseLogging` (default false) behind `ModLog.Verbose`.
- Validate values (clamp, reject NaN or non-positive), warn once per problem (`WarnOnce`), and fall back to the game's value.
- Keep the validation itself in `Rules/` so it can be tested.

### Patch style

- Prefix, Postfix, Transpiler or Finalizer, with `__instance`, `__result` (`ref`), `__state` (`out`) as needed.
- Wrap the body in try/catch. On error call `ModLog.Warning(...)` and let the game continue (`return true` from a prefix).
  For per-frame patches warn once.
- Patch by name: `[HarmonyPatch(typeof(X), "Name")]`. For private methods use the string. Harmony picks by name
  alone, so an overloaded target needs the argument types.
- Multiplayer: let only the host decide (`InstanceFinder.IsServer`) and use the game's own networked calls
  (e.g. `ApplyAdditive_Server`) so clients don't need the mod. Say in the README whether clients need it.
- Don't touch saved state unless the mod is about it. Prefer changing behavior over writing values the game
  persists, and say so in the README ("Saves no data of its own").

## Mono vs IL2CPP

Keep every difference inside `GlobalUsings.cs` and `Game/GameAccess.cs` (plus a flavor-only file when a feature
can't be shared). Patch classes should be identical on both flavors.

| Concern | Mono | IL2CPP |
|---|---|---|
| Namespaces | `ScheduleOne.*`, `FishNet.*` | `Il2CppScheduleOne.*`, `Il2CppFishNet.*`, `Il2Cppmscorlib` |
| Private or protected members | `AccessTools.FieldRefAccess<T,F>("name")`, `MethodInfo.Invoke` | Proxy exposes them as public: use directly |
| Type tests | `is`, `as` | Wrapper classes: `((Il2CppObjectBase)o).TryCast<T>()`; use the shared `As<T>` |
| Identity of a game object | `RuntimeHelpers.GetHashCode` | `.Pointer.ToInt64()`; use the shared `Same` and `IdOf` |
| Arrays and lists | `T[]`, `System.Collections.Generic.List<T>` | `Il2CppReferenceArray<T>`, `Il2CppSystem.Collections.Generic.List<T>` |
| Compile-time constants baked into code | Transpiler on the real IL | IL is gone: see below |
| Target | netstandard2.1 | net6.0 + `DefineConstants IL2CPP` |

Pattern for a split in `GameAccess`:

```csharp
#if !IL2CPP
    // Protected field on Mono; the IL2CPP proxy exposes it as a public property.
    private static readonly AccessTools.FieldRef<Skateboard, SkateboardSettings> SettingsRef =
        AccessTools.FieldRefAccess<Skateboard, SkateboardSettings>("_settings");
#endif
public static SkateboardSettings Settings(Skateboard b)
{
#if IL2CPP
    return b._settings;
#else
    return SettingsRef(b);
#endif
}
```

### IL2CPP hazards

- **Inlining:** the native compiler may inline small methods, so a patch on one can silently never fire. Patch
  the callers too, or patch a method that is certainly called (configurable-atm wraps all callers of the limit).
- **`const` values are compiled into every user** on both runtimes. Mono: a transpiler replaces the literal
  (`ldc.r4`) and throws if the expected count differs, so a game update fails loudly. IL2CPP: no IL to rewrite,
  so change the inputs around the call instead (configurable-atm shifts `ATM.WeeklyDepositSum` in a prefix and
  restores it in a Finalizer). See `MonoLimitPatches.cs` and `Il2CppLimitPatches.cs`.
- **Identical methods may be merged** (code folding). `configurable-employees` logs this for task durations.
- Don't hard-code native field offsets (AbsorbentSoil did, and it breaks on any game update).
- IL2CPP proxy objects are wrappers: don't use `==` or `is` to compare identity or type across base types.
- Flavor-only files are wrapped whole: `#if IL2CPP ... #endif` or `#if !IL2CPP ... #endif`.

## Tests

`mods/<mod>/tests/` (copy `Directory.Build.props` and `tests.csproj` from an existing mod, and change the namespace):

- **Rules tests** compile only `src/common/Rules/**` (`<Compile Include="$(SrcDir)Rules\**\*.cs" />`), so nothing
  game-related may be referenced from `Rules/`.
- **Canary tests** (`Canary/GameApi.cs` + `GameApiCanaryTests.cs`) read `lib/` metadata for **both** flavors with
  `MetadataLoadContext` and nothing is executed. For every `[HarmonyPatch]` target assert: the method exists, is
  declared exactly once, and has the expected parameters and return type. Assert flavor differences
  (e.g. "public on IL2CPP, private on Mono"). Tests skip when `lib/` is empty.
- Run `scripts/test.py [-m mod]` after any change and after a game update (refresh `lib/` first).

## New mod checklist

1. Read the Mono decompile and find the behavior, the methods and the constants.
2. Copy the smallest similar mod to `mods/<new-kebab>/` and rename: sln, csprojs (`RootNamespace`, `AssemblyName`,
   `[assembly: MelonInfo]`), namespace, config category. Keep the csproj file names `mono.csproj` and `il2cpp.csproj`.
3. Reference only the assemblies you use in each csproj, and drop `Newtonsoft.Json` unless the mod stores data.
4. Put decisions in `Rules/`, game calls in `Game/GameAccess.cs`, thin patches in `Patches/`, and settings in `Config/`.
5. Add `GlobalUsings.cs` with the namespaces of the game types you use, for both flavors.
6. Add rules tests and canary tests for every patch target.
7. `scripts/build.py -m <mod>` (both flavors) and `scripts/test.py -m <mod>`. Fix warnings.
8. Write the mod's README (Settings table, Things to know, Installing, Multiplayer, Status) and add a row to the
   table in the root README.
9. Commit with a short imperative title ("Add FooMod").

## Running it

`make build`, `make test`, `make bundle MOD= FLAVOR=`, `make deploy MOD= FLAVOR=` (scp to the game PC, needs
`etc/local.json`), `make log` (prints MelonLoader's `Latest.log`). Install = drop `<Name>.dll` into the game's
`Mods/` folder. Turn on `VerboseLogging` when diagnosing.
