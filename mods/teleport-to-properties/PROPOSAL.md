# Teleport to Properties: proposal

Status: **built**. The hotkeys and the phone icon are implemented and run on a Steam Deck (IL2CPP, single
player). The sections below are the original plan; "As built" says where the result differs.

## As built

- **Phone app:** not a hand-built app panel. A blue **TP** icon is cloned from a vanilla home-screen icon and
  joins the home screen's controller navigation. Pressing it opens the game's own list popup
  (`UIScreenManager.OpenPopupScreen("ContextMenu", ...)`, the one its settings dropdowns use), which already
  handles stick, A, B and mouse. This replaced the planned scroll list and the explicit `Selectable` navigation.
- **Icon:** the text "TP", no sprite.
- **Created on demand:** a postfix on `Phone.SetIsOpen(true)` adds the icon; no scene search or polling.
- **Hotkeys:** keyboard defaults only (`[`, `]`, `\`); the Deck button is mapped in Steam Input. They are
  ignored while the phone is open.
- **Landing:** feet aligned to the spawn point plus 0.3 m, because spawn points sit at ground level.
- **Still open:** multiplayer is untested, and the Mono build has not been run in game.

## Summary

A quality-of-life ("cheat") mod that adds a **Teleport** app to the in-game phone. The app lists every
property and business the player owns; picking one teleports the local player to that property's spawn
point. No cost, no cooldown. It must be usable on a **Steam Deck / gamepad**, and work in **multiplayer**
without extra networking.

## Goals

- Owned properties **and** businesses, listed live (a property bought later appears with no restart).
- Destination is the property's `SpawnPoint` (outside). `InteriorSpawnPoint` is an optional setting.
- Fully usable with a gamepad: open the phone, move the selection, press confirm.
- Works for every player in a multiplayer session; each player teleports only themselves.
- No cost, no cooldown, no saved data.

## Non-goals

- Map-click teleporting (FaleTeleport's approach). Cool, but out of scope.
- Custom/saved locations, dead drops, stores, dealers, theming, statistics (Teleport Menu's extras).
- Teleporting other players, or any server-side/host-only behavior.

## What the examples taught us

| | Teleport Menu | FaleTeleport |
| - | - | - |
| Destinations | ~70 hardcoded Vector3s | any map point (calibrated from POI markers) |
| Move call | sets `Player.Local.transform.position` | `PlayerMovement.Teleport(pos, false)` |
| UI | IMGUI, disables Keyboard/Mouse devices to capture input | none (double-click on phone map) |
| Take | do not copy coordinates, UI or input capture | copy the `PlayerMovement.Teleport` call and the vehicle guard |

## Game facts (checked against `decompiled/mono`)

- `Property.OwnedProperties` / `UnownedProperties` / `Properties` are static lists; `Business` has the
  same three. `Property.IsOwned` is a public getter. Ownership flips through `SetOwned_Server`, so lists
  update live.
- `Property` exposes `PropertyName`, `PropertyCode`, `SpawnPoint`, `InteriorSpawnPoint`, `PoI`.
- `PlayerMovement.Teleport(Vector3, bool alignFeetToPosition)` disables and re-enables the
  `CharacterController`, dismounts a skateboard, rejects NaN. Overload takes a `Quaternion`.
  `Player.Local.IsInVehicle` is available for the guard.
- `App<T>` is the phone app base class (`AppName`, `IconLabel`, `AppIcon`, `appContainer`, a `UIScreen`,
  `GenerateHomeScreenIcon`). The existing `ModManagerAndPhoneApp` adds an app by instantiating an
  existing app's container and home-screen icon under `AppsCanvas` and `HomeScreen/AppIcons`, then
  toggling `AppsCanvas`/`HomeScreen` open state.
- `Phone.Update` already special-cases gamepad (`GameInput.GetCurrentInputDeviceIsGamepad()`), so the
  phone is meant to be gamepad-driven.

## Design

### Layers (mirrors `warehouse-access`)

```
mods/teleport-to-properties/
  src/common/
    Config/   ModConfig, ModLog      (MelonPreferences: spawn choice, hotkeys)
    Game/     Destinations (reads Property lists), Teleporter (calls PlayerMovement.Teleport)
    Phone/    TeleportApp (builds the phone app, list rows, gamepad selection)
    Patches/  none expected
    Rules/    game-free logic: ordering, de-duplication, cycle next/previous, "can teleport now"
  src/il2cpp/  src/mono/  tests/
```

### Destination list

- Enumerate `Property.OwnedProperties`. Confirm whether businesses are included; if not, append
  `Business.OwnedBusinesses`, de-duplicated by `PropertyCode`.
- Skip entries whose spawn point is null. Order: properties then businesses, then by name.
- Rebuild the list each time the app opens (cheap), not on a timer.

### Teleporting

- `PlayerMovement.Teleport(spawn.position, spawn.rotation, alignFeetToPosition: false)` on the local
  player only. Refuse when `Player.Local.IsInVehicle` or the spawn is null; log the reason.
- Close the phone after a teleport.

### Phone app

- Clone an existing app's container and icon (as the Mod Manager does), label it "Teleport".
- One scrollable list of buttons, one per destination.
- **Gamepad plan:** set explicit `Selectable` navigation (vertical) on the rows, and call
  `EventSystem.SetSelectedGameObject` on the first row when the app opens and after list rebuilds, so
  the d-pad/stick and confirm work. Keep the list a single column to keep navigation predictable.
- Exit through the app's existing `GameInput.RegisterExitListener` path so B/back closes it.

### Fallback: hotkey cycling (build first, keep forever)

Two bindings (previous/next destination) plus a teleport binding, with a short toast of the current
name. Keyboard defaults only; the Deck button is mapped by the user in Steam Input. This is the safety net if phone gamepad
navigation proves unreliable, and costs little because it shares the same `Destinations` and
`Teleporter` code.

### Multiplayer

- Each client calls `PlayerMovement.Teleport` on its own local player; nothing is sent over the network.
- Property ownership is replicated by the game, so every player sees the same list.
- Open to verify: a non-host sees `IsOwned` correctly, and a remote player's new position is shown to
  others without extra work.

## Config (MelonPreferences)

| Key | Default | Purpose |
| - | - | - |
| Spawn | outside | `SpawnPoint` or `InteriorSpawnPoint` |
| EnableHotkeys | true | turns the hotkey fallback on/off |
| Next / Previous / Teleport | keyboard keys, TBD | hotkey bindings (no gamepad defaults; Deck handled via Steam Input) |

## Risks and unknowns

1. **Phone controller navigation** (largest). The user reports the phone "doesn't play super nice" with
   a controller. We do not yet know whether the cause is the vanilla phone or its Selectable setup.
   Mitigation: explicit navigation plus forced selection, and the hotkey fallback.
2. **Cloning an app is fragile.** Hierarchy paths (`.../Phone/phone/AppsCanvas`, `HomeScreen/AppIcons`)
   come from the Mod Manager and can break on game updates. Mitigation: canary tests for the types,
   runtime null checks that log and disable the app instead of throwing.
3. **Mono vs IL2CPP.** The same source must compile for both; phone UI code is where the two flavors
   differ most (Il2Cpp list types, delegates).
4. **Multiplayer** behavior cannot be tested from this machine; needs a two-player session.
5. **Icon.** The app shows "TP" text instead of a sprite, so the cloned icon's image must be hidden or
   replaced by a text label.

## Milestones

1. Skeleton project (both flavors), `Destinations` + `Teleporter`, hotkey cycling. Verify in-game solo.
2. Canary tests for `Property.OwnedProperties`, `Property.SpawnPoint`, `PlayerMovement.Teleport`,
   and the phone types used; logic tests for `Rules`.
3. Phone app with mouse/keyboard.
4. Gamepad navigation pass on the Deck.
5. Multiplayer check with a second player; README.

## Open questions

None. Resolved:

- Spawn: **exterior** (`SpawnPoint`) by default; interior stays an optional setting.

- App icon: no sprite; the app shows the text **"TP"**.
