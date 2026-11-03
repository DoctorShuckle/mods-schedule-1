#!/usr/bin/env python3
"""Copy game and MelonLoader assemblies from a source machine (etc/local.json) into lib/<flavor>.

Existing files are overwritten; the log lists how many changed. Optionally decompiles with ilspycmd.
"""
import filecmp
import shutil
import tempfile
from pathlib import Path

import common
from common import ROOT, log, run

# flavor -> [(folder in the game install, folder under lib/)]
SOURCES = {
    "mono": [("Schedule I_Data/Managed", "mono"), ("MelonLoader/net35", "melonloader-mono")],
    "il2cpp": [("MelonLoader/Il2CppAssemblies", "il2cpp"), ("MelonLoader/net6", "melonloader-il2cpp")],
}


def merge(src, dest):
    """Copy every file of src into dest, overwriting; returns (changed, total)."""
    dest.mkdir(parents=True, exist_ok=True)
    changed = total = 0
    for f in sorted(p for p in src.rglob("*") if p.is_file()):
        target = dest / f.relative_to(src)
        total += 1
        if target.is_file() and filecmp.cmp(f, target, shallow=False):
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(f, target)
        changed += 1
        log.debug("updated %s", target)
    return changed, total


def decompile(flavor):
    """ilspycmd project output of Assembly-CSharp into decompiled/<flavor>/Assembly-CSharp."""
    dll = ROOT / "lib" / flavor / "Assembly-CSharp.dll"
    out = ROOT / "decompiled" / flavor / "Assembly-CSharp"
    out.mkdir(parents=True, exist_ok=True)
    log.info("Decompiling %s (slow)", dll.relative_to(ROOT))
    run(["ilspycmd", "-p", "-o", str(out), str(dll)], env=common.dotnet_env())


def main():
    p = common.make_parser(__doc__)
    p.add_argument("-f", "--flavor", choices=["mono", "il2cpp"], help="default: from etc/local.json")
    p.add_argument("-t", "--target", help="machine to pull from (default: from etc/local.json)")
    p.add_argument("-d", "--decompile", action="store_true", default=None, help="also run ilspycmd afterwards")
    args = p.parse_args()
    common.setup_logging("pull", args.verbose)

    cfg = common.load_local_config()
    target = common.get_target(cfg, common.setting(cfg, "pull", "target", args.target))
    flavor = common.setting(cfg, "pull", "flavor", args.flavor)
    if flavor not in SOURCES:
        raise SystemExit("No flavor given; pass -f or set 'flavor' in etc/local.json")
    # A machine on the other flavor has no such assemblies; say so instead of failing mid-copy.
    expected = target.get("flavor")
    if expected and expected != flavor:
        raise SystemExit(f"Target '{target['name']}' runs {expected}, so it has no {flavor} assemblies")
    do_decompile = common.setting(cfg, "pull", "decompile", args.decompile)

    failed = []
    for src_rel, lib_name in SOURCES[flavor]:
        # scp -r into a scratch folder first, so a failed copy never leaves lib/ half-written.
        with tempfile.TemporaryDirectory() as tmp:
            log.info("Pulling %s:%s -> lib/%s", target["name"], src_rel, lib_name)
            try:
                run(["scp", "-q", "-r", common.remote(target, src_rel), tmp], filter_stderr=common.is_ssh_banner)
            except Exception as e:
                log.error("Failed to pull %s: %s", src_rel, e)
                failed.append(src_rel)
                continue
            changed, total = merge(Path(tmp) / Path(src_rel).name, ROOT / "lib" / lib_name)
            log.info("lib/%s: %d of %d files updated", lib_name, changed, total)
    if failed:
        raise SystemExit(f"Could not pull: {', '.join(failed)}")
    if do_decompile:
        decompile(flavor)


if __name__ == "__main__":
    common.main_wrapper(main)
