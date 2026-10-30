#!/usr/bin/env python3
"""Run the unit tests in mods/<mod>/tests/. With no arguments, runs every mod's tests."""
import sys

import common
from common import ROOT, log


def discover_tests():
    """Return {mod: csproj} for every mods/<mod>/tests/*.csproj."""
    return {p.parent.parent.name: p for p in sorted((ROOT / "mods").glob("*/tests/*.csproj"))}


def main():
    tests = discover_tests()
    p = common.make_parser(__doc__)
    p.add_argument("-m", "--mod", choices=sorted(tests), help="folder name under mods/ (default: all)")
    p.add_argument("-k", "--filter", help="only run tests matching this dotnet test --filter expression")
    args = p.parse_args()
    common.setup_logging("test", args.verbose)

    targets = {mod: csproj for mod, csproj in tests.items() if args.mod in (None, mod)}
    if not targets:
        raise SystemExit("No tests found")

    # Keep going after a failure so one mod's failing tests do not hide the others.
    failed = []
    for mod, csproj in targets.items():
        log.info("Testing %s", mod)
        cmd = ["dotnet", "test", str(csproj), "--nologo", "-v", "q"]
        if args.filter:
            cmd += ["--filter", args.filter]
        try:
            common.run(cmd, env=common.dotnet_env())
        except Exception as e:
            log.error("Tests failed for %s: %s", mod, e)
            failed.append(mod)

    if failed:
        log.error("%d of %d test project(s) failed: %s", len(failed), len(targets), ", ".join(failed))
        sys.exit(1)
    log.info("Tested %d mod(s)", len(targets))


if __name__ == "__main__":
    common.main_wrapper(main)
