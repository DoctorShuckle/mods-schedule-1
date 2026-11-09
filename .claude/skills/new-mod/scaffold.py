#!/usr/bin/env python3
"""Create mods/<kebab>/ from the templates next to this script.

    .claude/skills/new-mod/scaffold.py configurable-foo --topic Foo [--title "Configurable Foo"] [--out DIR]

Fills in {{Kebab}}, {{Pascal}}, {{Title}}, {{Topic}} and fresh solution GUIDs. Every other {{...}} is a
fill-in for the person (or agent) writing the mod: the script lists them at the end. It refuses to write
into a folder that already exists.
"""
import argparse
import re
import sys
import uuid
from pathlib import Path

HERE = Path(__file__).resolve().parent
TEMPLATES = HERE / "templates"
REPO = HERE.parents[2]

# template path (under templates/) -> output path (under the mod folder). Names may use the same fields.
FILES = {
    "mod.sln.tmpl": "{Kebab}.sln",
    "README.md.tmpl": "README.md",
    "mono.csproj.tmpl": "src/mono/mono.csproj",
    "il2cpp.csproj.tmpl": "src/il2cpp/il2cpp.csproj",
    "src/common/Mod.cs.tmpl": "src/common/{Pascal}Mod.cs",
    "src/common/GlobalUsings.cs.tmpl": "src/common/GlobalUsings.cs",
    "src/common/Config/ModConfig.cs.tmpl": "src/common/Config/ModConfig.cs",
    "src/common/Game/GameAccess.cs.tmpl": "src/common/Game/GameAccess.cs",
    "src/common/Patches/Patches.cs.tmpl": "src/common/Patches/{Topic}Patches.cs",
    "src/common/Rules/Rules.cs.tmpl": "src/common/Rules/{Topic}Rules.cs",
    "tests/tests.csproj.tmpl": "tests/tests.csproj",
    "tests/Directory.Build.props.tmpl": "tests/Directory.Build.props",
    "tests/Canary/GameApi.cs.tmpl": "tests/Canary/GameApi.cs",
    "tests/Canary/GameApiCanaryTests.cs.tmpl": "tests/Canary/GameApiCanaryTests.cs",
    "tests/Rules/RulesTests.cs.tmpl": "tests/Rules/{Topic}RulesTests.cs",
}


def pascal(kebab):
    return "".join(part.capitalize() for part in kebab.split("-"))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("kebab", help="folder name, kebab-case (configurable-foo)")
    ap.add_argument("--topic", required=True, help="PascalCase name for the first Patches/Rules files (Foo)")
    ap.add_argument("--title", help="display name for MelonInfo and the README (default: from the folder name)")
    ap.add_argument("--out", help="mod folder to create (default: mods/<kebab>)")
    args = ap.parse_args()

    if not re.fullmatch(r"[a-z][a-z0-9]*(-[a-z0-9]+)*", args.kebab):
        sys.exit("kebab must be lower-case words joined by hyphens, e.g. configurable-foo")
    if not re.fullmatch(r"[A-Z][A-Za-z0-9]*", args.topic):
        sys.exit("--topic must be PascalCase, e.g. Foo")

    fields = {
        "Kebab": args.kebab,
        "Pascal": pascal(args.kebab),
        "Title": args.title or " ".join(w.capitalize() for w in args.kebab.split("-")),
        "Topic": args.topic,
        "GUID_SRC": str(uuid.uuid4()).upper(),
        "GUID_MONO": str(uuid.uuid4()).upper(),
        "GUID_IL2CPP": str(uuid.uuid4()).upper(),
    }

    out = Path(args.out) if args.out else REPO / "mods" / args.kebab
    if out.exists():
        sys.exit(f"{out} already exists; not touching it")

    def fill(text):
        return re.sub(r"\{\{(\w+)\}\}", lambda m: fields.get(m.group(1), m.group(0)), text)

    for template, target in FILES.items():
        source = TEMPLATES / template
        if not source.is_file():
            sys.exit(f"missing template {template}")
        destination = out / fill(target.replace("{", "{{").replace("}", "}}"))
        destination.parent.mkdir(parents=True, exist_ok=True)
        # Keep each file's own line endings and encoding: read and write bytes-for-bytes through str.
        destination.write_text(fill(source.read_text(encoding="utf-8")), encoding="utf-8", newline="")

    print(f"Created {out}")
    left = 0
    for path in sorted(out.rglob("*")):
        if path.is_file():
            n = len(re.findall(r"\{\{[^}]*\}\}", path.read_text(encoding="utf-8")))
            if n:
                left += n
                print(f"  {path.relative_to(out)}: {n} fill-in(s) left")
    print(f"{left} fill-in(s) to replace before the mod builds or is committed.")


if __name__ == "__main__":
    main()
