# Brick Mixer: proposal

Status: **built** (compiles, tests pass), **not run in game**. Follows from the Mix Packaged Drugs investigation. Its product slot used to take bricks only, with its own unpack; that
is now left to Mix Packaged Drugs (optional), so the slot takes loose product, and packages when that mod is there.

## Idea

A separate buildable mixing station that takes only bricks, needs the usual one mixer per unit (20 for a brick)
and gives a brick back. It avoids the batch-size problem of the packaged-drugs mod: the copy's batch is 20, so a
brick is always a whole batch.

## Why a copy of the Mk2, not a new class

A new `MixingStation` subclass would need an IL2CPP-injected type and FishNet-generated RPC code, which cannot be
produced at runtime. The Mk2 component already does everything a mixer does, and the variant's few rules depend
only on the station's item ID, so they are plain Harmony patches.

## What the investigation found

- `Registry.AddToRegistry(ItemDefinition)` adds items at runtime; `RemoveRuntimeItems` removes them on every scene
  change. So the item is re-added in a prefix on `LoadManager.StartGame`, before saved stations are built.
- A station is built by `Instantiate(BuildableItemDefinition.BuiltItem.gameObject)` then
  `NetworkObject.Spawn` (`BuildManager`). The copy of the prefab is made under an inactive parent so its `Awake`
  does not run; what the game instantiates from it is active. It keeps the Mk2's network prefab ID, so spawning
  should be accepted; clients build their copy from the plain Mk2 prefab.
- `MaxMixQuantity`, `MixTimePerItem` and `RequiresIngredientInsertion` are public fields, set on the copy only:
  a batch of 20 and no hands-on step (whose ingredient spots would overflow at 20 units).
- Shops are scene objects with a `Listings` list read in `ShopInterface.Awake`; a prefix adds a listing to any
  shop that already lists the Mk2. Which shop sells it is not in the code.
- Stacks: a product stack is 20 units, so the slot holds one brick's worth. Package intake (unpacking, room in
  whole packages, the drag clamp) is Mix Packaged Drugs', which acts on every mixing station.
- Saves: the game saves a station with its item ID and rebuilds it through `Registry`. No data of the mod's own
  is needed, because the packaging is always a brick. The cost is that uninstalling the mod loses the stations.

## Appearance

The copy gets its own materials: each opaque mesh material (render queue below 3000) is copied and its color
(`_BaseColor` or `_Color`) multiplied by the tint, blended by `TintStrength`. Which shader the game uses and
whether those names exist is not in the code; a verbose log line says how many materials were tinted, and a
warning appears when none were. The shop listing uses the game's own `UseIconTint` / `IconTint`.

## Rules per station (item ID `brickmixer`)

| Hook | Rule |
|---|---|
| `GetMixQuantity`, `CanStartMix` | 20 or nothing |
| `AddItem` on the output slot (host) | Pack a batch of 20 as a brick; the brick is the registry packaging holding 20 units |

## Open questions (all need the game)

- Does the copied prefab spawn and behave as a Mk2 (visuals, animation, sound, config panel)?
- Which shop sells the Mk2, and does the listing appear with the right category, price and rank?
- Is the item registered early enough when a save loads, and does a station survive save and reload?
- Is the product stack limit really 20, and is the mixer stack limit at least 20?
- Multiplayer: the host spawns the copy, clients build a plain Mk2.

## Possible later work

A tinted inventory icon (the sprite's texture is not readable, so it would need a render-texture copy); a different mix speed; a shop other than the Mk2's; requiring the mod on every player so clients build the copy too.
