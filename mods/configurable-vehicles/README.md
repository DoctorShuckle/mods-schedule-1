# Configurable Vehicles

A mod for **Schedule I** that lets you tune each vehicle on its own: its weight, engine power,
steering, brakes, downforce, grip and trunk size. It does nothing until you set a value.

## Settings

Settings live in `UserData/MelonPreferences.cfg`. They appear after the game has started once with
the mod installed.

**General**, under `[ConfigurableVehicles]`:

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `TrunkSlots` | empty | A number from 1 to 20: the trunk size of every vehicle that has no `TrunkSlots` of its own |
| `TrunkRows` | `2` | Rows the trunk screen uses for vehicles whose size is set (1 to 5): 20 slots in 2 rows is 10 across |
| `VerboseLogging` | `false` | Logs every vehicle as it starts, with the game's own values and what the mod changed |

**Per vehicle**, one block each, `[ConfigurableVehicles_<code>]`, for `shitbox`, `shitbox_police`,
`bruiser`, `bruiser_police`, `hounddog`, `hotbox`, `dinkler`, `cheetah` and `veeper`. In every one,
**`0` leaves the game's value alone**. A value that cannot be used (negative, not a number) counts
as `0` with a warning, and an absurd one is capped.

| Setting | What it does |
| ------- | ------------ |
| `Mass` | Weight in kg (max 20000). A heavier vehicle also brakes harder: brake force scales with weight |
| `PowerMultiplier` | Multiplies the engine's torque (max 10): acceleration, and the speed the vehicle settles at |
| `TopSpeed` | km/h. Only scales braking and the engine sound, it is **not a speed limit** (see below) |
| `SteerAngle` | Degrees the front wheels turn at full lock (max 80) |
| `SteerRate` | Degrees per second the wheels turn toward the steering input |
| `BrakeMultiplier` | Multiplies the brake force (max 10) |
| `Downforce` | Pushes the vehicle onto the road harder at speed (max 100) |
| `GripMultiplier` | Multiplies every wheel's grip, sideways and forward (max 10) |
| `TrunkSlots` | Trunk size, 1 to 20; `0` uses the general `TrunkSlots`, else the game's size |

To pick values, turn on `VerboseLogging` and read the `Game values <code>: ...` line the log prints
for each vehicle as it starts: mass, the torque curve, steering, brake, downforce, drag and the
wheels' grip.

## Things to know

- **Top speed has no setting that works as a limit.** The game's `TopSpeed` is used only to scale
  braking and the engine sound. The torque curve levels off past its last point instead of dropping
  to zero, so the speed a vehicle settles at comes from engine power against drag. Raise
  `PowerMultiplier` to go faster. How close the result is to any given km/h is not worked out.
- **Settings reach cars already in the world when you save the config**, and a car that spawns
  later gets them when it starts. Clearing a value puts the game's value back.
- **Trunk size is set when the trunk wakes up.** It applies to a vehicle when it spawns, so
  restart or reload the save after changing it. Items are saved by slot, so a bigger trunk keeps its
  items. **Shrinking a trunk, or removing the mod, drops items in the slots that no longer exist** on
  the next load and save, so empty them first. Whether the trunk screen copes with 20 slots is not checked.
- Not changed: the body's shape and centre of mass, gearing, suspension and the handbrake. A vehicle
  the game adds later has no settings block until the mod lists it.
- Saves no data of its own: the game's own vehicle save is unchanged.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `ConfigurableVehicles.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

The driver's machine runs the car's physics, so a handling change only affects drivers who have the
mod. Trunk slots are built on each machine, so every player needs the mod **and the same trunk
settings**, or inventories will not line up.

## Status

Both builds compile and pass the unit and canary tests. The trunk sizes and the earlier
copy-from-another-car version were run in the game; the per-vehicle settings have not been yet.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `LandVehicle.Start` | Remembers the game's values for the vehicle, then writes the settings for its code, computed from those |
| `StorageEntity.Awake` | If the storage belongs to a vehicle with a trunk size, sets `SlotCount` and `DisplayRowCount` before the slots are built |
| Preferences loaded or saved | Re-applies the settings to every vehicle in the world |

Nothing runs per frame. Removing the mod returns the game to normal.

## For developers

```bash
make test MOD=configurable-vehicles
make build MOD=configurable-vehicles
```

The canary tests read the game's DLLs in `lib/` and fail if a patch target or field has gone.
