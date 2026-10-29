"""Shared helpers for the build/deploy scripts: paths, dotnet environment, logging, subprocess."""
import argparse
import json
import logging
import os
import subprocess
import sys
from datetime import datetime
from pathlib import Path

# Repo root, resolved from this file so the scripts work from any cwd.
ROOT = Path(__file__).resolve().parent.parent
LOG_DIR = ROOT / "logs"
# Machine-specific settings (gitignored); etc/local.example.json is the template.
LOCAL_CONFIG = ROOT / "etc" / "local.json"

# Every log line (console and files) looks like: [2026-10-29 16:30:28 INFO] message
LOG_FORMAT = "[%(asctime)s %(levelname)s] %(message)s"
DATE_FORMAT = "%Y-%m-%d %H:%M:%S"

# Shared logger; call setup_logging() once before using it.
log = logging.getLogger("schedule-1")


def dotnet_env():
    """Environment for dotnet: user-local SDK on PATH, and run on a newer runtime than the SDK targets."""
    home = Path.home()
    env = dict(os.environ)
    env["DOTNET_ROOT"] = str(home / ".dotnet")
    env["DOTNET_ROLL_FORWARD"] = "Major"
    env["PATH"] = os.pathsep.join([str(home / ".dotnet"), str(home / ".dotnet" / "tools"), env["PATH"]])
    return env


def setup_logging(name, verbose=False):
    """Log to stdout (below WARNING), stderr (WARNING+), logs/<name>.log and logs/<name>-<timestamp>.log."""
    LOG_DIR.mkdir(exist_ok=True)
    formatter = logging.Formatter(LOG_FORMAT, DATE_FORMAT)
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")

    # Split console output like a normal CLI: progress on stdout, problems on stderr.
    out = logging.StreamHandler(sys.stdout)
    out.addFilter(lambda r: r.levelno < logging.WARNING)
    err = logging.StreamHandler(sys.stderr)
    err.setLevel(logging.WARNING)
    # <name>.log is overwritten each run (always the latest); the timestamped copy is kept as history.
    handlers = [
        out,
        err,
        logging.FileHandler(LOG_DIR / f"{name}.log", mode="w", encoding="utf-8"),
        logging.FileHandler(LOG_DIR / f"{name}-{stamp}.log", encoding="utf-8"),
    ]
    log.setLevel(logging.DEBUG if verbose else logging.INFO)
    for h in handlers:
        h.setFormatter(formatter)
        log.addHandler(h)


def make_parser(description):
    """ArgumentParser with the flags every script shares."""
    p = argparse.ArgumentParser(description=description)
    p.add_argument("-v", "--verbose", action="store_true", help="log debug output")
    return p


def discover():
    """Return {mod: [flavor, ...]} for every mods/<mod>/src/<flavor>/<flavor>.csproj."""
    # The folder layout is the registry of mods: no separate list to keep in sync.
    found = {}
    for csproj in sorted((ROOT / "mods").glob("*/src/*/*.csproj")):
        # Skip stray projects whose name does not match their folder.
        if csproj.stem == csproj.parent.name:
            found.setdefault(csproj.parent.parent.parent.name, []).append(csproj.parent.name)
    return found


def add_mod_flavor(parser, required):
    """Add -m/--mod and -f/--flavor. Optional ones mean 'all'."""
    parser.add_argument("-m", "--mod", required=required, choices=sorted(discover()),
                        help="folder name under mods/" + ("" if required else " (default: all)"))
    parser.add_argument("-f", "--flavor", required=required, choices=["mono", "il2cpp"],
                        help="game branch to build for" + ("" if required else " (default: all)"))


def load_local_config():
    """Machine-specific settings from etc/local.json (gitignored); copy etc/local.example.json."""
    if not LOCAL_CONFIG.is_file():
        raise SystemExit(f"Missing {LOCAL_CONFIG}; copy etc/local.example.json and edit it.")
    with LOCAL_CONFIG.open(encoding="utf-8") as f:
        cfg = json.load(f)
    # Fail early with a clear message rather than a KeyError mid-deploy.
    for key in ("win_host", "win_game_dir"):
        if key not in cfg:
            raise SystemExit(f"{LOCAL_CONFIG} is missing '{key}'")
    return cfg


def run(cmd, *, env=None, filter_stderr=None):
    """Run a command, logging each output line. Returns stdout text. Raises CalledProcessError on failure."""
    # Output is captured then logged, so it lands in the log files as well as the console.
    log.debug("$ %s", " ".join(str(c) for c in cmd))
    proc = subprocess.run(cmd, env=env, text=True, capture_output=True)
    for line in proc.stdout.splitlines():
        if line.strip():
            log.info("%s", line)
    # stderr is surfaced as warnings; the caller can drop known-noisy lines.
    for line in proc.stderr.splitlines():
        if line.strip() and not (filter_stderr and filter_stderr(line)):
            log.warning("%s", line)
    if proc.returncode:
        raise subprocess.CalledProcessError(proc.returncode, cmd)
    return proc.stdout


def find_csproj(mod, flavor):
    """Path to mods/<mod>/src/<flavor>/<flavor>.csproj; exits if it does not exist."""
    csproj = ROOT / "mods" / mod / "src" / flavor / f"{flavor}.csproj"
    if not csproj.is_file():
        raise SystemExit(f"No {csproj}")
    return csproj


def build(mod, flavor):
    """Build mods/<mod>/src/<flavor> in Release; returns (output dir, dll file name)."""
    csproj = find_csproj(mod, flavor)
    env = dotnet_env()
    out = ROOT / "out" / mod / flavor
    # Ask msbuild for the assembly name instead of guessing it from the folder.
    name = subprocess.run(
        ["dotnet", "msbuild", str(csproj), "-getProperty:AssemblyName", "-nologo"],
        env=env, text=True, capture_output=True, check=True,
    ).stdout.strip()
    log.info("Building %s/%s", mod, flavor)
    run(["dotnet", "build", str(csproj), "-c", "Release", "-o", str(out), "--nologo", "-v", "q"], env=env)
    return out, f"{name}.dll"


def is_ssh_banner(line):
    """True for the "**" warning banner some Windows OpenSSH servers print on every scp."""
    return line.startswith("**")


def main_wrapper(fn):
    """Run fn(); log failures instead of dumping a traceback to the console only."""
    try:
        fn()
    except SystemExit:
        raise
    except subprocess.CalledProcessError as e:
        log.error("Command failed (exit %s): %s", e.returncode, " ".join(str(c) for c in e.cmd))
        sys.exit(1)
    except Exception:
        log.exception("Unhandled error")
        sys.exit(1)
