# C# style

Sample guide: the shape the other guides should follow (short rules, a reason, a good and a bad example).
Applies to everything under `mods/*/src/common/` and `lib/shared/`.

> Existing mods predate this guide: they use Allman braces and one-line `if (...) return;`. New code follows
> this guide. Reformat a file only when you are already changing it, and never in the same commit as a
> behavior change.

## Braces

Opening brace on the same line (K&R). `else` goes on the closing brace's line. **Every** `if`, `else`, `for`,
`foreach`, `while` and `using` body gets braces, even a single statement. This covers guard clauses too.

```csharp
// good
if (instance == null) {
    return;
}

if (ModConfig.Enabled) {
    Apply(instance);
} else {
    Restore(instance);
}

// bad: no braces, or Allman
if (instance == null) return;
if (ModConfig.Enabled)
{
    Apply(instance);
}
else
{
    Restore(instance);
}
```

**Why:** a braceless body is the classic place for a later edit to silently fall outside the `if`, and
patches that must fail open are exactly where that bug hides.

Exceptions: expression-bodied members (`=> ...`) for a one-line getter or method, and `switch` expression arms.

Types and methods use the same rule: `class Foo {` and `void Bar() {` open on the declaration line.
`namespace X { ... }` blocks stay braced; do not switch to file-scoped namespaces (the files already don't).

## Layout

- 4 spaces, no tabs. LF line endings. A final newline.
- One type per file; the file is named after the type (`DepositRules.cs`). Patch files group related patch
  classes and are named `<Area>Patches.cs`.
- Soft limit of 120 columns. Break long argument lists one argument per line, aligned.
- Usings: `System` first, then other namespaces alphabetically, outside the namespace. Delete unused ones.
  Namespaces that differ between runtimes belong only in `GlobalUsings.cs`.

## Naming

| Thing | Style | Example |
|---|---|---|
| Namespace, type, method, property, public const | `PascalCase` | `DepositRules.GameLimit` |
| Private or internal field | `_camelCase` | `_lastSummary` |
| Local, parameter | `camelCase` | `weeklySum` |
| Harmony patch class | `Target_Method_Patch` | `ATMInterface_Start_Patch` |
| Pure decision class | `<Topic>Rules` | `DepositRules` |
| Mod folder | `kebab-case`; assembly and namespace `PascalCase` | `configurable-atm` / `ConfigurableAtm` |

Harmony parameters keep Harmony's names (`__instance`, `__result`, `__state`).

## Language features

- Nullable and implicit usings stay **off**; `LangVersion` is 11. Don't write `?` annotations on reference types.
- `internal` by default. `public` only for the `MelonMod` subclass, which MelonLoader must see.
- `static` classes for anything without state: `Rules`, `GameAccess`, patches.
- `var` only when the type is on the right-hand side (`new List<int>()`) or the game type is long and obvious.
  Write the type for return values of game calls, so a reader sees what the game gives back.
- Prefer `=>` for one-expression members; otherwise a block.
- No `async`, no LINQ in a patch that can run per frame (it allocates).

## Errors

- A patch body is wrapped in `try`/`catch`, logs through `ModLog.Warning`, and lets the game continue
  (see the patch guide). Catch `Exception`, never swallow silently, never rethrow from a patch.
- Don't use exceptions for expected outcomes. Parsing and validation return `bool` through `TryX(out ...)`
  or a fallback value, and live in `Rules/`.

## Comments

- Explain **why**, in full sentences: the game's behavior, the runtime difference, the thing that would
  surprise a reader. Don't restate the code.
- `<summary>` on every type and every non-obvious member. No `<param>` or `<returns>` unless the meaning
  isn't clear from the name.
- Runtime split: `#if IL2CPP` only in `GlobalUsings.cs` and `GameAccess.cs`, with a one-line comment saying
  why the flavors differ.
- No commented-out code, no `TODO` without a reason or an owner.

## Checklist before committing

- Braces on every control-flow body, K&R placement.
- No unused usings; no `#if IL2CPP` outside the two allowed files.
- New names follow the table above.
- `scripts/build.py -m <mod>` has no warnings and `scripts/test.py -m <mod>` passes.
