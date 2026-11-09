# Configurable Storage: context for the next agent

Read this first when the mod stops working after a game update. [README.md](README.md) is for players; this
file is for whoever has to fix it. Repo-wide rules are in `../../CLAUDE.md`.

## Version this was written against

| Thing | Value | Source |
| --- | --- | --- |
| Game | **Schedule I 0.4.7f12** | `Game Version:` line of a MelonLoader `Latest.log` from the IL2CPP install, 2026-11-02 (kept at the repo root as `Latest.log`, gitignored) |
| Unity | 2022.3.62f2 | same log |
| MelonLoader | 0.7.3 Open-Beta, IL2CPP, net6 | same log |
| Mod | 1.0.0 | `[MelonInfo]` in `src/common/ConfigurableStorageMod.cs` |
| `lib/` | copied 2026-10-28 | file dates of `lib/*/Assembly-CSharp.dll` |

Not recorded: the game version of the **Mono** `lib/mono` copy. It is assumed to match 0.4.7f12 but was
never checked. If the canary tests pass on IL2CPP and fail on Mono, suspect a version mismatch in `lib/`
before suspecting the game.

**Not run in game.** Everything below marked "unverified" is an inference from the decompile and the asset
strings. Only builds and the 24 unit and canary tests have run. Treat the first in-game run as the real test.

## What the mod does, in one paragraph

The 8 player-placed containers are `PlaceableStorageEntity` objects. The game builds one by
`Instantiate(buildableItemDefinition.BuiltItem.gameObject)` (`BuildManager.CreateGridItem`), and
`StorageEntity.Awake` creates its slots from `SlotCount` during that call. The mod edits `SlotCount` and
`DisplayRowCount` on the **prefab's** `StorageEntity` (found through `Registry.GetAllItems()`, keyed by item
ID) before any container exists. Defaults: 20 slots, 2 rows. `StorageMenu.Open` uses `DisplayRowCount` as the
grid's row constraint; the column count follows from slots and rows.

## The 8 item IDs and how they were found

| Name | ID |
| --- | --- |
| Small / Medium / Large Storage Rack | `smallstoragerack`, `mediumstoragerack`, `largestoragerack` |
| Small / Medium / Large / Huge Storage Closet | `smallstoragecloset`, `mediumstoragecloset`, `largestoragecloset`, `hugestoragecloset` |
| Safe | `safe` |

The IDs are asset data, not in any decompiled C#. They were read with `strings` from
`Schedule I_Data/sharedassets0.assets` on the Steam Deck (`ssh steamdeck`, game dir in `etc/local.json`),
next to each item's display name and description. Raw `strings` output shows a stray trailing byte on some
(`largestoragerack6`, `smallstoragerack;`, `safe0`): that is the next field, because Unity pads strings to
4 bytes. The racks' wiki sizes are 4, 6 and 8 slots; the closets' and the safe's sizes are **unknown**.
The closets' prefabs are named `Storage Rack Mk2 (Small|Medium|Large|Huge)`; the racks `1.0x0.5_storagerack`
and similar.

If the game renames or adds container items, the list is in `src/common/Rules/Containers.cs` and nothing
else. Turn on `VerboseLogging`: the mod then logs `Not handled: <id> (<name>), N slots in M rows` for every
placeable storage item that is not in the list, which is how to find new IDs without touching asset files.

## Where it can break, and how to tell

Run `python3 scripts/test.py -m configurable-storage` after refreshing `lib/` (the canary tests read the DLLs
of **both** flavors and skip when `lib/` is empty). A failure names the missing member. Then the in-game log.

| Symptom | Likely cause | Where to look |
| --- | --- | --- |
| Log: `N/M patches applied` with N < 3, or `Patch ... failed` | `LoadManager.StartGame`, `LoadAsClient` or `LoadTutorialAsClient` renamed, overloaded or moved | `src/common/Patches/StoragePatches.cs`; canary `Load_patch_targets_are_declared_once`; `decompiled/mono/Assembly-CSharp/ScheduleOne.Persistence/LoadManager.cs` |
| Warning `Container <id> (...) was not found as a placeable storage item` | Item ID renamed or removed, or the item's `BuiltItem` is no longer a `PlaceableStorageEntity` (the safe is the most likely to differ: **unverified** that it is one) | `Containers.cs`; `GameAccess.ApplyAll`; verbose log lists what exists |
| No warnings but sizes unchanged | The prefab edit did not reach the instances: `Instantiate` no longer clones this prefab, `SlotCount` is read somewhere else, or on IL2CPP the edit did not stick (**unverified**) | `BuildManager.CreateGridItem`; `StorageEntity.Awake_UserLogic_...` (the slot loop) |
| Sizes right in the log, wrong in game | Another mod or the game sets `SlotCount` after Awake; or an old config value (see Config below) | the `[ConfigurableStorage_<id>]` block in `UserData/MelonPreferences.cfg` |
| Slots exist but 8+ show no slot UI | `StorageMenu.SlotsUIs` (a fixed prefab array) is shorter than the new count (**length unknown**) | `ScheduleOne.UI/StorageMenu.cs`, `Open(...)`: slots with index >= `SlotsUIs.Length` get no UI. A fix would clone the last `ItemSlotUI` |
| Menu overflows the screen / close button misplaced | Row count too low for the slot count, or the layout code changed | `StorageMenu.Open`: `CloseButtonContainer.anchoredPosition` is computed from `constraintCount * cellSize.y`; `Rows` setting |
| Players see different inventories / index errors in RPCs | Host and clients have different sizes: the slot count is not synced | README "Multiplayer"; every player needs the same config |
| Items missing after loading | A size below what the save was made with: shrinking drops items past the new count (`ItemSet.LoadTo` fills by index) | by design, documented |
| Shelf pictures missing items | `StorageVisualizer` only draws what fits the physical `StorageGrid` tiles and destroys the rest | by design; cosmetic |

### The members the mod depends on

All checked by `tests/Canary/GameApiCanaryTests.cs`:

- `Persistence.LoadManager`: `StartGame`, `LoadAsClient(string)`, `LoadTutorialAsClient()`, each declared once.
- `Registry.GetAllItems()`; `ItemFramework.ItemDefinition.ID` and `.Name` (strings).
- `ItemFramework.BuildableItemDefinition.BuiltItem` of type `BuildableItem`;
  `ObjectScripts.PlaceableStorageEntity` derives from `EntityFramework.BuildableItem` and has a `StorageEntity` member.
- `Storage.StorageEntity.SlotCount` and `.DisplayRowCount` (int; a public field on Mono, a public property in
  the IL2CPP proxy) and an `Awake` method.

Not tested by the canary because it is a Unity-side fact: that the `PlaceableStorageEntity` prefab's
`StorageEntity` reference points at the component that builds the slots (it does in the decompile:
`PlaceableStorageEntity.Start` copies `StorageEntity.ItemSlots` into its input and output slots).

## Design decisions that are easy to undo by accident

- **Prefab edit, not an `Awake` patch.** At `StorageEntity.Awake` the owning item's `ItemInstance` is not set
  yet (`GridItem.InitializeGridItem` runs after `Instantiate`), so a patch there cannot tell a rack from a
  closet. The prefab edit knows the ID. Do not "simplify" this into an Awake prefix.
- **Original sizes are remembered** (`GameAccess.GameSizes`, read once per ID) so the automatic row count and
  `0 = as in the game` stay correct after the prefab has been edited and re-edited.
- **Resizing runs on game start and on preference save**, never per frame (repo rule: no polling). A container
  already standing is not resized until the save reloads.
- **Beds are excluded by accident, not by code:** `BedItem` is a `PlaceableStorageEntity` but its ID is not in
  the list. `WorldStorageEntity` (map containers) and vehicle trunks are different paths.
- **Shrinking is allowed on purpose** (the user chose it over a raise-only floor). The README and the setting
  text warn about lost items.
- `configurable-vehicles` also sets `SlotCount` and `DisplayRowCount`, through a prefix on `StorageEntity.Awake`
  for vehicle trunks only. The two mods do not overlap, since this one edits placeable prefabs.

## Config pitfalls

- Defaults are `Slots = 20`, `Rows = 2` (`ModConfig.DefaultSlots`, `DefaultRows`). MelonLoader keeps values
  already written to `UserData/MelonPreferences.cfg`, so a machine that ran an earlier build keeps `0` until the
  block is edited or deleted. When debugging, read the cfg first.
- `Slots` is clamped to 1 to 20 (the game's `[Range(1,20)]` and `MAX_SLOTS`; `MAX_SLOTS` is never read by the
  game). `Rows` to 1 to 5 and never above the slot count. Logic and tests: `Rules/StorageSizing.cs`,
  `tests/Rules/StorageSizingTests.cs`.

## Reading the game

- Logic is in `decompiled/mono/Assembly-CSharp/`: `ScheduleOne.Storage/StorageEntity.cs`,
  `ScheduleOne.UI/StorageMenu.cs`, `ScheduleOne.ObjectScripts/PlaceableStorageEntity.cs`,
  `ScheduleOne.Building/BuildManager.cs`, `ScheduleOne/Registry.cs`, `ScheduleOne.Persistence/LoadManager.cs`.
  `decompiled/il2cpp/` is signatures only. Regenerate both after a game update (commands in `../../CLAUDE.md`).
- Item names, IDs, prefab names and the default slot and row values are **not** in the decompile. They are in the
  asset files (`sharedassets0.assets`, `level*`) or visible at runtime with `VerboseLogging`.
- After a game update: refresh `lib/` and `decompiled/`, run the tests, bump the version row above and the
  `lib/` date, deploy, run once with `VerboseLogging` on, and compare the logged game sizes with this file.

## Unverified, in the order they will matter

1. The safe is a `PlaceableStorageEntity` like the racks and closets.
2. Editing the prefab's `SlotCount` and `DisplayRowCount` takes effect on IL2CPP.
3. `StorageMenu.SlotsUIs` holds at least 20 slot pictures, and 20 slots in 2 rows (10 across) looks acceptable.
4. The registry exists, and lists these items, by the time `LoadManager.StartGame` runs. (`brick-mixer` relies
   on the same hook for `Registry.AddToRegistry`, which is encouraging, not proof.)
5. The game versions of the two `lib/` folders match.
