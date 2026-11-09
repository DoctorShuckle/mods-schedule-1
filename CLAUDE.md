# Schedule I mods: guide for agents

How to create (and change) a mod in this repo. Mods are MelonLoader mods for **Schedule I** (TVGS), written
once in C# and compiled twice: **IL2CPP is the primary flavor**, **Mono** is built too. Read
[README.md](README.md) for building, scripts and licence.

The guidance is split up under `.claude/`:

| Where | What |
|---|---|
| `.claude/rules/ground-rules.md` | Always loaded: the rules every change follows |
| `.claude/rules/mod-structure.md` | Anatomy of a mod, shared code, entry point, config, patch style |
| `.claude/rules/mono-il2cpp.md` | Runtime differences and IL2CPP hazards |
| `.claude/rules/reading-the-game.md` | Using `decompiled/` and `third-party/` |
| `.claude/rules/tests.md` | Rules tests and canary tests |
| `.claude/style/` | Code style guides |
| `.claude/skills/new-mod/` | New-mod spec, templates and `scaffold.py` |

Rules other than the ground rules load when Claude touches matching files (see each file's `paths:`).

@.claude/rules/ground-rules.md
@.claude/style/csharp-style.md

## Repo layout

```
Directory.Build.props   RepoRoot, LibDir, ModName; output goes to out/<mod>/<flavor>/
lib/mono, lib/il2cpp                 game assemblies (each folder has a README saying what to copy in)
lib/melonloader-mono (net35), lib/melonloader-il2cpp (net6 + Il2CppInterop)
decompiled/mono, decompiled/il2cpp   ilspycmd output (see .claude/rules/reading-the-game.md)
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

## Running it

`make build`, `make test`, `make bundle MOD= FLAVOR=`, `make deploy MOD= FLAVOR=` (scp to the game PC, needs
`etc/local.json`), `make log` (prints MelonLoader's `Latest.log`). Install = drop `<Name>.dll` into the game's
`Mods/` folder. Turn on `VerboseLogging` when diagnosing.
