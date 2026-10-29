#!/usr/bin/env python3
"""Build mods into out/<mod>/<flavor>/. With no arguments, builds every flavor of every mod."""
import sys

import common
from common import ROOT, log


def main():
    mods = common.discover()
    p = common.make_parser(__doc__)
    common.add_mod_flavor(p, required=False)
    args = p.parse_args()
    common.setup_logging("build", args.verbose)

    # No -m/-f means "all": narrow the full mod/flavor list down by whichever filters were given.
    targets = [
        (mod, flavor)
        for mod, flavors in mods.items()
        if args.mod in (None, mod)
        for flavor in flavors
        if args.flavor in (None, flavor)
    ]
    if not targets:
        raise SystemExit("Nothing to build")

    # Keep going after a failure so one broken flavor does not hide the others; report at the end.
    failed = []
    for mod, flavor in targets:
        try:
            out, dll = common.build(mod, flavor)
            log.info("Built out/%s/%s/%s", mod, flavor, dll)
        except Exception as e:
            log.error("Failed %s/%s: %s", mod, flavor, e)
            failed.append(f"{mod}/{flavor}")

    if failed:
        log.error("%d of %d builds failed: %s", len(failed), len(targets), ", ".join(failed))
        sys.exit(1)
    log.info("Built %d target(s)", len(targets))


if __name__ == "__main__":
    common.main_wrapper(main)
