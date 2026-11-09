# Dealers Run This Town

A mod for **Schedule I** that makes your dealers run to their deals. While a dealer is on the way to a
customer they move at a fixed speed you choose (6 m/s by default, against the game's 1.8 m/s walk). Everywhere
else they walk at the game's own pace.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[DealersRunThisTown]`.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `DealSpeed` | `6.0` | Metres per second while a dealer is on the way to a deal. Values from 1 to 12 are used |
| `IncludeCartelDealers` | `false` | `false` changes only the dealers you recruit. `true` changes the cartel's dealers too |
| `VerboseLogging` | `false` | Logs each time a dealer starts or stops running to a deal |

For scale: the game's dealer walk is 1.8, the player's walk 3.25, the game's own "urgent deal" speed 4.9.
A value that is not a number, or is zero or negative, is logged and counts as 6. A value outside 1 to 12 is
moved to the nearest end and logged. Changes made in the preferences file while the game runs are picked up
at each dealer's next trip, not part-way through one.

## Things to know

- **A deal is always at one speed.** The mod's speed beats the game's rain and urgent-contract speeds, so
  neither adds anything on top. With the default 6 m/s that is never slower than the game would have been.
  Set `DealSpeed` below 5.4 and rain would have made a dealer faster than the mod does.
- **Things that stop a dealer still do.** Cowering, flinching and getting up from a fall use a higher
  priority than the mod, so a dealer who is hit still stops. The effects that change an NPC's speed
  (Athletic, Sedating and so on) still scale the result.
- **Dealers set off early.** The game works out when a dealer leaves from their walking speed, which the
  mod does not change, so dealers arrive well before the customer and wait.
- **Saves no data of its own.** Nothing is written to your save; removing the mod returns the game to normal.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `FasterDealerMovement.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Not tested. Dealers are moved by the host's game, so the mod most likely needs to be on the host only, but
this has not been confirmed.

## Status

Both builds compile and pass the unit and canary tests. **Not run in game.** Unconfirmed until it is:
that all four hooks fire in the IL2CPP build, whether a dealer who is knocked out mid-deal keeps the speed,
and whether the walk animation and the turning hold up at high speeds.

## How it works

A dealer on the way to a customer is driven by `DealerAttendDealBehaviour`. Each NPC has a speed stack
(`NPCSpeedController`) of named entries where only the highest priority counts, and the game applies it to the
navigation agent itself.

| Hook | What the mod does |
| ---- | ----------------- |
| `DealerAttendDealBehaviour.Activate` | Adds the entry `drt_deal_run` (absolute, priority 11) with `DealSpeed` |
| `DealerAttendDealBehaviour.Resume` | Adds it again (the same id just updates it) |
| `DealerAttendDealBehaviour.Pause` | Removes it |
| `DealerAttendDealBehaviour.Deactivate` | Removes it |

Nothing is polled. The game's own entries use priority 0 (walking), 2 (rain), 10 (urgent contract) and 80 to
100 (cowering, flinching, getting up), so 11 sits above the first three and below the last.

## For developers

```bash
make test MOD=faster-dealer-movement
make build MOD=faster-dealer-movement
```

The canary tests read the game's DLLs in `lib/` and fail if a patch target has gone.
