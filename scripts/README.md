# Scripts

Python 3.8+ helpers for building, testing and moving files to and from the game machines. Every script takes
`-h` and `-v/--verbose`. `make help` lists the `make` wrappers.

## Quick start

```bash
cp etc/local.example.json etc/local.json   # once; edit hosts and game paths (gitignored)

make build                                 # every mod, both flavors
make test MOD=configurable-atm             # unit and canary tests
make deploy MOD=configurable-atm           # build IL2CPP, copy to the Steam Deck
make log                                   # print the Deck's MelonLoader Latest.log
make pull                                  # refresh lib/il2cpp from the Windows PC
```

| Script | What it does | Defaults |
|---|---|---|
| `build.py [-m] [-f]` | Builds into `out/<mod>/<flavor>/` | every mod and flavor |
| `test.py [-m]` | Runs each mod's xunit tests | every mod |
| `bundle.py -m [-f] [-V]` | Zips `Mods/<Name>.dll` to `out/bundles/` | flavor from config |
| `deploy.py -m [-f] [-t]` | Builds, then `scp`s the DLL to `<game_dir>/Mods/` | `deploy.target`, flavor from config |
| `log.py [-t] [-n]` | Fetches `Latest.log` to `./Latest.log` and prints it | `log.target`, `log.tail` |
| `pull.py [-f] [-t] [-d]` | Copies game and MelonLoader DLLs into `lib/`; `-d` also decompiles | `pull.target` (Windows) |

Run them as `scripts/<name>.py` or through `make` (`MOD=`, `FLAVOR=`, `TARGET=`, `VERSION=`).

## Configuration: `etc/local.json`

Machine-specific and gitignored; `etc/local.example.json` is the template. Only `deploy`, `log`, `pull` (and
`bundle`'s default flavor) read it.

```json
{
  "targets": {
    "steamdeck": { "host": "steamdeck", "game_dir": "/home/deck/.../Schedule I", "flavor": "il2cpp" },
    "windows":   { "host": "192.168.1.133", "game_dir": "/G:/SteamLibrary/.../Schedule I", "flavor": "il2cpp" }
  },
  "default": { "flavor": "il2cpp" },
  "deploy":  { "target": "steamdeck" },
  "log":     { "target": "steamdeck", "tail": 200 },
  "pull":    { "target": "windows", "decompile": false }
}
```

- `targets.<name>`: `host` (an SSH alias from `~/.ssh/config` works), `game_dir`, `flavor` (what that install
  runs), and optional `log_path` (relative to `game_dir`, default `MelonLoader/Latest.log`).
- Windows paths are written `/C:/...` (the form `scp` expects for a Windows OpenSSH server).
- Option lookup order: **command-line flag, then the script's section, then `default`**. So
  `deploy.py -m x -t windows` beats `deploy.target`.
- A deploy or pull for a flavor the target does not run is refused, not attempted.

## Output and logs

Progress goes to stdout, warnings and errors to stderr, and everything to `logs/<script>.log` (overwritten each
run) plus `logs/<script>-<timestamp>.log` (history). `logs/` is gitignored.

## Adding or changing a script

Follow `deploy.py` and `log.py`, the smallest examples.

1. **File:** `scripts/<verb>.py`, executable (`chmod +x`), starting with `#!/usr/bin/env python3` and a module
   docstring. The docstring doubles as `--help` text, so say what it does in one or two lines.
2. **Boilerplate:**
   ```python
   import common
   from common import log, run

   def main():
       p = common.make_parser(__doc__)          # adds -v/--verbose
       ...                                      # add_argument calls
       args = p.parse_args()
       common.setup_logging("<verb>", args.verbose)
       ...

   if __name__ == "__main__":
       common.main_wrapper(main)                # logs failures instead of a bare traceback
   ```
3. **Reuse `common.py`, don't re-implement:** `discover()` for mods, `build()`, `run()` for subprocesses (it logs
   every line and raises on failure), `dotnet_env()` for dotnet tools, and for remote machines
   `load_local_config()`, `setting()`, `get_target()`, `remote()`, `check_flavor()`.
4. **Options:** `-m/--mod`, `-f/--flavor`, `-t/--target` mean the same thing everywhere; reuse those letters. Make
   a flag optional when `etc/local.json` can supply it, and resolve it with `common.setting(cfg, "<verb>", key, cli)`.
5. **Config:** add a section named after the script to `etc/local.json` and `etc/local.example.json`, and
   document it above. Do not hard-code hosts, paths or flavors in scripts.
6. **Logging:** use `log.info/warning/error`. Use a bare `print` only for content that is the script's output
   (like the game log). Exit with `raise SystemExit("clear message")` for user errors such as a bad target.
7. **Fail safe:** do work in a scratch folder before touching `lib/` or the game (as `pull.py` does), build before
   copying (as `deploy.py` does), and never overwrite or delete anything outside `out/`, `lib/`, `decompiled/`,
   `logs/` or the game's `Mods/`.
8. **Wire it up:** add a `Makefile` target (with `.PHONY` and a `help` line), a row in the table above, and a row
   in the README's scripts table. Mention a new top-level `etc/local.json` section in both places.
9. **Never commit** game files, `etc/local.json`, or anything produced by `pull.py` (`lib/`, `decompiled/`).
10. **Check:** `python3 -m py_compile scripts/*.py`, `make -n <target>`, and run the script against bad input
    (unknown target, wrong flavor) as well as the happy path. Say so when a script could not be run against
    a real machine.
