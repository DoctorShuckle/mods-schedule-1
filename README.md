# Schedule I mods

By **DoctorShuckle**.

Mods for **Schedule I** (TVGS), built with [MelonLoader](https://melonwiki.xyz). Each mod builds for
both the **Mono** and **IL2CPP** branches of the game.

> This repository contains only original source code. It does **not** include any game files or
> MelonLoader files. To build, you supply those from your own install (see below).

## Mods

| Mod                                                                                    | What it does                                                                                                                  |
| -------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| [Persistent Pot Additives](mods/persistent-pot-additives/README.md) | Additives on a pot last as long as the soil, instead of being wiped after every harvest. Host-only, saves no data of its own. |
| [Configurable Employees](mods/configurable-employees/README.md) | Tune employee wages, signing fees, walk speed, work speed, assignment limits, the Fixer's fee and per-property capacity. Does nothing until configured. |
| [Tidy Stations](mods/tidy-stations/README.md) | Stops work leaving trash: station packaging, botanists' empty bags and packets, and plant scrap. Each kind can be switched back on; map trash is untouched. |
| [Tidy Streets](mods/tidy-streets/README.md) | Stops the map's own trash from spawning and clears the world's trash each time a save loads. Each part can be switched off. |
| [Configurable Skateboard](mods/configurable-skateboard/README.md) | Tune the skateboard's top speed, push force, jump force and push stamina cost with multipliers. |

## Building

You need the [.NET SDK](https://dotnet.microsoft.com/download) (8.0 works). The projects target
`netstandard2.1` and reference DLLs from the `lib/` folder at the repo root. The empty folders are
already there (each has a README listing what to put in it); you fill them with files from your own install:

```
lib/
├── mono/            from <game>/Schedule I_Data/Managed        (Mono branch)
├── il2cpp/          from <game>/MelonLoader/Il2CppAssemblies    (IL2CPP branch, generated on first launch)
├── melonloader-mono/    MelonLoader.dll, 0Harmony.dll, ... net35 build (official release or Mono install)
└── melonloader-il2cpp/  MelonLoader.dll, 0Harmony.dll, Il2CppInterop.* ... net6 build (IL2CPP install)
```

Roughly: install the matching MelonLoader build on the game branch you want, launch once, then copy the
files above. Check each `.csproj` under `mods/` for the exact assemblies it references. You only need the
folders for the flavor you are building.

### Build

Build with the Python scripts (Python 3.8+, the .NET SDK on your `PATH` or in `~/.dotnet`):

```bash
scripts/build.py                                       # every mod, both flavors
scripts/build.py -m persistent-pot-additives -f il2cpp # one mod and flavor
```

Output goes to `out/<mod>/<flavor>/`. Copy the resulting DLL into the game's `Mods` folder. Plain
`dotnet build` works too:

```bash
dotnet build mods/persistent-pot-additives/src/mono -c Release -o out/persistent-pot-additives/mono       # Mono branch
dotnet build mods/persistent-pot-additives/src/il2cpp -c Release -o out/persistent-pot-additives/il2cpp   # IL2CPP branch
```

`decompiled/` is likewise an empty skeleton for optional `ilspycmd` output (handy for reading game code,
not needed to build).

## Scripts

All scripts live in `scripts/`, take `-h` and `-v/--verbose`, and share a `-m/--mod` and `-f/--flavor`
pair (`mono` or `il2cpp`). A `Makefile` wraps them; run `make help` for the list.

| Script        | Make target                              | What it does                                                          |
| ------------- | ---------------------------------------- | --------------------------------------------------------------------- |
| `build.py`    | `make build [MOD=… FLAVOR=…]`            | Builds mods; with no `-m`/`-f` it builds everything.                  |
| `test.py`     | `make test [MOD=…]`                      | Runs each mod's unit tests (`mods/<mod>/tests/`).                     |
| `bundle.py`   | `make bundle MOD=… FLAVOR=… [VERSION=…]` | Builds and zips to `out/bundles/<mod>-<flavor>-<version>.zip`.        |
| `deploy.py`   | `make deploy MOD=… FLAVOR=…`             | Builds and `scp`s the DLL to the game PC (see below).                 |
| `log.py`      | `make log`                               | Fetches MelonLoader's `Latest.log` from the game PC and prints it.    |

`make clean` removes `out/`; `make clean-logs` removes `logs/`.

### Testing

```bash
scripts/test.py                          # every mod's tests
scripts/test.py -m persistent-pot-additives -k "FullyQualifiedName~Canary"   # one mod, filtered
```

Each mod's `tests/` project has two kinds of test:

- **Logic tests** cover the mod's decisions (`Decisions.cs`), which use no game types, so they run anywhere.
- **Canary tests** read the game DLLs in `lib/` (metadata only; nothing is run) and fail if a member the
  mod depends on has been renamed or removed. After a game update, refresh `lib/` and run the tests to
  find out before launching the game. They skip themselves when `lib/` is empty.

Neither kind proves the mod behaves correctly in the game; that still needs a run in-game.

### Bundling for release

`scripts/bundle.py -m <mod> -f <flavor> [-V <version>]` (version defaults to `git describe --always --dirty`)
produces a zip whose root mirrors the game folder (`Mods/<Mod>.dll`), so it can be dropped straight onto
Vortex or unzipped into the game directory.

### Deploying to another machine

`deploy.py` and `log.py` copy files over `scp` to a game install on another machine. Copy
`etc/local.example.json` to `etc/local.json` (gitignored) and fill in `win_host` and `win_game_dir`. Not
needed if you build and play on the same machine.

### Logs

Every script logs to the console (info to stdout, warnings and errors to stderr) and to `logs/`
(gitignored): `<script>.log` is overwritten on each run, and `<script>-<timestamp>.log` is kept as history.
Lines look like `[2026-10-29 16:30:28 INFO] message`.

## License

[MIT](LICENSE). Fork, modify, and re-upload freely; keep the copyright notice. The game and MelonLoader
are not covered by this license and belong to their respective owners.
