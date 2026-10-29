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
folders for the flavour you are building.

```bash
dotnet build mods/persistent-pot-additives/src/mono -c Release -o out/persistent-pot-additives/mono       # Mono branch
dotnet build mods/persistent-pot-additives/src/il2cpp -c Release -o out/persistent-pot-additives/il2cpp   # IL2CPP branch
```

`decompiled/` is likewise an empty skeleton for optional `ilspycmd` output (handy for reading game code,
not needed to build).

Output goes to `out/<mod>/<flavour>/`. Copy the resulting DLL into the game's `Mods` folder.

## Optional: deploy scripts

`scripts/deploy.sh <mod> <flavour>` (for example `scripts/deploy.sh persistent-pot-additives il2cpp`) builds a project and `scp`s the DLL to a game install on another machine;
`scripts/log.sh` pulls MelonLoader's `Latest.log` back. Copy `etc/local.example.sh` to
`etc/local.sh` and fill in your host and game path. Not needed if you build and play on the
same machine.

## License

[MIT](LICENSE). Fork, modify, and re-upload freely; keep the copyright notice. The game and MelonLoader
are not covered by this license and belong to their respective owners.
