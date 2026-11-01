# Configurable Employees

A mod for **Schedule I** that lets you tune your employees: what they cost, how fast they walk, how
long each of their jobs takes, how much you can give them to look after, what the Fixer charges, and
how many employees each property can hold.

It replaces *Better Employees* and *Ultimate Employees*, which stopped working on current versions of
the game. It is a rewrite, not a patch of either one, and it does nothing until you change a setting.

## What you can change

Every number defaults to `-1`, which means "leave the game's value alone". Set a number to override it.

Settings live in `UserData/ConfigurableEmployees.cfg`, created on first launch. Each type of employee
has one section holding everything for them, job times included: `[ConfigurableEmployees_Botanist]`,
`_Chemist`, `_Packager` and `_Cleaner`. The Fixer, the properties and the mod's own options have
sections too.

### Every employee

| Setting | What it does |
| ------- | ------------ |
| `DailyWage` | What the employee is paid each day. `0` is allowed. |
| `SigningFee` | What hiring one costs, before the Fixer's extra fee. `0` is allowed. |
| `WalkSpeed` | Metres per second. Up to 20. |
| `MaxAssigned` | The most things one employee can be given, 1 to 100. Pots, drying racks, mushroom beds and spawn stations for a botanist (game: 8), stations for a chemist (4) and a packager (3), trash bins for a cleaner (6). |

### Packager only

| Setting | What it does |
| ------- | ------------ |
| `MaxRoutes` | The most routes one packager can have, 1 to 20 (game: 5). |
| `SpeedMultiplier` | How fast packagers pack. `1` is the game's speed, `2` is twice as fast. Up to 100. |

### How long jobs take

Each job has a setting, in seconds, named after it. The value is the job's **base time**, the same
kind of number the game uses (the settings file shows the game's own next to each one). `5` for a
10-second job makes it twice as fast.

| Employee | Setting | The job | Game |
| -------- | ------- | ------- | ---- |
| **Botanist** | `SoilSeconds` | Pouring soil into a pot | 10 |
| | `WaterSeconds` | Watering a pot, or misting a mushroom bed | 10 |
| | `AdditiveSeconds` | Applying one additive | 10 |
| | `SowSeconds` | Sowing one seed | 15 |
| | `HarvestSecondsPerPlant` | Harvesting, for each plant or mushroom | 1 |
| | `SpawnSeconds` | Putting spawn on a mushroom bed | 15 |
| | `DryingRackSecondsPerItem` | Putting each item on a drying rack | 1 |
| | `SpawnStationSeconds` | Working a spawn station | 6 |
| | `MoveItemSeconds` | Each of the two steps (pick up, put down) of moving an item along a route | 0.5 |
| **Chemist** | `ChemistryStationSeconds` | A chemistry station's cook. Its three stages (8 + 6 + 6) change together | 20 |
| | `LabOvenLoadSeconds` | Loading a lab oven | 5 |
| | `LabOvenUnloadSeconds` | Emptying a finished lab oven | 10 |
| | `MixingSecondsPerItem` | Mixing each item at a mixing station | 1 |
| | `CauldronSeconds` | Starting a cauldron | 15 |
| | `MoveItemSeconds` | As for the botanist | 0.5 |
| **Packager** | `PackagingStationSeconds` | Packing one item at a packaging station, before the speed multipliers | 5 |
| | `BrickPressSeconds` | Pressing one brick, before the speed multiplier | 15 |
| | `MoveItemSeconds` | As for the botanist | 0.5 |
| **Cleaner** | none | See below | |

A job time must be between 0.1 and 3600 seconds. Each worker who moves items has their own
`MoveItemSeconds`, so speeding up packagers does not speed up chemists.

**What a job time changes, and what it does not.** Only the part of a job that the game itself
divides by the worker's speed changes. Walking to the station, and the short fixed pauses (opening an
oven door, grabbing an item) stay as they are. Because the setting is a base time, anything else that
changes a worker's speed still applies on top of it, and the packager `SpeedMultiplier` still
applies to `PackagingStationSeconds` and `BrickPressSeconds`: with `SpeedMultiplier = 2`, a
`PackagingStationSeconds` of 5 packs in 2.5 seconds (the packaging station's own speed multiplier,
which the game also applies, comes on top of that).

**Cleaners have no job times.** Their waits (bagging a trash can takes 3 seconds, for example) are
fixed in the game and do not depend on anything the mod can reach. Walk speed is the only way to make
them faster.

### Fixer

The Fixer charges an extra fee on top of each employee's signing fee, based on how many people you
have hired in the whole save: 100 for each of the first six hires and 250 for every later one, never
more than 500 in total. These settings change those numbers; their defaults are the game's, so
nothing changes until you do.

| Setting | Game | What it does |
| ------- | ---- | ------------ |
| `EarlyHireFee` | 100 | The extra fee for each early hire |
| `EarlyHireCount` | 6 | How many hires count as early |
| `LaterHireFee` | 250 | The extra fee for every hire after that |
| `FeeCap` | 500 | The most the extra fee can be |

### Properties

One setting per property, giving the most employees it can hold (1 to 50):

`Barn` (game: 10), `Bungalow` (5), `DocksWarehouse` (12), `Manor` (12), `StorageUnit` (3), `Sweatshop` (1).

These are the properties that can hold employees. The RV, the motel room, the sewer office and the
businesses cannot (the game gives them a capacity of 0, and leaves the first two and the businesses out of
the Fixer's menu), so they have no setting. The game's own capacities are logged at startup, and the
settings file shows each one next to its setting.

If you ask for more employees than a property has idle points for, the mod adds idle points beside
the existing ones. Employees may stand close together.

### Example

```toml
[ConfigurableEmployees_Chemist]
DailyWage = 250.0
WalkSpeed = 2.5
MaxAssigned = 6
CauldronSeconds = 5.0        # 15 s in the game, so three times as fast

[ConfigurableEmployees_Packager]
MaxRoutes = 10
SpeedMultiplier = 2.0
MoveItemSeconds = 0.25

[ConfigurableEmployees_Botanist]
SoilSeconds = 5.0
HarvestSecondsPerPlant = 0.5

[ConfigurableEmployees_Properties]
Barn = 15
StorageUnit = 5
```

Values that make no sense (negative wages, zero stations, a walk speed of zero) are ignored rather
than applied, and speeds, counts and times have an upper limit.

### When changes take effect

The mod reads the settings file when the game starts, and again whenever MelonLoader reports that the
preferences were loaded or saved. The mod does not watch the file itself, so a hand edit made while
the game runs only takes effect if something triggers that reload.

| Setting | After a reload |
| ------- | -------------- |
| Wage, signing fee, walk speed, `MaxAssigned`, `MaxRoutes`, `SpeedMultiplier` | Applied at once, to everyone already hired |
| Job times | From the next job each worker starts |
| Fixer fee | At once |
| `Verbosity` | At once |
| Property capacities, `SkipPatches` | Need a game restart |
| Setting a value back to `-1` | Needs a game restart (the mod never records the game's original values, so it cannot restore them mid-game) |

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `ConfigurableEmployees.dll` into the game's `Mods` folder.
3. Start the game.

There are two builds, one for each version of the game (the Mono branch and the main IL2CPP
branch). Use the one that matches your install. Remove *Better Employees* and *Ultimate Employees*
first; they change the same values. Any other mod that sets wages or worker speeds may fight with this
one over the same numbers.

## Multiplayer

Wages, fees and hiring are decided by the host. Use the same settings on every machine so that the
numbers on screen match.

## Things to know

- **More routes than the game's five** adds extra rows to the packager's management screen and makes
  the list scrollable (mouse wheel). The mouse wheel is blocked from changing the held item while
  that screen is open. More than the screen's existing rows for *stations* is not covered: the limit
  is raised, but the screen only has so many rows.
- **Job times work by changing the worker's speed**, not the game's timers. For the length of a job
  the mod multiplies the worker's work speed by the game's base time divided by yours. The work
  animation follows the speed, so a faster job also looks faster. For the botanist's jobs the change
  is held exactly while the job's timer is read. For the chemist's and packager's jobs it is held
  while the job is active, the walk to the station included.
- **A job time of the same length as the game's** changes nothing and is not logged as a change.
- **Properties for sale** show the game's capacity on their listing poster, not yours.

## Troubleshooting

- `Verbosity` (under `[ConfigurableEmployees_General]`) sets how much the mod logs. Include the log
  lines when reporting a problem.

  | Level | What is logged |
  | ----- | -------------- |
  | `0` | The startup summary, warnings and errors |
  | `1` | Also every change the mod makes to an employee, and each time a job's speed changes |
  | `2` | Also the start and stop of every timed job, with the worker, the time and the game clock |

  At level 2 a job looks like this. The `ran` figure is how long a botanist's job took, so it should
  match the time you set. For the other workers it is `active`, which also includes walking:

  ```
  [trace] Task start [22:31:38.958, game t=95.08] Ryan Campnell (ryan_campnell): Water, 10 s -> 5 s, work speed x2 held.
  [trace] Task stop  [22:31:46.013, game t=100.07] Ryan Campnell (ryan_campnell): Water, ran 4.99 s of game time, work speed restored.
  ```

  The game ends most jobs with two stop calls a few milliseconds apart, so a second `Task stop` line
  saying `nothing to restore` is normal.

- **The startup summary** reads like `15/15 patches applied`. A smaller number means a patch failed
  and that feature is off; the line above it says which and why. After a game update this is the
  first thing to check.

- **The `GetActionDuration address` lines** (IL2CPP only) list where the game keeps the code for each
  botanist job. Several jobs sharing an address is the reason the mod does not patch the game's own
  timers: the compiler merged them into one function, and patching it froze the game.

- `SkipPatches` (under `[ConfigurableEmployees_General]`) switches individual patches off. It takes
  comma-separated parts of patch names, ignoring case, and needs a game restart. The log says which
  were skipped, for example `15/15 patches applied (1 skipped)`.

  | Part of name | Turns off |
  | ------------ | --------- |
  | `TaskDuration` | all job times (botanist, chemist and packager) |
  | `Botanist_TaskDuration` | the botanist job times only |
  | `Worker_TaskDuration` | the chemist and packager job times only |
  | `Fixer` | the Fixer's fee setting |
  | `Property_Awake` | setting a property's capacity as it starts. Capacities are still set once the scene has loaded |
  | `RouteListFieldUI` | extra route rows and the scroll area |
  | `ManagementInterface` | the tracking of the management screen being open (so the mouse wheel is not blocked) |
  | `MouseScrollDelta` | blocking the mouse wheel while the management screen is open |
  | `InitializeNPC` | walk speed |
  | `Employee_Start` | wage, fee and limits for employees loaded from a save |
  | `EmployeeManager_Awake` | wage, fee and limits for new hires |

  Example: `SkipPatches = "TaskDuration, MouseScrollDelta"`.

## How it works

Nothing in the mod runs on a timer or a loop. Each setting is applied in response to something the
game does, at the point where the game creates or reads the number:

- **Wage, signing fee, packing speed and the packager/botanist limits** are fields on the employee
  *template* the game clones every hire from. The mod writes them when the game creates its employee
  manager, so every later hire (and the Fixer's price) uses them. Employees loaded from a save, and the
  limits the game builds from fixed numbers, are set when the employee starts.
- **Walk speed** is set after the game initialises the employee, because the game sets it itself at
  that point.
- **Job times** hold a work speed multiplier from the moment a job starts to the moment it ends, as
  described above. The botanist's jobs use the start and stop calls the game makes around the job.
  The other jobs use the game's general behaviour lifecycle (activate and resume, deactivate and
  pause), which every one of them goes through, and do nothing for anything that is not a known job.
- **The Fixer's fee** replaces the result of the game's fee calculation.
- **Property capacity** is set as each property starts, and again once the scene has loaded. The
  second pass exists because the bungalow, sweatshop and the other special properties start through
  code of their own that can skip the mod's first hook.
- **Settings** are re-applied when MelonLoader reports that the preferences were loaded or saved.

Every patch is a separate class and is applied on its own, so a game update that removes one method
turns off one feature and leaves the rest working. The mod saves no data of its own, so removing it
returns the game to normal.

## Compatibility

Written for Schedule I version 0.4.7. If it stops working after an update, check the MelonLoader log
for a "patch failed" message and the patch count at startup.

### What has been tested in the game

Run on the IL2CPP build, under Proton on a Steam Deck:

- All 15 patches apply.
- Wage, signing fee and walk speed.
- The botanist's job times (soil, water, mist, additive, sow, spawn and harvest). Each finished within
  a few hundredths of a second of the time set, and a harvest scaled with the number of plants.
- The chemist's and packager's job times, for the brick press, the cauldron, the lab oven and the
  chemistry station. Their jobs finished faster than the game's own base time plus its fixed pauses
  would allow. The exact times cannot be read from the log, because these jobs are timed from the
  start of the walk.
- Property capacities. The second pass finds every property, including the ones the first hook cannot
  see, and raising the bungalow from 5 to 8 worked: the mod added the idle points and the game shows 8.

Not yet confirmed in the game:

- `DryingRackSecondsPerItem` and `SpawnStationSeconds`, which have not come up in play yet, and the
  effect of each worker's `MoveItemSeconds` (its jobs include walking, so the log cannot isolate it).
- Employees standing at the idle points the mod adds, lowering a capacity, and capacities for
  properties other than the bungalow.
- The extra route rows and scroll area.
- The Mono build, which compiles and passes the tests but has not been run in the game.

## For developers

The code is in `src/common`, compiled unchanged into both builds (`src/mono` and `src/il2cpp` hold
only the project files). It is grouped by what it may touch:

| Folder | What is in it |
| ------ | ------------- |
| `Rules/` | The mod's rules as plain functions of plain values, with no game, Unity or MelonLoader type: how a setting is validated (`Setting`), the arithmetic of a job time (`TaskTiming`), the table of jobs (`TaskTimes`), the table of properties (`PropertyCatalog`), the Fixer's fee, and so on. The unit tests compile this folder on its own, so it cannot use a game type. |
| `Game/` | Everything that reads or changes the game: `GameAccess` (casts, lookups; the only place the Mono and IL2CPP builds differ), `TaskHold` (holding and releasing a job's speed), `EmployeeTuning`, `PropertyTuning`, `RouteListUi`, `FoldingCheck`. |
| `Patches/` | The Harmony patches, one file per feature. The mod finds them by their `[HarmonyPatch]` attribute, so a new patch class needs no registration. |
| `Config/` | The settings file (`ModConfig`) and logging (`ModLog`). |

To **add a job time**: add a row to `Rules/TaskTimes.cs` (the setting, the game's base time and its
text), and a line in `Game/TaskHold.cs` that maps the game's behaviour class to that row. The cfg entry
is created from the table. If the behaviour is a new class, add its name to the canary test too.

To build and test, from the repository root:

```bash
make test MOD=configurable-employees
make build MOD=configurable-employees
```

There are two kinds of tests. The rule tests need nothing but the .NET SDK. The canary tests read the
game's DLLs from `lib/` (reading their metadata only, nothing is run) and fail if a method or field
the mod depends on has gone, so after a game update: refresh `lib/`, run the tests, and you know
before launching the game. They check that members exist, not that the game still behaves the way the
mod assumes.
