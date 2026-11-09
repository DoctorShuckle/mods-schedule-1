# Mix Packaged Drugs: proposal

Status: **built** (compiles, tests pass), **not run in game**. Everything below comes from the Mono decompile.

## As built

- The hooks are on `ItemSlot` (`DoesItemMatchHardFilters`, `GetCapacityForItem`, `AddItem`, `SetStoredItem`), not on the
  station's setup or a slot event: the filter list is protected, and a managed `ItemFilter` subclass is awkward on
  IL2CPP. Each hook checks that the slot is a mixing station's product slot.
- A package cannot stack on a loose stack in the game (`CanStackWith` is false), so the mod unpacks it first:
  `AddItem` and `SetStoredItem` (a prefix on each, on whichever machine does it) swap the package for loose product,
  `GetCapacityForItem` asks the game for the room for the
  unpackaged copy and divides by the package's units, and a drag onto a loose stack adds the units itself
  (the game would swap the stacks).
- Output repack, its remembered packaging, its file in `UserData` and the save and destroy hooks were removed:
  the standard station's output is loose. Packing a brick out is the Brick Mixer mod's job.
- A stack of packages dragged onto the slot is cut down by an `ItemUIManager.EndDrag` prefix: the game drops into
  an empty slot without a capacity check, and `BaseItemInstance.ChangeQuantity` refuses to change a stack that is
  over its limit.
- Every mixing station is treated alike, the Brick Mixer's included: it takes packages through this mod, and
  its own mod no longer carries any intake code.
- The drop decision is `Packages.PlanDrop` in `Rules/`, so it is tested; locked slots and `CanDragFromSlot` are
  honoured, and `onItemMoved` is invoked after a merge.
- Settings are `Enabled` (the whole mod) and `VerboseLogging`. All three package sizes are always allowed. No
  refund option.
- Still unchecked in game: the product `StackLimit`, `MaxMixQuantity` on Mk1/Mk2, the `SetStoredItem` swap on
  IL2CPP, and multiplayer.

## What the game does (from the Mono decompile)

- `MixingStation.Awake` gives `ProductSlot` an `ItemFilter_UnpackagedProduct`, which rejects any
  `ProductItemInstance` whose `AppliedPackaging != null`. The same class guards `PackagingStation` and
  `BrickPress`, so it cannot be changed globally; the mod answers in `DoesItemMatchHardFilters` for the one slot.
- `GetMixQuantity()` is `min(ProductSlot.Quantity, MixerSlot.Quantity, MaxMixQuantity)`: it counts stack items,
  not units. Three callers (`MixingStationInterface.BeginMix`, `UseMixingStationTask`,
  `StartMixingStationBehaviour`) take that many items from both slots and build a `MixOperation`, which carries no
  packaging. So the mod unpacks first (a brick becomes 20 units) and leaves the mixing code alone. Patching
  `GetMixQuantity` instead would need changes in all three callers.
- `ItemSlot.AddItem` and `SetStoredItem` do not check hard filters; callers do, through `GetCapacityForItem` and
  `DoesItemMatchHardFilters`.
- `ProductItemInstance.CanStackWith` is false between a packaged and a loose stack, and `GetCapacityForItem` is
  `StackLimit - Quantity` in items.
- `ItemUIManager.EndDrag` drops into an empty slot without a capacity check, swaps two stacks that cannot stack,
  and `BaseItemInstance.ChangeQuantity` refuses to change a stack that is over its limit.

## Open questions (all need the game)

- The product `StackLimit` is asset data. A brick fits only if it is at least 20.
- `MaxMixQuantity` on the Mk1 and Mk2 is asset data (10 assumed), so a brick mixes in several loose batches.
- The `SetStoredItem` prefix swapping the item on IL2CPP.
- Employee delivery (`MoveItemBehaviour`) uses `DoesItemMatchHardFilters`, so packaged product on a worker's
  route to a mixing station will now be accepted.
- Multiplayer: not tested.

## Test plan

- Rules: `Packages.Units`, `PackagesForRoom`, `PlanDrop`.
- Canary: every patched method and the members the mod reads, on both flavors.
- In game: drop a baggie, a jar and a brick; confirm a brick needs 20 mixers; drag, quick-move and a Chemist's
  transit onto a partial loose stack; a drag onto a locked slot; run the hands-on mix and a Chemist mix.
