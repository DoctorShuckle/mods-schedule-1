# Tidy Streets

A mod for **Schedule I** that keeps the streets clean. It stops the map's own trash from spawning,
and removes the trash already in the world each time a save finishes loading.

It is the opposite of what the game intends for a new player: the trash scattered around the map is
there so that someone with no cash can pick it up and sell it. Use it once you no longer need that.
Trash that comes from work (the stations, botanists and plant scrap) is the job of
[Tidy Stations](../tidy-stations/README.md); the two mods work together.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[TidyStreets]`. Each is named for what it
does and defaults to `true`, so installing the mod does everything below. Set one to `false` to turn
that part off.

| Setting | What it does |
| ------- | ------------ |
| `NoGeneratorTrash` | Stops the trash that refills the map's areas (a fifth of their cap) each time you fall asleep |
| `NoSpawnVolumeTrash` | Stops the trash that appears in set places each time you wake up |
| `NoGenerationVolumeTrash` | Stops the game's other generation volumes, which spawn a set amount of trash when something asks |
| `ClearTrashOnLoad` | Removes all trash in the world each time a save finishes loading |
| `VerboseLogging` | Logs each spawn the mod stops and each clear (default `false`) |

The three spawner settings take effect at once. `ClearTrashOnLoad` is read when a save finishes
loading.

## Clearing on load

When a save has finished loading, the mod calls `TrashManager.DestroyAllTrash`, which is what the
game's own `cleartrash` console command runs. That removes **all** trash: the map's, anything you
dropped and left on the ground, and trash bags. Only the host clears; on a client it does nothing.

## What it does not touch

The mod never patches `TrashManager.CreateTrashItem`, which every trash item goes through, so trash
made by work, the cleaner emptying a grabber, and trash you throw away still appear. Trash created
after the load (by stations, for example) stays until you pick it up or clear it.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `TidyStreets.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Spawning and clearing are decided by the host. Install it there.

## Status

The IL2CPP build has been run in the game (Steam Deck, under Proton): the load listener fires and the
world is cleared on load. Not yet confirmed in the game: that sleeping and waking spawn no new street
trash, and the Mono build, which compiles and passes the canary tests but has not been run.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `TrashGenerator.SleepStart` | Skipped |
| `TrashSpawnVolume.SleepStart` | Skipped |
| `TrashGenerationVolume.Generate` | Skipped |
| `LoadManager.onLoadComplete` | A listener calls `DestroyAllTrash` |

`LoadManager` outlives the scenes, so the mod adds its listener once, when it first exists, and it
fires on every later load. Nothing is saved, so removing the mod returns the game to normal.

## For developers

```bash
make test MOD=tidy-streets
make build MOD=tidy-streets
```

The canary tests read the game's DLLs in `lib/` and fail if a patch target has gone.
