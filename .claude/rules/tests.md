---
paths:
  - "mods/*/tests/**"
  - "mods/*/src/common/Rules/**"
---

# Tests

`mods/<mod>/tests/` (copy `Directory.Build.props` and `tests.csproj` from an existing mod, and change the namespace):

- **Rules tests** compile only `src/common/Rules/**` (`<Compile Include="$(SrcDir)Rules\**\*.cs" />`), so nothing
  game-related may be referenced from `Rules/`.
- **Canary tests** (`Canary/GameApi.cs` + `GameApiCanaryTests.cs`) read `lib/` metadata for **both** flavors with
  `MetadataLoadContext` and nothing is executed. For every `[HarmonyPatch]` target assert: the method exists, is
  declared exactly once, and has the expected parameters and return type. Assert flavor differences
  (e.g. "public on IL2CPP, private on Mono"). Tests skip when `lib/` is empty.
- Run `scripts/test.py [-m mod]` after any change and after a game update (refresh `lib/` first).
