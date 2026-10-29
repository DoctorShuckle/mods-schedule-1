#!/usr/bin/env python3
"""Build a mod flavor and zip it so the archive root mirrors the game folder (drag-and-drop for Vortex).

Archive layout:  Mods/<AssemblyName>.dll
Output:          out/bundles/<mod>-<flavor>-<version>.zip
"""
import subprocess
import zipfile

import common
from common import ROOT, log


def git_version():
    """Default bundle version: the short commit hash, with -dirty if there are uncommitted changes."""
    try:
        return subprocess.run(
            ["git", "-C", str(ROOT), "describe", "--always", "--dirty"],
            text=True, capture_output=True, check=True,
        ).stdout.strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        return "dev"  # not a git checkout, or git is missing


def main():
    p = common.make_parser(__doc__)
    common.add_mod_flavor(p, required=True)
    p.add_argument("-V", "--version", help="defaults to `git describe --always --dirty`")
    args = p.parse_args()
    common.setup_logging("bundle", args.verbose)

    version = args.version or git_version()
    out, dll = common.build(args.mod, args.flavor)

    bundles = ROOT / "out" / "bundles"
    bundles.mkdir(parents=True, exist_ok=True)
    zip_path = bundles / f"{args.mod}-{args.flavor}-{version}.zip"
    zip_path.unlink(missing_ok=True)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
        # Path inside the zip mirrors the game folder, so Vortex can deploy it as-is.
        z.write(out / dll, f"Mods/{dll}")

    log.info("Bundled %s", zip_path)
    with zipfile.ZipFile(zip_path) as z:
        for info in z.infolist():
            log.info("  %8d  %s", info.file_size, info.filename)


if __name__ == "__main__":
    common.main_wrapper(main)
