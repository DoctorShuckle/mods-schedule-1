# Persistent Pot Additives

A mod for **Schedule I** that makes the additives you put on a pot last as long as the soil does.

## The problem

In the base game, every time you harvest a plant, the pot forgets its additives. That happens even
when the soil still has uses left. A 3-use soil means applying Speed Grow, Fertilizer and PGR
again after every single harvest.

## What this mod does

Additives stay on the pot until the soil is used up.

- Apply an additive once, and it works on every plant grown in that soil.
- When the soil runs out, the additives are cleared along with it, just as the game normally does
  when soil is spent.
- **Speed Grow** gives its instant growth boost to every new plant, not only the first one.
  Fertilizer and PGR keep their quality and yield effects on each plant as well.

Mushroom beds are not affected. Their substrate is single-use, so there is nothing to carry over.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `PersistentPotAdditives.dll` into the game's `Mods` folder.
3. Start the game.

There are two builds, one for each version of the game (the Mono branch and the main IL2CPP
branch). Use the one that matches your install.

## Multiplayer

Only the host needs the mod. Other players don't have to install it and will still see the
additives come back on the pot.

## How it works

The game already saves what is on each pot and how many uses the soil has left. This mod doesn't
keep its own records or write any files. It only steps in at two moments:

1. **After a harvest**, it puts back the additives the game just removed, as long as the soil
   still has uses left.
2. **When a new seed is planted**, it gives the plant Speed Grow's growth boost if the pot has it.

Because it saves nothing of its own, there is nothing to get out of sync or corrupt. You can add
the mod to an existing save, and removing it later just returns to the normal game behavior.

## Settings

There is one setting, **VerboseLogging** (off by default). It prints extra detail to the
MelonLoader log, which is useful when reporting a problem. You can change it in
`UserData/MelonPreferences.cfg`.

## Compatibility

Written for Schedule I version 0.4.7. A game update that changes how pots or planting work could
break it. If it stops working after an update, check the MelonLoader log for a "patch failed"
message.
