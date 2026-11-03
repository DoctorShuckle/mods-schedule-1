# Skip Disclaimer

A mod for **Schedule I** that skips the disclaimer screen shown when the game starts, so the main menu
appears straight away. It works as soon as it is installed; the setting below turns it off.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[SkipDisclaimer]`.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `SkipDisclaimer` | `true` | `true` skips the disclaimer. `false` leaves the game as it is |
| `VerboseLogging` | `false` | Logs when the mod skips the disclaimer |

Changes made in the preferences file while the game runs are picked up, but the disclaimer only
appears once per launch, so set it before starting the game.

## Things to know

- Only the game's own disclaimer is skipped. The Unity logo and any engine splash screens before the
  menu scene are not game code and are not touched.
- Saves no data of its own, and changes nothing the game persists.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `SkipDisclaimer.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

The disclaimer is a local menu screen, so only the player who wants it skipped needs the mod.

## Status

Not run in game. Both builds are meant to compile and pass the canary tests, which only prove the
patched members exist.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `Disclaimer.Awake` (prefix) | Sets the static `Disclaimer.Shown` flag first, so the game closes the screen at once through its own path (it calls `OnClose` and hides itself) |

No polling: the patch runs once, when the menu scene creates the disclaimer.

## For developers

```bash
make test MOD=skip-disclaimer
make build MOD=skip-disclaimer
```
