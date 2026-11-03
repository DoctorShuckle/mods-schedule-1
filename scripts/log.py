#!/usr/bin/env python3
"""Fetch MelonLoader's Latest.log from a target (etc/local.json) into the repo root and print it."""
import common
from common import log, run


def main():
    p = common.make_parser(__doc__)
    p.add_argument("-t", "--target", help="machine to read from (default: from etc/local.json)")
    p.add_argument("-n", "--tail", type=int, help="print only the last N lines (default: from etc/local.json, else all)")
    args = p.parse_args()
    common.setup_logging("log", args.verbose)

    cfg = common.load_local_config()
    target = common.get_target(cfg, common.setting(cfg, "log", "target", args.target))
    tail = common.setting(cfg, "log", "tail", args.tail)
    # Saved next to the repo root (gitignored) so it can be opened in an editor afterwards.
    dest = common.ROOT / "Latest.log"
    run(["scp", "-q", common.remote(target, target["log_file"]), str(dest)], filter_stderr=common.is_ssh_banner)
    log.info("Fetched %s from %s", dest, target["name"])
    text = dest.read_text(encoding="utf-8", errors="replace")
    if tail:
        text = "".join(text.splitlines(keepends=True)[-tail:])
    # Raw print, not log.info: this is the log content itself, not a message about this script.
    print(text, end="")


if __name__ == "__main__":
    common.main_wrapper(main)
