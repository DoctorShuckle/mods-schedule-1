# Configurable Storage

A mod for **Schedule I** that lets you give each player-placed storage container more slots: the three
storage racks, the four storage closets and the safe. **Out of the box every one of the 8 has 20 slots in 2 rows** (10 across); change or zero the settings to get something else.

## Settings

Settings live in `UserData/MelonPreferences.cfg`. They appear after the game has started once with
the mod installed.

**General**, under `[ConfigurableStorage]`:

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `VerboseLogging` | `false` | Logs each container the mod resizes with the game's own size, and lists every other placeable storage item the game has |

**Per container**, one block each, `[ConfigurableStorage_<id>]`, for `smallstoragerack`,
`mediumstoragerack`, `largestoragerack`, `smallstoragecloset`, `mediumstoragecloset`,
`largestoragecloset`, `hugestoragecloset` and `safe`:

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `Slots` | `20` | Slots in the container, 1 to 20. `0` leaves the game's size. A value below the game's own is allowed (see below) |
| `Rows` | `2` | Rows the storage screen uses, 1 to 5. `0` is automatic: the game's width is kept and rows are added as slots are added |

Set both to `0` to get the game's own sizes back. A value that cannot be used (negative) counts as `0`, and one above 20 is capped at 20.

The game's own sizes (racks 4, 6 and 8 slots) are not worked out for the closets and the safe: turn on
`VerboseLogging` once to see them in the log.

## Things to know

- **Shrinking drops items.** A saved container keeps its items by slot number, so if you set a size below
  the game's own (or below what you raised it to earlier), or remove the mod after raising one, the items in
  slots that no longer exist are lost on the next load and save. **Empty the container first.**
- **Applies to containers built or loaded after the setting is read.** The size is set when a game starts
  (hosting, loading, joining, tutorial) and when you save the config. A container already standing keeps
  its size until the save is loaded again.
- **Rows.** The storage screen lays slots out in a fixed number of rows, and the width follows from the
  slot count, so the default 20 slots in 2 rows is 10 across and may be wide on a small screen: try `Rows` 4 for 5 across. With `Rows` at `0` a container the game shows 2 across stays 2 across and gains rows, up to
  5, after which it gets wider (20 slots in 5 rows is 4 across). Set `Rows` to choose the shape yourself.
- **The shelf pictures are separate.** A container draws the items it holds on its shelves until the
  physical space is full; extra slots still hold items, they just stop being drawn.
- **Whether the storage screen copes with 20 slots is not checked.** The screen has a fixed number of slot
  pictures. If a size looks wrong, report it with `VerboseLogging` on.
- Not changed: stack sizes, vehicle trunks (see Configurable Vehicles), employee beds and fixed world containers.
- Saves no data of its own: the game's own container save is unchanged.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `ConfigurableStorage.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Each machine builds a container's slots itself from its own prefab, and the game refers to slots by number.
Every player, the host included, needs the mod **and the same settings**, or inventories will not line up.

## Status

Both builds compile and pass the unit and canary tests. **Not run in game.** Still to confirm in game:
that the safe is a placeable storage item like the racks and closets (the log warns if it is not found),
that editing the prefab takes effect on IL2CPP, and that the storage screen shows 20 slots.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `LoadManager.StartGame`, `LoadAsClient`, `LoadTutorialAsClient` | Before any container is built, finds each listed item in the registry and sets `SlotCount` and `DisplayRowCount` on the `StorageEntity` of its prefab |
| `OnPreferencesLoaded` / `OnPreferencesSaved` | Does the same again, so edits to the config reach containers built afterwards |

Each placed container is made by instantiating the item's prefab, and `StorageEntity` builds its slots from
`SlotCount` while that happens, so no per-container patch is needed and nothing runs per frame.
