# Teleport to Properties

A quality-of-life ("cheat") mod for **Schedule I**. Pick one of the properties or businesses you own
with two keys and teleport there with a third. It is free and has no cooldown.

## Using it

| Key (default) | What it does |
| ------------- | ------------ |
| `[` | Pick the previous place |
| `]` | Pick the next place |
| `\` | Teleport to the picked place |

**On the phone:** open the phone and press the blue **TP** icon. The game's own list popup opens with your
places (businesses are marked); choose one with the stick and A, or the mouse, and you teleport and the phone
closes. B closes the list. The popup is the one the game's settings dropdowns use, so it works with a controller.

A line of text near the bottom of the screen shows the pick ("Teleport to: Bungalow (2/5)") and the result.
The list is your owned properties first, then your owned businesses, each by name, and it follows what
you own right now: a place bought later appears without restarting.

**Steam Deck:** map a controller button to each key in Steam Input (keyboard output). The mod itself
only listens for keyboard keys.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[TeleportToProperties]`.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
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
2. Copy `TeleportToProperties.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

Not tested. Each player teleports only themselves, so install it on every player who wants it. The
game shares which properties are owned, so each player should see the same list.

## Status

The hotkeys and the phone icon. The icon is added the first time the phone is opened in a save; if it cannot
be added the log says so and the hotkeys still work. See [PROPOSAL.md](PROPOSAL.md) for the plan.

Run in game on a Steam Deck (IL2CPP) in single player: the hotkeys (picking, teleporting, landing on the ground)
and the phone icon with its list work, with a controller.
The Mono build is not run in game, and multiplayer is not tested.
