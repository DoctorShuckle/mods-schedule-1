# Copy to etc/local.sh and edit. Only needed for deploy.sh / log.sh (copying to a remote game PC over scp).
# Host reachable over SSH (user@host or an alias from ~/.ssh/config).
WIN_HOST="user@windows-pc"
# Game folder as scp sees it on the Windows host (OpenSSH on Windows uses /C:/... style paths).
WIN_GAME_DIR="/C:/Program Files (x86)/Steam/steamapps/common/Schedule I"
