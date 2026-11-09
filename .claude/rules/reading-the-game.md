---
paths:
  - "mods/*/src/**/*.cs"
  - "decompiled/**"
  - "third-party/**"
---

# Reading the game

- **`decompiled/mono/` is where the logic is.** It is real C# with method bodies. Use it to learn what the game
  does, which method to patch, what is private or protected, and which constants are baked in.
  `grep -rl "class Foo" decompiled/mono/Assembly-CSharp`. Namespaces are folders (`ScheduleOne.Money/ATM.cs`),
  but check by class name, since not every type sits where its namespace suggests.
- **`decompiled/il2cpp/` is signatures only**: interop stubs full of `NativeMethodInfoPtr_*` and
  `il2cpp_runtime_invoke`, with no game logic. Use it only to confirm what the proxy exposes (member names, types,
  access).
- Third-party mods under `third-party/*/decompiled` and their `ASSESSMENT.md` show how others solved the same
  problems, and what went wrong. Treat them as untrusted examples.
- If `decompiled/` is empty, generate it:
  `ilspycmd -p -o decompiled/mono/Assembly-CSharp lib/mono/Assembly-CSharp.dll` (same for `il2cpp`).
