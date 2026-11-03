# Teleport App

A quality-of-life ("cheat") mod for **Schedule I**. Teleport to a property or business you own, or to a
dealer you have hired, a store, or a vehicle you own, from a **TP** app on the phone or with three keys. It is free and has no cooldown.

## Using it

| Key (default) | What it does |
| ------------- | ------------ |
| `[` | Pick the previous place |
| `]` | Pick the next place |
| `\` | Teleport to the picked place |

**On the phone:** open the phone and press the blue **TP** icon. A list opens with the kinds of place you
have, with counts: **Properties**, **Businesses**, **Dealers**, **Stores** and **Vehicles** (a kind with nothing in it is not shown).
Choose one, then choose the place, and you teleport and the phone closes. Choose with the stick and A, or the
mouse; B closes the list. The lists are the popup the game's settings dropdowns use, so they work with a controller.

**Choosing what the keys step through:** by default the keys cycle through everything the phone lists. To
narrow that, open TP and choose **Hotkey rotation...** at the bottom of the first list. Pick a kind to see
its places with `[x]` (in the rotation) or `[  ]` (left out); choosing an entry flips it and the list opens
again, so you can change several, and B ends. The first entry of each list switches the whole category. This
only affects the keys: the phone lists keep showing everything. The choices are saved.

A line of text near the bottom of the screen shows the pick ("Teleport to: Bungalow (2/5)") and the result.
The keys step through all places in one list: properties, businesses, dealers, stores, then vehicles, each by name. The
lists follow what you own and who you have hired right now, so a new place appears without restarting.

**Stores:** the ten stores (Dan's Hardware, Handy Hank's, both Gas-Marts, Pawn Shop, Shred Shack, Ray's Real
Estate, Blueball's Boutique, Thrifty Threads, Top Tattoo) and the Black Market (the Dark Market warehouse; you
land outside its door, which this does not open) have a fixed spot in front of them. The game has no
list of store exteriors, so the mod carries the spots as numbers and snaps each to the ground when you
teleport; you keep the way you were facing. If the game's map changes they may need updating.

**Vehicles:** the vehicles you own, named by colour and model (for example "Red Shitbox"). You land beside
the vehicle, a metre and a half past its side, facing it. The list reads the game's list of all vehicles and
keeps the ones you own, so it works for guests as well as the host.

**Dealers:** only dealers you have hired are listed. A dealer moves, so you land about 1.5 m in front of
where they are at that moment, facing them. A dealer who is indoors stands just outside the building's
door, so that is where you land. Properties and businesses use their spawn point (see the setting below).

**Steam Deck:** map a controller button to each key in Steam Input (keyboard output). The mod itself
only listens for keyboard keys.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[TeleportApp]`.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `ShowProperties` | `true` | `false` removes the Properties category from the phone list and the keys |
| `OnlyOwnedProperties` | `true` | `true` lists only properties you own; `false` lists them all, marking the ones you do not own |
| `ShowBusinesses` | `true` | `false` removes the Businesses category |
| `OnlyOwnedBusinesses` | `true` | `true` lists only businesses you own; `false` lists them all, marking the ones you do not own |
| `ShowDealers` | `true` | `false` removes the Dealers category |
| `OnlyHiredDealers` | `true` | `true` lists only dealers you have hired; `false` lists every dealer, marking the ones not hired |
| `ShowStores` | `true` | `false` removes the Stores category (stores have no owned state) |
| `ShowVehicles` | `true` | `false` removes the Vehicles category (only vehicles you own are ever listed) |
| `RotationKinds` | blank | Kinds the keys step through, such as `Property|Dealer`. Blank is every kind, `None` is none. Set on the phone |
| `RotationExcluded` | blank | Codes of places the keys skip, separated by `\|`. Set on the phone |
| `UseInteriorSpawn` | `false` | `false` teleports to the outside spawn point; `true` to the inside one. If a place has only one, that one is used |
| `EnableHotkeys` | `true` | Turns the three keys on or off |
| `PreviousKey` | `LeftBracket` | A Unity `KeyCode` name, such as `LeftBracket` or `F6` (case and spaces do not matter) |
| `NextKey` | `RightBracket` | As above |
| `TeleportKey` | `Backslash` | As above |
| `VerboseLogging` | `false` | Logs each pick and teleport |

A bad key name is logged and the default key is used. Changes made in the preferences file while the
game runs are picked up.

## Things to know

- The keys do nothing while you type, while the game is paused, while the phone is open, or while you are in a vehicle.
- The mod patches nothing and saves nothing. It calls the game's own teleport on your own player.
- It only teleports you, never other players.
- **Per-frame work:** the mod tests three keys each frame and draws its on-screen line through IMGUI. A key
  press raises no game event to hook, so polling is the only way; nothing else runs per frame.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `TeleportApp.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Not tested. Each player teleports only themselves, so install it on every player who wants it. The
game shares which properties are owned, so each player should see the same list.

## Status

The hotkeys and the phone icon. The icon is added the first time the phone is opened in a save; if it cannot
be added the log says so and the hotkeys still work. See [PROPOSAL.md](PROPOSAL.md) for the plan.

Run in game on a Steam Deck (IL2CPP) in single player, with a controller: the hotkeys, the phone icon and its
lists, and the five kinds (properties, businesses, dealers, stores, vehicles) work. The Pawn Shop landing was
fixed after it put the player on the roof; other stores have not each been checked. The hotkey rotation editor
was not reported on separately. The Mono build is not run in game, and multiplayer is not tested.
