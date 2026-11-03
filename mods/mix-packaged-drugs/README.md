# Mix Packaged Drugs

A mod for **Schedule I** that lets you put **packaged** product straight into a mixing station. A baggie
is 1 unit, a jar 5 and a brick 20, and the ratio stays one mixer per unit: a brick needs 20 mixers.

The station unpacks the package as it goes in and then mixes as the game always does, so the hands-on mix,
the Chemist and the batch size are untouched. When the mix finishes, the output is packed again in the same
packaging, if the batch fills whole packages: a 20-unit batch from a brick comes out as 1 brick, a 10-unit
batch from jars as 2 jars.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[MixPackagedDrugs]`.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `AllowBaggies` | `true` | Baggies (1 unit) may go into the product slot |
| `AllowJars` | `true` | Jars (5 units) may go into the product slot |
| `AllowBricks` | `true` | Bricks (20 units) may go into the product slot |
| `VerboseLogging` | `false` | Logs each package the mod unpacks |

Changes made in the preferences file while the game runs are picked up.

## Things to know

- **The output is packed again only when the batch divides evenly.** A station mixes at most `MaxMixQuantity`
  units at a time (10 by default; the real value is in the game's assets and has not been read). A brick
  needs a batch of 20, so on a station that mixes 10 at a time its 20 units come out as two loose batches of
  10. Jars (5) and baggies (1) always fit.
- **The packaging comes from the last package put in.** The station remembers it until loose product is put
  in the product slot. Loose units topped up onto an unpacked stack can end up packed too.
- **The packaging survives a reload, from a file of the mod's own.** Whenever the game saves, the mod writes
  `UserData/MixPackagedDrugs/<save>-<hash>.txt` (one line per station: its GUID and the packaging's item ID).
  It cannot go in the save folder, because the game deletes files it did not write from there. So the file is
  not in the game's save exports, backup zips or cloud sync. A save restored from a backup, or copied to another
  folder or PC, finds no matching file (or a newer one) and its running mix comes out loose or in the newer
  packaging. Nothing is lost.
- **Whole packages only.** A package that does not fully fit the slot's room is refused, never split. Dragging
  a stack of packages onto the slot with the mouse is cut down to what fits and the rest stays where it was.
- **An empty product slot.** A package cannot be added to a slot that already holds unpackaged product (the
  game does not stack the two), so empty the slot first. Such a drag is cancelled.
- **Brick press and packaging station are unchanged.** Only the mixing stations' product slot is opened.
- **Saves one small file of its own** (above), only when a station has a remembered packaging. Packages are
  unpacked at once, so no packaged item is ever saved in a station. Removing the mod leaves the file unused.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `MixPackagedDrugs.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Not tested. The host does the unpacking and the game's own station networking carries the result, so clients
do not need the mod for that. A player who wants to *drop a package in* needs the mod, because their own
game still refuses it without it.

## Status

Both builds compile and pass the unit and canary tests. Not run in game.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `ItemSlot.DoesItemMatchHardFilters` | Says yes to a packaged product, for a mixing station's product slot only |
| `ItemSlot.GetCapacityForItem` | Counts the room for a packaged product in whole packages |
| `ItemSlot.SetStoredItem` | On the host, swaps a packaged stack in that slot for the same product unpackaged (quantity times units) and remembers the packaging; loose product put in the slot makes the station forget it |
| `ItemSlot.AddItem` | On the host, for a mixing station's output slot: packs a finished mix in the remembered packaging when it fills whole packages |
| `SaveManager.Save` | On the host, writes the remembered packagings to the mod's file for this save |
| `MixingStation.Destroy` | Forgets the removed station's packaging |
| `ItemUIManager.EndDrag` | Cuts a drag of packages onto the product slot to the whole packages that fit, or cancels it |

## For developers

```bash
make test MOD=mix-packaged-drugs
make build MOD=mix-packaged-drugs
```

See [PROPOSAL.md](PROPOSAL.md) for the investigation behind the design.
