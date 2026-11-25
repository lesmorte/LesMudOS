#!/usr/bin/env bash
# Updates a droplet set up by setup-droplet.sh to the latest commit: rebuilds the driver, installs the
# mudlib, and starts the game again.
#
# Run as root on the droplet:   bash /opt/lesmudos/src/deploy/update-droplet.sh
#
# BEFORE YOU RUN IT: log in as a level 10 wizard and type "shutdown". That saves every character and
# moves the contents of corpses to lost property. Stopping the service from outside does neither.
# This script refuses to run while the game is up unless you pass --force.

set -euo pipefail
[ "$(id -u)" -eq 0 ] || { echo "Run this script as root." >&2; exit 1; }

APP_DIR="${APP_DIR:-/opt/lesmudos}"
if systemctl is-active --quiet lesmudos && [ "${1:-}" != "--force" ]; then
    echo "The game is running. Type \"shutdown\" in the game as a level 10 wizard first," >&2
    echo "or pass --force to stop it from outside (characters are not saved and corpse items are lost)." >&2
    exit 1
fi

# A backup first, so that a bad update can be undone.
/usr/local/sbin/lesmudos-backup

# The settings the droplet was set up with are read back from the installed service.
unit=/etc/systemd/system/lesmudos.service
GAME_PORT="$(grep -oP -- '--port \K[0-9]+' "$unit")"
# The bridge port: what the unit has (absent in units written before the bridge existed: 0 = off), unless you give BRIDGE_PORT yourself:
#     BRIDGE_PORT=4001 bash update-droplet.sh     switches the browser client's game side on;   BRIDGE_PORT=0 ...  switches it off
BRIDGE_PORT="${BRIDGE_PORT:-$(grep -oP -- '--bridge-port \K[0-9]+' "$unit" || echo 0)}"
[[ "$BRIDGE_PORT" =~ ^[0-9]+$ ]] || { echo "BRIDGE_PORT must be a number (0 = off)." >&2; exit 1; }
if grep -q "ReadWritePaths=.*$APP_DIR/mudlib\$" "$unit"; then MUDLIB_WRITABLE=all; else MUDLIB_WRITABLE=wiz; fi
export APP_DIR GAME_PORT BRIDGE_PORT MUDLIB_WRITABLE

if [ "$MUDLIB_WRITABLE" = all ]; then
    echo "NOTE: the mudlib is writable by the game. Files that wizards changed in the live world, outside"
    echo "their home directories, are replaced by the repository's versions. Commit such work first."
fi

systemctl stop lesmudos || true

# Fetch the new source here, before handing over, so that the setup script that runs is the new one
# and is not rewritten underneath itself.
BRANCH="${BRANCH:-$(git -C "$APP_DIR/src" rev-parse --abbrev-ref HEAD)}"
export BRANCH
git -C "$APP_DIR/src" fetch --quiet origin "$BRANCH"
git -C "$APP_DIR/src" reset --quiet --hard "origin/$BRANCH"
# (setup-droplet.sh adds the commented network-limit and login-throttle settings to /etc/lesmudos.env if they are not there yet; an existing
# install runs on the defaults, with the bridge listener off, until someone edits that file.)
exec bash "$APP_DIR/src/deploy/setup-droplet.sh"
