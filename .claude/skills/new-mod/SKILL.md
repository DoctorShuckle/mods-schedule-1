---
name: new-mod
description: Create a new Schedule I mod under mods/ from the repo's templates, with an explicit structure spec. Use when asked to add, create or start a new mod.
---

# New mod

The rules in `.claude/rules/` (ground rules, mod structure, Mono vs IL2CPP, tests) and
`.claude/style/csharp-style.md` apply. This skill adds the **exact tree**, a **scaffold script** that writes it,
and the steps to finish and verify the mod. Do not copy another mod to start: other mods carry their own
leftovers (comments, unused usings, reference sets).

## 1. Decide before scaffolding

Write these down first; the scaffold and the README need them.

| Question | Becomes |
|---|---|
| Folder name, kebab-case (`configurable-foo`) | folder, sln and csproj discovery (`scripts/common.py` globs `mods/*/src/*/*.csproj`) |
| Display title (`Configurable Foo`) | `[MelonInfo]` name, README heading |
| Topic, PascalCase (`Foo`) | first `Patches/FooPatches.cs`, `Rules/FooRules.cs`, `FooRulesTests.cs` |
| Which game types and methods change, from `decompiled/mono/` | patch targets, `GlobalUsings.cs`, canary tests |
| What is configurable, and what is the default | `ModConfig`. The default makes the mod do what its name says |
| Host-only or every player (`InstanceFinder.IsServer`) | the README's Multiplayer section |
| Does the mod save data? | Usually no. Only then add `Newtonsoft.Json` to the csprojs |

Folder is kebab-case; assembly, namespace and config category are the PascalCase form (`ConfigurableFoo`).

## 2. Scaffold

```bash
.claude/skills/new-mod/scaffold.py <kebab> --topic <Topic> [--title "<Title>"]
```

It writes the tree below from `templates/`, fills `{{Kebab}}`, `{{Pascal}}`, `{{Title}}`, `{{Topic}}` and
generates fresh solution GUIDs, then lists the `{{...}}` markers left. It refuses to write into a folder that
exists. Never reuse GUIDs from another mod.

```
mods/<kebab>/
  <kebab>.sln                        three projects: src folder, mono, il2cpp (fixed GUIDs per mod)
  README.md
  src/
    mono/mono.csproj                 netstandard2.1, LangVersion 11, Nullable and ImplicitUsings off
    il2cpp/il2cpp.csproj             net6.0, DefineConstants adds IL2CPP
    common/
      <Pascal>Mod.cs                 attributes, "How the mod works" banner, MelonMod
      GlobalUsings.cs                the only place namespaces differ (see below)
      Config/ModConfig.cs            Init (entries) and Apply (values, validation, warnings)
      Game/GameAccess.cs             every call into the game; the only home of #if IL2CPP besides GlobalUsings
      Patches/<Topic>Patches.cs      thin [HarmonyPatch] classes, named Type_Method_Patch
      Rules/<Topic>Rules.cs          pure decisions, no game, Unity or MelonLoader types
  tests/
    tests.csproj                     net8.0, xunit; compiles only Rules/** and lib/shared/Rules/**
    Directory.Build.props            re-derives ModName and output paths one level up
    Canary/GameApi.cs                metadata reader for lib/; identical in every mod, only the namespace differs
    Canary/GameApiCanaryTests.cs     one test per patch target, both flavors
    Rules/<Topic>RulesTests.cs
```

Not every mod needs everything. Drop `Rules/` and its tests only when the mod makes no decision worth testing
(skip-disclaimer, tidy-*); drop `Game/` only when no game state is read or called. Keep both csprojs, the sln
and the canary tests always.

## 3. Fill in the markers

- **`GlobalUsings.cs`**: one `global using Il2CppScheduleOne.X;` and the matching `global using ScheduleOne.X;`
  per game namespace used (same order in both blocks). Add `FishNet` as `Il2CppFishNet.*` / `FishNet.*` if used.
- **csproj references**: both templates carry the common set. Keep only what the code uses; each is a
  `<Reference Include=... HintPath=... Private="false" />`.

  | Add | When | Mono | IL2CPP |
  |---|---|---|---|
  | UI types | `UnityEngine.UI`, `UnityEngine.UIModule`, `Unity.TextMeshPro` | `$(GameLibDir)` | `$(GameLibDir)` |
  | Physics, vehicles, input, IMGUI | `UnityEngine.PhysicsModule`, `VehiclesModule`, `InputLegacyModule`, `IMGUIModule` | `$(GameLibDir)` | `$(GameLibDir)` |
  | Saved data | `Newtonsoft.Json` | add to both | add to both |

  Drop `ScheduleOne.Core` and FishNet if the mod uses neither. IL2CPP always keeps `Il2CppInterop.Runtime`
  and `Il2Cppmscorlib`.
- **Mod.cs**: the banner explains how the mod works and why, in prose. It is the first thing a reader sees.
- **ModConfig**: every entry has an id, a default, a display name and a description stating the valid range and
  the fallback. `VerboseLogging` comes from `Prefs.CreateVerbose` and is always present. Validation lives in
  `Rules/`, warnings go through `ModLog.WarnChanged` (settings) or `ModLog.WarnOnce`.
- **Patches**: one class per target, body in try/catch, fail open, `ModLog.Warning` on error. Prefer the game's
  own event or the method that changes the state over per-frame patches (ground rules). Add a patch class and
  nothing else: `PatchLoader` finds it.
- **GameAccess**: private or protected members are public on IL2CPP and need `AccessTools`/reflection on Mono.
  Use `As<T>`, `Same`, `IdOf` from `lib/shared` for type tests and identity.
- **Canary tests**: for every `[HarmonyPatch]` target assert that it exists, is declared exactly once, and has the
  expected parameters and return type; assert flavor differences (public on IL2CPP, private on Mono).
- **README**: fill every marker. Sections in this order: Settings, Things to know, Installing, Multiplayer,
  Status, How it works, For developers. Status says "Neither has been run in the game yet" until it was.

## 4. Verify

1. `grep -r "{{" mods/<kebab>` finds nothing.
2. `scripts/build.py -m <kebab>`: both flavors, **no warnings**.
3. `scripts/test.py -m <kebab>`: rules and canary tests pass (canary tests skip when `lib/` is empty: say so).
4. Check the new mod is picked up: `scripts/build.py -m <kebab>` accepted the name, and `out/<kebab>/{mono,il2cpp}/<Pascal>.dll` exist.
5. Add a row to the mods table in the root `README.md`.
6. Make sure nothing outside `mods/<kebab>/` and the README changed.

## 5. Done

Commit with a short imperative title ("Add Configurable Foo"). Do not say it works in game unless it was run.

## Maintaining the templates

`templates/*.csproj.tmpl`, `mod.sln.tmpl`, and `tests/*` come from warehouse-access with the name replaced;
`GameApi.cs` is identical in every mod except skip-disclaimer. When a repo-wide change touches these files in
the existing mods (a new reference, a new test package version), change the template too.
