#!/usr/bin/env bash
# Print the latest MelonLoader log from the Windows machine.
source "$(dirname "$0")/config.sh"
scp -q "$WIN_HOST:$WIN_GAME_DIR/MelonLoader/Latest.log" "$ROOT/Latest.log" 2> >(grep -v '^\*\*' >&2) && cat "$ROOT/Latest.log"
