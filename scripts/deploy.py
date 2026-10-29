#!/usr/bin/env python3
"""Build a mod flavor into out/<mod>/<flavor>/ and scp its DLL to the Windows game's Mods folder."""
import common
from common import log, run


def main():
    p = common.make_parser(__doc__)
    common.add_mod_flavor(p, required=True)
    args = p.parse_args()
    common.setup_logging("deploy", args.verbose)

    cfg = common.load_local_config()
    # Build first so a failed build never copies a stale DLL.
    out, dll = common.build(args.mod, args.flavor)
    # The Windows host expects /C:/... style paths (see etc/local.example.json).
    run(["scp", "-q", str(out / dll), f"{cfg['win_host']}:{cfg['win_game_dir']}/Mods/"],
        filter_stderr=common.is_ssh_banner)
    log.info("Deployed out/%s/%s/%s -> Mods/", args.mod, args.flavor, dll)


if __name__ == "__main__":
    common.main_wrapper(main)
