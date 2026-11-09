---
paths:
  - "mods/*/src/**"
  - "lib/shared/**"
---

# Mono vs IL2CPP

Keep every difference inside `GlobalUsings.cs` and `Game/GameAccess.cs` (plus a flavor-only file when a feature
can't be shared). Patch classes should be identical on both flavors.

| Concern | Mono | IL2CPP |
|---|---|---|
| Namespaces | `ScheduleOne.*`, `FishNet.*` | `Il2CppScheduleOne.*`, `Il2CppFishNet.*`, `Il2Cppmscorlib` |
| Private or protected members | `AccessTools.FieldRefAccess<T,F>("name")`, `MethodInfo.Invoke` | Proxy exposes them as public: use directly |
| Type tests | `is`, `as` | Wrapper classes: `((Il2CppObjectBase)o).TryCast<T>()`; use the shared `As<T>` |
| Identity of a game object | `RuntimeHelpers.GetHashCode` | `.Pointer.ToInt64()`; use the shared `Same` and `IdOf` |
| Arrays and lists | `T[]`, `System.Collections.Generic.List<T>` | `Il2CppReferenceArray<T>`, `Il2CppSystem.Collections.Generic.List<T>` |
| Compile-time constants baked into code | Transpiler on the real IL | IL is gone: see below |
| Target | netstandard2.1 | net6.0 + `DefineConstants IL2CPP` |

Pattern for a split in `GameAccess`:

```csharp
#if !IL2CPP
    // Protected field on Mono; the IL2CPP proxy exposes it as a public property.
    private static readonly AccessTools.FieldRef<Skateboard, SkateboardSettings> SettingsRef =
        AccessTools.FieldRefAccess<Skateboard, SkateboardSettings>("_settings");
#endif
public static SkateboardSettings Settings(Skateboard b)
{
#if IL2CPP
    return b._settings;
#else
    return SettingsRef(b);
#endif
}
```

## IL2CPP hazards

- **Inlining:** the native compiler may inline small methods, so a patch on one can silently never fire. Patch
  the callers too, or patch a method that is certainly called (configurable-atm wraps all callers of the limit).
- **`const` values are compiled into every user** on both runtimes. Mono: a transpiler replaces the literal
  (`ldc.r4`) and throws if the expected count differs, so a game update fails loudly. IL2CPP: no IL to rewrite,
  so change the inputs around the call instead (configurable-atm shifts `ATM.WeeklyDepositSum` in a prefix and
  restores it in a Finalizer). See `MonoLimitPatches.cs` and `Il2CppLimitPatches.cs`.
- **Identical methods may be merged** (code folding). `configurable-employees` logs this for task durations.
- Don't hard-code native field offsets (AbsorbentSoil did, and it breaks on any game update).
- IL2CPP proxy objects are wrappers: don't use `==` or `is` to compare identity or type across base types.
- Flavor-only files are wrapped whole: `#if IL2CPP ... #endif` or `#if !IL2CPP ... #endif`.
