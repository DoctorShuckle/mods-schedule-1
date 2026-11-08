# Mix Packaged Drugs: proposal

Status: **built** (compiles, tests pass), **not run in game**. Everything below comes from the Mono decompile.

"As built" is current. The sections after it are the original investigation and design; where they differ
(an event-driven unpack, a filter swap, the setting names, the host-only unpack, the patch list), "As built" wins.

## As built

- The hooks are on `ItemSlot` (`DoesItemMatchHardFilters`, `GetCapacityForItem`, `AddItem`, `SetStoredItem`), not on the
  station's setup or a slot event: the filter list is protected, and a managed `ItemFilter` subclass is awkward on
  IL2CPP. Each hook checks that the slot is a mixing station's product slot.
- The inventory drag drops a stack into an empty slot without a capacity check (`ItemUIManager.EndDrag`), so a
  slot can briefly or lastingly hold more units than the stack limit. Accepted; documented in the README.
- A package cannot stack on a loose stack in the game (`CanStackWith` is false), so the mod unpacks it first:
  `AddItem` and `SetStoredItem` (a prefix on each, on whichever machine does it) swap the package for loose product, `GetCapacityForItem` asks the game for the room for the
  unpackaged copy and divides by the package's units, and a drag onto a loose stack adds the units itself
  (the game would swap the stacks).
- Output repack, its remembered packaging, its file in `UserData` and the save and destroy hooks were removed:
  the standard station's output is loose. Packing a brick out is the Brick Mixer mod's job.
- A stack of packages dragged onto the slot is cut down by an `ItemUIManager.EndDrag` prefix, because
  `BaseItemInstance.ChangeQuantity` refuses to change a stack that is over its limit.
- The Brick Mixer's station (item ID `brickmixer`) is skipped, so the two mods do not both act on its slot.
- The drop decision is `Packages.PlanDrop` in `Rules/`, so it is tested; locked slots and `CanDragFromSlot` are
  honoured, and `onItemMoved` is invoked after a merge.
- Settings are `AllowBaggies`, `AllowJars`, `AllowBricks`, `VerboseLogging`. No refund option.
- Still unchecked in game: the product `StackLimit`, `MaxMixQuantity` on Mk1/Mk2, the `SetStoredItem` swap on
  IL2CPP, and multiplayer.

## Summary

Let the mixing station's product slot accept **packaged** product (baggie, jar, brick) and keep the ratio at
one mixer per **unit** of drug. A bag is 1 unit, a jar 5, a brick 20, so a brick needs 20 mixers. The result is
the same unpackaged mixed product the station makes today.

## Verdict

Feasible, and small. The game already counts units of packaged product (`ProductItemInstance.Amount` is
`AppliedPackaging.Quantity`), and the only thing stopping a packaged stack is one slot filter.

## What the game does today

- `MixingStation.Awake` gives `ProductSlot` an `ItemFilter_UnpackagedProduct` and `MixerSlot` an
  `ItemFilter_MixingIngredient` (`MixingStation.cs` ~1820).
- `ItemFilter_UnpackagedProduct` rejects any `ProductItemInstance` whose `AppliedPackaging != null`. The same
  class guards `PackagingStation` and `BrickPress`, so it cannot be patched globally.
- `GetMixQuantity()` is `min(ProductSlot.Quantity, MixerSlot.Quantity, MaxMixQuantity)`. It counts **stack
  items, not units**, and nothing in it knows about `Amount`.
- Three callers consume the slots with that number: `MixingStationInterface.BeginMix`, `UseMixingStationTask`
  (the hands-on mini-game, which also spawns one bowl item per unit) and `StartMixingStationBehaviour`
  (the Chemist). Each does `ProductSlot.ChangeQuantity(-n)`, `MixerSlot.ChangeQuantity(-n)` and builds a
  `MixOperation(product.ID, product.Quality, mixer.ID, n)`. `MixOperation` carries no packaging.
- `TryCreateOutputItems` makes `knownProduct.GetDefaultInstance(Quantity)`, which is unpackaged.
- `ItemSlot.AddItem` and `SetStoredItem` do **not** check hard filters. The filters are consulted by callers
  through `GetCapacityForItem` / `DoesItemMatchHardFilters` (inventory UI, `MoveItemBehaviour`, quick-move).

## Design

**Unpack on entry.** Open the slot to packaged product, then turn a packaged stack into an unpackaged stack the
moment it lands in `ProductSlot`. After that the game's own mixing code sees ordinary units and nothing else
changes: 20 units still need 20 mixers, `MaxMixQuantity` still batches them, and the three consumers, the
mini-game and the Chemist all work unmodified. Anything left over after a batch stays in the slot as unpackaged
product.

1. **Open the filter.** Postfix on `MixingStation` setup replaces the `ItemFilter_UnpackagedProduct` on
   `ProductSlot` with a filter accepting any product, packaged or not (same category check as the original).
   The slot lists filters through a protected property, so this is a `GameAccess` helper (Mono: reflection /
   `AccessTools`; IL2CPP: the proxy exposes it). No new filter class is needed if swapping the list is awkward:
   a replacement `ItemFilter` subclass is enough on Mono, but on IL2CPP a managed subclass of a game type is
   hard, so the fallback is to patch `ItemFilter_UnpackagedProduct.DoesItemMatchFilter` with a prefix that
   returns true when a flag set by the station's own checks is on. The prototype decides which.
2. **Be unit-aware about capacity.** The filter says yes, but `ItemSlot.GetCapacityForItem` returns
   `StackLimit - Quantity` in *items*. For a packaged item that must become
   `(StackLimit - Quantity) / Amount` packages, otherwise a 20-unit brick is accepted into a slot that holds
   15 units. Postfix on `GetCapacityForItem`, limited to the mixing station's product slot.
3. **Unpack, server only.** Subscribe to `ProductSlot.onItemInstanceChanged` / `onItemDataChanged` (an event, so
   no polling). If `InstanceFinder.IsServer` and the slot holds a packaged `ProductItemInstance`, replace it with
   `new ...Instance(def, Quantity * Amount, Quality, null)` through the slot's replicated setter
   (`ReplicateStoredInstance` path, i.e. `SetStoredInstance`), guarded against re-entry. Use the typed copy
   (`GetCopy` returns the Weed/Meth/Cocaine/Shroom subclass) and then `SetPackaging(null)` so subtype fields
   (e.g. cocaine and meth appearance) survive.
4. **Mixer unchanged.** One mixer per unit already holds because the product slot is now in units.

### Why not patch `GetMixQuantity`

It would need changes in three consumers that each remove `n` items from a slot and would have to remove
`n / Amount` packages, split a stack that is not evenly divisible, and keep the mini-game's bowl item count
right. Unpacking first avoids all of it.

## Behavior to decide

| Question | Proposal |
|---|---|
| Packaging materials | Consumed (the bag, jar or brick is gone). Setting `RefundPackaging` is a possible later option; it would need a way to put empty packaging into the player's inventory. |
| Partial fit | A package that does not fully fit is refused; it is never split. |
| Stack limit | The product's `StackLimit` is asset data, not in code. Read it at runtime; if it is below 20, bricks never fit and the README must say so. |
| Settings | `AllowBagged`, `AllowJarred`, `AllowBricked` (all true), plus `VerboseLogging`. |

## Patch list

| Target | Kind | Purpose | IL2CPP risk |
|---|---|---|---|
| `MixingStation` setup (`Awake`/`Start`) | Postfix | Open `ProductSlot` filter, subscribe to its change event | `Awake_UserLogic` may be what runs; patch what the canary shows is called |
| `ItemSlot.GetCapacityForItem` | Postfix | Unit-aware capacity for the product slot | Virtual and hot; check the override list and that it is not inlined |
| `ProductSlot.onItemInstanceChanged` handler | Event | Unpack on server | None |

## Multiplayer

As built, whoever drops the item unpacks it locally and the loose result rides the game's own slot
replication. A client without the mod cannot drop a packaged item into the slot, because its own filter still
refuses it, so the **mod is needed by each player who wants to drop packages in**.

## Risks and open questions

- The inventory drag/drop path may check the filter somewhere other than `GetCapacityForItem`. Find it before
  coding (look at `ItemSlotUI` and `PlayerInventory.TryInsertItemIntoSet`).
- `MoveItemBehaviour` (employee delivery) uses `DoesItemMatchHardFilters`; once the filter opens, packaged product
  routed to a mixing station by a workers' route will be accepted, which is a bonus but changes behavior.
- `MaxMixQuantity` on the station prefabs (Mk1 vs Mk2) is asset data. A 20-unit brick is mixed in two batches on
  a station with 10; confirm at runtime.
- Packaged items in a station that is saved before unpacking is impossible, since unpacking is immediate, so
  there is nothing new to persist. Confirm that `MixingStationData` loading, which bypasses the event, never
  restores a packaged item (it cannot, because it only restores what was saved after unpacking).
- Unverified: the exact hook that makes the vanilla UI show the slot as droppable.

## Test plan

- Rules (`Rules/`): `UnitsOf(quantity, amount)`, `PackagesThatFit(stackLimit, current, amount)`, setting
  validation.
- Canary: `MixingStation` setup method, `ItemSlot.GetCapacityForItem`, `ItemSlot.onItemInstanceChanged`,
  `ProductItemInstance.AppliedPackaging` / `SetPackaging`, `PackagingDefinition.Quantity`, and the
  `ProductSlot` field, on both flavors.
- In game: drop a bag, a jar and a brick; confirm 20 mixers are needed for a brick; run the hands-on mix and a
  Chemist mix; check the output stack and a save/reload mid-mix.

## Next steps

1. Run the open-questions search in the decompile (drag/drop path).
2. Scaffold from `configurable-atm` as `mix-packaged-drugs` / `MixPackagedDrugs`.
3. Prototype the unpack on entry and test in game on IL2CPP first.
