# Warehouse Access

A mod for **Schedule I** that opens the Dark Market warehouse without the knock: no "Knock" prompt,
no waiting for the hours, no closing during a police chase, and Igor leaves the door for his office.
It does nothing until you have completed the warehouse quest (this can be switched off).

It is a trimmed take on the third-party *Warehouse Always Open* mod, rebuilt on the same ideas with
fewer patches and the game's own rank value instead of a copied one. Do not run both together.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[WarehouseAccess]`.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `AlwaysOpen` | `true` | `true` opens the warehouse for everyone, at every hour. `false` opens it only for a player who has the rank below |
| `SkipWarehouseQuest` | `false` | `true` lets the mod work before the quest is completed. Nothing is written to the quest or the save |
| `UseCustomRank` | `false` | Only matters when `AlwaysOpen` is `false`. `false` asks for the rank the game itself asks for at the door; `true` asks for the two settings below |
| `MinimumRank` | `Street_Rat` | The rank name when `UseCustomRank` is `true`, such as `Hoodlum` or `Block_Boss` (case and spaces do not matter) |
| `MinimumTier` | `1` | The tier, 1 to 5, when `UseCustomRank` is `true` |
| `VerboseLogging` | `false` | Logs each time the mod removes the knock prompt or moves Igor |

A bad `MinimumRank` is logged and counts as `Street_Rat`; a tier outside 1 to 5 is moved to the nearest.
Changes made in the preferences file while the game runs are picked up.

## Things to know

- **The rank settings change who gets in, not when.** Whoever qualifies gets the warehouse at every
  hour and during a police chase, whichever setting let them in.
- **The game's own unlock is not triggered.** The mod leaves `DarkMarket.Unlocked` alone, because
  setting it also saves the unlock and turns on Oscar's deliveries. So with the mod alone, Oscar's
  deliveries still need the normal knock at least once.
- **Igor is moved when he starts.** If the quest has not been found yet at that moment he stays at the
  door until you reload the save. The door and the hours are checked continuously and are not affected.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `WarehouseAccess.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Not tested. The checks run on whichever machine has the mod, so install it on every player who
should see the warehouse open.

## Status

Both builds compile and pass the unit and canary tests. Neither has been run in the game yet.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `DarkMarket.ShouldBeOpen` | Answers yes, so the game's open flag (and the roller doors and Igor's dialogue that read it) agree |
| `DarkMarketAccessZone.GetIsOpen` | Answers yes, so the doors' access zone opens at every hour |
| `DarkMarketMainDoor.Hovered` | Calls the game's own `SetKnockingEnabled(false)`, which removes the knock prompt |
| `Igor.Start` | Sends Igor to his office, as the game does once the warehouse unlocks |

All four ask one question first (`WarehouseGate`): is the quest completed (or skipped), and is the
mod set to always open or does the player have the rank? Nothing is saved, so removing the mod
returns the game to normal.

## For developers

```bash
make test MOD=warehouse-access
make build MOD=warehouse-access
```

The canary tests read the game's DLLs in `lib/` and fail if a patch target has gone.
