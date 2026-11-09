# Brick Mixer

A mod for **Schedule I** that adds a new buildable, the **Brick Mixer**: a mixing station that takes 20 units of
product (a brick, or jars and baggies) and 20 mixers, mixes them in one go, and gives the result back as a
**brick**.

It is a copy of the Mixing Station Mk2, sold in the same shop. The mod saves no files of its own.

## How it works in the game

- **Buy it** where the Mixing Station Mk2 is sold. It is listed next to the Mk2.
- **Product slot:** takes loose product, and with [Mix Packaged Drugs](../mix-packaged-drugs/README.md)
  installed also bricks, jars and baggies, which are unpacked as they go in (a brick becomes 20 units). Loose
  product and packages stack together up to 20 units.
- **Mixer slot:** as on any station. One mixer per unit, so a brick needs **20 mixers**.
- **Starting:** the start button works only with 20 units of product and 20 mixers. There is no hands-on step.
- **Output:** the finished batch is packed as a brick, whatever went in, so you collect a brick of the mixed
  product.
- **Chemists** can run it like a Mk2.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[BrickMixer]`.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `PriceMultiplier` | `1.5` | The Brick Mixer costs this many times the Mk2's price. Above 0 and at most 1000; anything else is logged and counts as 1.5. Applies after a restart of the game |
| `TintColor` | `#E02626` | The color the model is tinted, as `#RRGGBB`. Red by default. A bad value is logged and counts as the default. Applies after a restart of the game |
| `TintStrength` | `1` | From 0 (the Mk2's own colors) to 1 (fully tinted). Anything else is logged and counts as 1. Applies after a restart of the game |
| `VerboseLogging` | `false` | Logs each step of adding the item and each brick it packs |

## Things to know

- **Uninstalling removes your Brick Mixers.** The game saves a station by its item, and without the mod the
  game does not know the item, so those stations (and what is inside them) are gone from the save the next time
  it loads. Pick them up before removing the mod.
- **It is red.** The model's opaque materials are copied and tinted (glass and glows are left alone, and the
  Mk2's own materials are not changed), and the shop tints its icon. The inventory icon and the hologram you
  place are the Mk2's. The instruction text on the screen still says "unpackaged product".
- **20 units at a time.** The product slot holds 20 units, which is one brick. A package that does not fully fit
  the room is refused, never split: a brick needs an empty slot, a jar needs room for 5.
- **Packages need Mix Packaged Drugs.** Without it the Brick Mixer takes loose product only (it still gives a
  brick back). The intake itself is that mod's, including its `Enabled` setting; this mod has no switch of its
  own, because turning it off would leave Brick Mixers already in a save without their item.
- **Packaging is used up** when a package is unpacked, and the output comes back packed in a new brick.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `BrickMixer.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Not supported yet; the plan is for every player to need the mod. For now the host builds the station from the copied Mk2 and other players receive a plain Mk2 for
it, so they would see Mk2 rules where the Brick Mixer's apply. Every player would also need the mod for the item.

## Status

Both builds compile and pass the unit and canary tests. **Not run in game**, and adding a new item to the game
this way has no precedent in this repository, so expect to find problems on first run. The riskiest parts, in
order: the copied prefab spawning as a station, the item appearing in the shop, and the item being known when a
save loads.

## How it works

| Hook | What the mod does |
| ---- | ----------------- |
| `LoadManager.StartGame` | Copies the Mk2's item and prefab (batch of 20, no hands-on step) and adds the item to the game's registry. The game empties its registry of runtime items on each scene change, so this runs at every game start |
| `ShopInterface.Awake` | Adds a listing for the Brick Mixer, with a tinted icon, to any shop that sells the Mk2 |
| `ItemSlot.AddItem` | On the host, packs a finished batch in the Brick Mixer's output slot as a brick |
| `MixingStation.GetMixQuantity`, `CanStartMix` | A Brick Mixer mixes 20 units or nothing |

The station is told apart from every other by its item ID (`brickmixer`). What goes into its product slot is not
this mod's: loose product is the game's own rule, and packages come from
[Mix Packaged Drugs](../mix-packaged-drugs/README.md), which treats every mixing station alike.

## For developers

```bash
make test MOD=brick-mixer
make build MOD=brick-mixer
```

See [PROPOSAL.md](PROPOSAL.md) for the investigation behind the design.
