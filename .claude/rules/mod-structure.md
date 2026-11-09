---
paths:
  - "mods/*/src/common/**"
  - "lib/shared/**"
---

# Anatomy of a mod (`src/common/`)

| Path | Role |
|---|---|
| `<Mod>Mod.cs` | `MelonMod` subclass and assembly attributes |
| `GlobalUsings.cs` | The one place namespaces differ: `#if IL2CPP global using Il2CppScheduleOne.X; #else global using ScheduleOne.X; #endif`, plus `global using UnityEngine;` |
| `Patches/` | Thin `[HarmonyPatch]` classes |
| `Game/GameAccess.cs` | Every read of or call into the game, and where the remaining `#if IL2CPP` branches live |
| `Rules/` | Pure decisions with no game, Unity or MelonLoader types (unit-tested) |
| `Config/ModConfig.cs` | `MelonPreferences` entries (logging is `ModLog` from `lib/shared/`) |

Use [configurable-atm](../../mods/configurable-atm/src/common/ConfigurableAtmMod.cs) and
[warehouse-access](../../mods/warehouse-access/src/common/WarehouseAccessMod.cs) as the templates. Copy their structure.

## Shared code (`lib/shared/`)

Linked source: each csproj has `<Compile Include="$(LibDir)shared\**\*.cs" />`, so every mod gets its own `internal`
copy in namespace `ModShared` (`global using ModShared;` in `GlobalUsings.cs`) and still ships as one DLL.
`Melon/ModLog.cs` (`Info`, `Warning`, `WarnOnce`, `WarnChanged` for settings checked on every preferences event,
`Verbose`, `Trace`; `Level` 0-2 or `VerboseEnabled`), `Melon/Prefs.cs` (`CreateVerbose` and `ApplyVerbose` for the
VerboseLogging entry), `Melon/Interop.cs` (`As<T>`, `Same`, `IdOf`: type tests and identity on both runtimes, used
through `global using static ModShared.Interop;`),
`Melon/PatchLoader.cs` (`PatchLoader.ApplyAll(this)`), `Rules/Numbers.cs` (game-free, also compiled into the tests:
`PositiveOr`, `PositiveCappedOr`, `ClampOr`, ...). Anything in `Rules/` there must stay free of game, Unity and
MelonLoader types. Rules files that use it need `using ModShared;`.

## Entry point

- Attributes: `[MelonInfo(typeof(XMod), "Name", "1.0.0", "DoctorShuckle")]`, `[MelonGame("TVGS", "Schedule I")]`,
  `[MelonPlatformDomain(...IL2CPP)]` or `...MONO` chosen with `#if`, and `[HarmonyDontPatchAll]`.
- A header comment explains how the mod works and why.
- `OnInitializeMelon`: `ModLog.Init(LoggerInstance)`, `ModConfig.Init()`, `ModConfig.Apply()`, then apply patches.
- `PatchLoader.ApplyAll(this)` reflects over the assembly for types marked `[HarmonyPatch]`. Each is applied alone
  inside try/catch, then it logs `"<Name> <ver> (IL2CPP|Mono): N/M patches applied."` so a missing method disables one feature, not the mod.
  Adding a patch class needs no registration.
- `OnPreferencesLoaded` and `OnPreferencesSaved` both call `ModConfig.Apply()`, so settings change at runtime.

## Config and logging

- One `MelonPreferences` category named after the mod (it appears in `UserData/MelonPreferences.cfg`). Every entry
  gets a display name and a description. Include `VerboseLogging` (default false) behind `ModLog.Verbose`.
- Validate values (clamp, reject NaN or non-positive), warn once per problem (`WarnOnce`), and fall back to the game's value.
- Keep the validation itself in `Rules/` so it can be tested.

## Patch style

- Prefix, Postfix, Transpiler or Finalizer, with `__instance`, `__result` (`ref`), `__state` (`out`) as needed.
- Wrap the body in try/catch. On error call `ModLog.Warning(...)` and let the game continue (`return true` from a prefix).
  For per-frame patches warn once.
- Patch by name: `[HarmonyPatch(typeof(X), "Name")]`. For private methods use the string. Harmony picks by name
  alone, so an overloaded target needs the argument types.
- Multiplayer: let only the host decide (`InstanceFinder.IsServer`) and use the game's own networked calls
  (e.g. `ApplyAdditive_Server`) so clients don't need the mod. Say in the README whether clients need it.
- Don't touch saved state unless the mod is about it. Prefer changing behavior over writing values the game
  persists, and say so in the README ("Saves no data of its own").
