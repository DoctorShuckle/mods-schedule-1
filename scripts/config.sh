# Shared settings for the build/deploy scripts.
export DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_ROLL_FORWARD=Major
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Machine-specific settings (WIN_HOST, WIN_GAME_DIR) live in etc/local.sh, which is gitignored.
# Copy etc/local.example.sh to etc/local.sh and edit it.
[ -f "$ROOT/etc/local.sh" ] && source "$ROOT/etc/local.sh"
