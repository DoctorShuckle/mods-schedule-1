# Configurable ATM

A mod for **Schedule I** that lets you set the ATM's weekly deposit limit, switch it off, and choose
the six amount buttons.

## Settings

Settings live in `UserData/MelonPreferences.cfg`, under `[ConfigurableAtm]`. The defaults leave the
game as it is, so the mod does nothing until you change one.

| Setting | Default | What it does |
| ------- | ------- | ------------ |
| `WeeklyDepositLimit` | `10000` | How much cash you can deposit each week. Zero, negative or not-a-number counts as `10000`; anything above `1000000000` is capped there |
| `DepositLimitEnabled` | `true` | `false` removes the limit: the ATM takes any amount, and the "spent / limit" readout is hidden |
| `DepositAmounts` | `20, 50, 100, 500, 1000, 5000` | The six amount buttons, as six whole numbers above 0 separated by commas. Anything else is ignored and the game's buttons are used |
| `VerboseLogging` | `false` | Logs what the mod patches and changes |

Changes are picked up when preferences are saved, including from a settings app in the game, and
take effect at once; the amount buttons are relabelled straight away.

## Things to know

- **The last button.** When depositing, the sixth button is **MAX** (all you hold, up to what the limit
  still allows), so its number is used only when withdrawing.
- **The Clean Cash quest** starts when your weekly deposits reach the game's limit, which is also the
  point where the game points you to buy a business and clean your cash. The mod starts it at the
  smaller of your limit and `10000`: a lower limit does not make it unreachable, and a higher limit does
  not make you wait for it. With the limit switched off it starts at `10000`.
- **The week still resets** on the game's own schedule, and your total this week is saved by the game as
  before. Lowering the limit below what you have already deposited just means no more this week.
- **Changing the limit does not refund or undo anything.** Only new deposits are checked.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) for Schedule I.
2. Copy `ConfigurableAtm.dll` into the game's `Mods` folder.
3. Start the game.

There is a build for each branch of the game (Mono and IL2CPP). Use the one that matches your install.

## Multiplayer

The deposit total and the ATM screen are local to each player, so install the mod on your own machine
to change your own ATM. Other players do not need it, and it does not change theirs.

## Status

Both builds compile, and the unit and canary tests pass (the canary reads the game's IL and confirms
each method the Mono patches rewrite still holds the expected number of `10000` literals). **Neither
build has been run in the game yet.** The IL2CPP build is the less certain: see below.

## How it works

The game's weekly limit is a `const`, so its compiler wrote the number `10000` into every method that
uses it. Nothing reads the limit at run time, so a setting cannot change it directly.

**Mono** rewrites those methods with Harmony transpilers, swapping each `10000` for a call that returns
your limit:

| Method | Uses replaced |
| ------ | ------------- |
| `ATMInterface.remainingAllowedDeposit` | 1: what a deposit may still be |
| `ATMInterface.Update` | 3: the readout, its red colour, the menu's Deposit button |
| `ATMInterface.UpdateAvailableAmounts` | 1: which buttons are greyed out |
| `Quest_CleanCash.OnUncappedMinPass` | 1: when the quest starts (with the quest threshold) |

If a game update changes the count in any of them, that patch fails loudly in the log and leaves the
game's method alone.

**IL2CPP** has no IL to rewrite, and overriding the game's answers afterwards breaks controller
navigation (the game sets a button non-interactable, the mod sets it back, and Unity drops the
controller's selection each time). So the game is left to decide, on a total it decides correctly:
while an ATM screen method runs, the weekly total is moved down by how far your limit is above 10000,
so the game's own checks against 10000 come out as they would against your limit. The real total is put
back exactly when the method ends. Only the "spent / limit" text is rewritten. The quest gets a
postfix that starts it at a lower limit.

Both builds set the amount buttons and hide the limit readout (when the limit is off) as the ATM screen
starts. Nothing is saved, so removing the mod returns the game to normal.

## For developers

```bash
make test MOD=configurable-atm
make build MOD=configurable-atm
```

The canary tests read the game's DLLs in `lib/` and fail if a patch target has gone.
