# Mix Packaged Drugs

A mod for **Schedule I** that lets you put **packaged** product straight into a mixing station. A baggie
is 1 unit, a jar 5 and a brick 20, and the ratio stays one mixer per unit: a brick needs 20 mixers.

The package is unpacked as it goes in, and the station then mixes as the game always does, so the hands-on mix,
the Chemist and the batch size are untouched. A package that goes onto loose product of the same kind (same
product and quality) joins that stack. The output always comes out loose: the mod does not pack it again. For a
brick out, see the [Brick Mixer](../brick-mixer/README.md) mod.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[MixPackagedDrugs]`.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `AllowBaggies` | `true` | Baggies (1 unit) may go into the product slot |
| `AllowJars` | `true` | Jars (5 units) may go into the product slot |
| `AllowBricks` | `true` | Bricks (20 units) may go into the product slot |
| `VerboseLogging` | `false` | Logs each package the mod unpacks, cuts or cancels |

Changes made in the preferences file while the game runs are picked up.

## Things to know

- **Packages and loose product mix freely.** A baggie, jar or brick can go onto a loose stack of the same
  product and quality, by dragging, quick-move or a Chemist's transit, and the units add up. A different product
  or quality is refused (the game's own rule for stacks).
- **Whole packages only.** A package that does not fully fit the slot's room is refused, never split. Dragging
  a stack of packages onto the slot with the mouse is cut down to what fits and the rest stays where it was.
- **The output is loose.** A mixing station mixes at most `MaxMixQuantity` units at a time (10 by default; the
  real value is in the game's assets and has not been read), so a brick comes out as loose batches.
- **Brick Mixer is left alone.** The [Brick Mixer](../brick-mixer/README.md) station is recognised by its item
  ID and keeps its own bricks-only rules, so the two mods can be installed together (not tested).
- **Brick press and packaging station are unchanged.** Only the mixing stations' product slot is opened.
- **A drag onto a locked slot, or from a slot the game does not let you drag from, does nothing**, as in the
  game.
- **Saves no data of its own.** Packages are unpacked at once, so no packaged item is ever saved in a station.
  Removing the mod leaves nothing behind.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `MixPackagedDrugs.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Not tested. Whoever puts a package in (host or client) unpacks it on their own machine before the game's code runs, and
the game's own station networking carries the loose result. A player without the mod cannot drop a package in,
because their game still refuses it, but is not otherwise affected. So the mod is needed by each player who
wants to drop packages in.

## Status

Both builds compile and pass the unit and canary tests. **Not run in game.** Unchecked there: that the product
stack limit is at least 20 (a brick fits only then), `MaxMixQuantity`, the swap inside `SetStoredItem` on
IL2CPP, and multiplayer.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `ItemSlot.DoesItemMatchHardFilters` | Says yes to a packaged product, for a mixing station's product slot only |
| `ItemSlot.GetCapacityForItem` | Counts the room for a packaged product in whole packages, from the game's answer for the same product unpackaged |
| `ItemSlot.AddItem` | Swaps a package added to the product slot for the same product unpackaged, so the game stacks it with loose product (the Brick Mixer's slot is skipped) |
| `ItemSlot.SetStoredItem` | Swaps a packaged stack stored in that slot (an empty slot) for the same product unpackaged (quantity times units) |
| `ItemUIManager.EndDrag` | Decides a drag of packages onto the product slot (`Packages.PlanDrop`): cut to the whole packages that fit, cancel, or, onto a loose stack, add the units there and cancel the game's swap |

## For developers

```bash
make test MOD=mix-packaged-drugs
make build MOD=mix-packaged-drugs
```

See [PROPOSAL.md](PROPOSAL.md) for the investigation behind the design.
