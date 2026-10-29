#!/usr/bin/env bash
# Build a mod flavour into out/<mod>/<flavour>/ and copy its DLL to the Windows game's Mods folder.
# Usage: scripts/deploy.sh <mod> <flavour>
#   <mod>      folder name under mods/, e.g. persistent-pot-additives
#   <flavour>  mono or il2cpp (a project under mods/<mod>/src/); match the branch installed on Windows
set -euo pipefail
source "$(dirname "$0")/config.sh"

MOD="${1:?usage: deploy.sh <mod> <flavour>}"
FLAVOUR="${2:?usage: deploy.sh <mod> <flavour>}"
CSPROJ="$ROOT/mods/$MOD/src/$FLAVOUR/$FLAVOUR.csproj"
[ -f "$CSPROJ" ] || { echo "No $CSPROJ" >&2; exit 1; }

OUT="$ROOT/out/$MOD/$FLAVOUR"
DLL="$(dotnet msbuild "$CSPROJ" -getProperty:AssemblyName -nologo | tr -d '[:space:]').dll"

dotnet build "$CSPROJ" -c Release -o "$OUT" --nologo -v q
scp -q "$OUT/$DLL" "$WIN_HOST:$WIN_GAME_DIR/Mods/" 2> >(grep -v '^\*\*' >&2)
echo "Deployed out/$MOD/$FLAVOUR/$DLL -> Mods/"
