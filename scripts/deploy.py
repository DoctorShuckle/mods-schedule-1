#!/usr/bin/env python3
"""Build a mod flavor into out/<mod>/<flavor>/ and scp its DLL to a target's Mods folder (etc/local.json)."""
import common
from common import log, run


def main():
    p = common.make_parser(__doc__)
    p.add_argument("-m", "--mod", required=True, choices=sorted(common.discover()), help="folder name under mods/")
    p.add_argument("-f", "--flavor", choices=["mono", "il2cpp"], help="default: from etc/local.json")
    p.add_argument("-t", "--target", help="machine to deploy to (default: from etc/local.json)")
    args = p.parse_args()
    common.setup_logging("deploy", args.verbose)

    cfg = common.load_local_config()
    target = common.get_target(cfg, common.setting(cfg, "deploy", "target", args.target))
    flavor = common.setting(cfg, "deploy", "flavor", args.flavor) or target.get("flavor")
    if flavor not in ("mono", "il2cpp"):
        raise SystemExit("No flavor given; pass -f or set 'flavor' in etc/local.json")
    common.check_flavor(target, flavor)

    # Build first so a failed build never copies a stale DLL.
    out, dll = common.build(args.mod, flavor)
    run(["scp", "-q", str(out / dll), common.remote(target, "Mods/")], filter_stderr=common.is_ssh_banner)
    log.info("Deployed out/%s/%s/%s -> %s Mods/", args.mod, flavor, dll, target["name"])


if __name__ == "__main__":
    common.main_wrapper(main)
