# Ground rules

- All mod code lives in `src/common/` and compiles into both flavors. Code every mod needs (logging, the patch
  loader, number checks) lives in `lib/shared/` and is compiled into each mod, so don't copy it into a mod. Write for IL2CPP first, then make Mono work.
- Never commit game or MelonLoader files. `lib/` (except `lib/shared/`, our own code), `decompiled/`, `third-party/`, `out/`, `logs/` are gitignored.
- Defaults should make the mod do what its name says. Ship the change on by default when that is the point of the
  mod (e.g. skip-disclaimer); use neutral defaults only when the mod has no sensible one (e.g. tuning multipliers).
- Patches fail open. A broken patch must never break the game's own code path.
- Be event-driven, not polling. Never use `OnUpdate`/`OnLateUpdate`/`OnFixedUpdate`, timers, coroutines that
  loop, or "check every N seconds" logic to notice state changes: they run every frame and are wasteful. Hook the
  game's own events instead (a Harmony patch on the method that changes the state, a game event or `UnityEvent`
  subscription, `OnPreferencesLoaded`/`OnPreferencesSaved`, scene-load callbacks). If no event exists, patch the
  setter or the method that causes the change. Only fall back to per-frame work when nothing can be hooked, and
  say why in a comment and in the README.
- Don't claim a mod works in game unless it was run. Builds and tests prove only that it compiles and that the
  patched members exist. Say "not run in game" in the README's Status section.
