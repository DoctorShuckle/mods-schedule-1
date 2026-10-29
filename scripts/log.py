#!/usr/bin/env python3
"""Fetch MelonLoader's Latest.log from the Windows machine into the repo root and print it."""
import common
from common import log, run


def main():
    p = common.make_parser(__doc__)
    args = p.parse_args()
    common.setup_logging("log", args.verbose)

    cfg = common.load_local_config()
    # Saved next to the repo root (gitignored) so it can be opened in an editor afterwards.
    dest = common.ROOT / "Latest.log"
    run(["scp", "-q", f"{cfg['win_host']}:{cfg['win_game_dir']}/MelonLoader/Latest.log", str(dest)],
        filter_stderr=common.is_ssh_banner)
    log.info("Fetched %s", dest)
    # Raw print, not log.info: this is the log content itself, not a message about this script.
    print(dest.read_text(encoding="utf-8", errors="replace"), end="")


if __name__ == "__main__":
    common.main_wrapper(main)
