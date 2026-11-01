# Configurable Skateboard

A mod for **Schedule I** that lets you tune the skateboard: how fast it goes, how hard it pushes, how
high it jumps, and what a push costs in stamina.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[ConfigurableSkateboard]`. Each is a
multiplier on the game's own value, so `1` leaves it as in the game. Out-of-range values are not
obeyed: zero, negative or not-a-number counts as `1`, and anything above `10` is capped at `10`.
(The stamina setting may be `0`.)

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `TopSpeedMultiplier` | `1.5` | Multiplies the board's top speed (10 km/h in the game) |
| `PushForceMultiplier` | `1.5` | Multiplies how hard each push accelerates the board |
| `JumpForceMultiplier` | `1.5` | Multiplies the upward force of a jump, so jump height |
| `PushStaminaMultiplier` | `0.5` | Multiplies the stamina a push spends (12.5 in the game); `0` makes pushing free |
| `VerboseLogging` | `false` | Logs each board the mod tunes |

The defaults are a starting point, not tested values. A change reaches a board the next time you
mount it (and again after each weather change).

## Things to know

- **Top speed is not a hard cap.** The game has no speed limit: push force fades out as you approach
  the top speed, and friction grows. Raising the top speed moves where the board settles. Raise the
  push force with it, or you gain little.
- **Starting a push still needs 12.5 stamina.** That check is a constant in the game's code, which
  this mod does not rewrite. Cheaper pushes work; pushing with a nearly empty bar does not.
- **Not reachable:** the jump charge time, the jump cooldown, and the speed you keep on mounting
  and dismounting. They are constants too.
- Spending stamina is scaled only while you are on a board, so sprinting is unaffected.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `ConfigurableSkateboard.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Only the player riding a board runs its physics, so install the mod on your own machine to change
your own board. Other players see the result and do not need the mod.

## Status

Both builds compile and pass the unit and canary tests. Neither has been run in the game yet.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `Skateboard.Start` | Gives the new board its own copy of the settings and scales it |
| `Skateboard.OnWeatherChange` | Scales the settings the game just rebuilt (default plus rain) |
| `PlayerMovement.ChangeStamina` | While riding, scales any spend of stamina |

The game rebuilds a board's settings on every weather change, so the mod scales after each rebuild.
It never edits the game's shared default settings. Nothing is saved, so removing the mod returns the
game to normal.

## For developers

```bash
make test MOD=configurable-skateboard
make build MOD=configurable-skateboard
```

The canary tests read the game's DLLs in `lib/` and fail if a patch target has gone.
