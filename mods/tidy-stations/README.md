# Tidy Stations

A mod for **Schedule I** that stops work leaving trash behind: the empty packaging at the mixing
station, chemistry station and cauldron, the empty bags and packets botanists drop, and the scrap a
finished plant leaves. It has one on/off setting.

The trash that spawns around the map is **not** affected, so a new player can still bootstrap
themselves by picking it up.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[TidyStations]`. There is one setting per
kind of trash, named for what it does. All default to `true`, so installing the mod removes
everything below. Set one to `false` to get that trash back as in the game.

| Setting | Trash it controls |
| ------- | ----------------- |
| `NoMixingStationTrash` | Packaging left by the mixing station |
| `NoChemistryStationTrash` | Packaging left by the chemistry station |
| `NoCauldronTrash` | Packaging left by the cauldron |
| `NoBotanistSoilTrash` | The empty soil bag a botanist drops |
| `NoBotanistAdditiveTrash` | The empty additive container a botanist drops |
| `NoBotanistSeedTrash` | The empty seed packet a botanist drops |
| `NoPlantScrapTrash` | Scrap left when a plant's last harvest is taken |
| `VerboseLogging` | Logs each piece of trash the mod stops (default `false`) |

A change takes effect on the next job, with no restart.

## What it stops

| Where | Trash | How |
| ----- | ----- | --- |
| Mixing station | Used ingredient packaging | Skips `UseMixingStationTask.CreateTrash` |
| Chemistry station | Used ingredient packaging | Skips `ChemistryStation.CreateTrash` |
| Cauldron | Used ingredient packaging | Skips `Cauldron.CreateTrash` |
| Botanist, soil | Empty soil bag | `GetTrashPrefab` returns nothing |
| Botanist, additive | Empty fertilizer, PGR, Speed Grow and other containers | `GetTrashPrefab` returns nothing |
| Botanist, sowing | Seed packet | `GetTrashPrefab` returns nothing |
| Plant | Scrap left when the last harvest is taken | Clears the plant's scrap prefab just before it is dropped |

The station trash methods are called from the player's own station tasks. Chemists, packagers and
cleaners do not create trash.

## What it does not touch

The mod never patches `TrashManager.CreateTrashItem`, which every trash item goes through. Trash
spawned by `TrashGenerator`, `TrashSpawnVolume` and `TrashGenerationVolume` (the map's trash), the
cleaner emptying a trash grabber, and trash already on the ground are left alone. The mod saves no
data.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `TidyStations.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Botanist and plant trash is decided by the host. Install it there; install it on a client too if
that player uses the stations themselves.

## Status

The IL2CPP build has been run in the game (Steam Deck, under Proton) and works. The Mono build
compiles and passes the canary tests but has not been run in the game.

## For developers

```bash
make test MOD=tidy-stations
make build MOD=tidy-stations
```

The canary tests read the game's DLLs in `lib/` and fail if a patch target has gone.
