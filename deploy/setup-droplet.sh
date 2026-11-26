#!/usr/bin/env bash
# Sets up a fresh Ubuntu 24.04 droplet to run Legends of Hollowmere on the LesMudOS driver.
#
# Run as root on the droplet:
#     curl -fsSL https://raw.githubusercontent.com/lesmorte/LesMudOS/main/deploy/setup-droplet.sh -o setup-droplet.sh
#     less setup-droplet.sh          # read it first
#     bash setup-droplet.sh
#
# It is safe to run again: every step checks what is already there.
# What it does, and why, is explained in docs/DEPLOY.md.

set -euo pipefail

# ---- Settings (override by exporting before you run the script) ----
REPO_URL="${REPO_URL:-https://github.com/lesmorte/LesMudOS.git}"
BRANCH="${BRANCH:-main}"
GAME_PORT="${GAME_PORT:-4000}"            # the telnet port players connect to
BRIDGE_PORT="${BRIDGE_PORT:-0}"          # the loopback port of the website's browser client: 0 = off (default); 4001 once the website side is deployed. Never opened in the firewall.
APP_DIR="${APP_DIR:-/opt/lesmudos}"
APP_USER="${APP_USER:-lesmudos}"
# Which parts of the mudlib the running game may write to:
#   wiz  = wizards' home directories only (in-game editing works only there)
#   all  = the whole mudlib (level 10 wizards can edit the live world; needed for publishing sandbox work)
MUDLIB_WRITABLE="${MUDLIB_WRITABLE:-wiz}"
SWAP_MB="${SWAP_MB:-1024}"                # swap file size; 0 for none
BACKUP_KEEP_DAYS="${BACKUP_KEEP_DAYS:-14}"

SRC_DIR="$APP_DIR/src"
BIN_DIR="$APP_DIR/bin"
MUDLIB_DIR="$APP_DIR/mudlib"
SAVE_DIR="$APP_DIR/save"
BACKUP_DIR="/var/backups/lesmudos"
ENV_FILE="/etc/lesmudos.env"
UNIT_FILE="/etc/systemd/system/lesmudos.service"

say() { printf '\n==> %s\n' "$*"; }

[ "$(id -u)" -eq 0 ] || { echo "Run this script as root." >&2; exit 1; }
case "$MUDLIB_WRITABLE" in wiz|all) ;; *) echo "MUDLIB_WRITABLE must be wiz or all." >&2; exit 1;; esac
export DEBIAN_FRONTEND=noninteractive DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# ---- 1. System packages ----
say "Updating the system and installing packages"
apt-get update -y
apt-get upgrade -y
apt-get install -y git rsync ufw fail2ban unattended-upgrades ca-certificates curl

# ---- 2. Swap (the C# compiler inside the driver is memory-hungry on a small droplet) ----
if [ "$SWAP_MB" -gt 0 ] && ! swapon --show | grep -q .; then
    say "Creating a ${SWAP_MB} MB swap file"
    fallocate -l "${SWAP_MB}M" /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    grep -q '^/swapfile ' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

# ---- 3. .NET 10 SDK (needed to build; the game itself is published with its own runtime) ----
DOTNET=""
if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
    DOTNET="$(command -v dotnet)"
else
    say "Installing the .NET 10 SDK"
    if apt-get install -y dotnet-sdk-10.0; then
        DOTNET="$(command -v dotnet)"
    else
        echo "The Ubuntu package was not available; using Microsoft's install script instead."
        curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
        bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /opt/dotnet
        ln -sf /opt/dotnet/dotnet /usr/local/bin/dotnet
        DOTNET=/usr/local/bin/dotnet
    fi
fi
"$DOTNET" --version

# ---- 4. The game's own user ----
if ! id "$APP_USER" >/dev/null 2>&1; then
    say "Creating the user $APP_USER"
    useradd --system --home-dir "$APP_DIR" --shell /usr/sbin/nologin "$APP_USER"
fi
mkdir -p "$APP_DIR" "$BIN_DIR" "$MUDLIB_DIR" "$SAVE_DIR" "$BACKUP_DIR"

# ---- 5. Source ----
if [ -d "$SRC_DIR/.git" ]; then
    say "Updating the source in $SRC_DIR"
    git -C "$SRC_DIR" fetch --quiet origin "$BRANCH"
    git -C "$SRC_DIR" checkout --quiet "$BRANCH"
    git -C "$SRC_DIR" reset --quiet --hard "origin/$BRANCH"
else
    say "Cloning $REPO_URL"
    git clone --quiet --branch "$BRANCH" "$REPO_URL" "$SRC_DIR"
fi
echo "Source is at commit $(git -C "$SRC_DIR" rev-parse --short HEAD)"

# ---- 6. Build ----
say "Building the driver (this takes a few minutes on a small droplet)"
rm -rf "$BIN_DIR.new"
"$DOTNET" publish "$SRC_DIR/src/LesMudOS.Driver" -c Release -r linux-x64 --self-contained true -o "$BIN_DIR.new" -v quiet
# Single-file publishing is not used: the compiler inside the driver needs the framework assemblies as files.
[ -x "$BIN_DIR.new/LesMudOS.Driver" ] || { echo "The build did not produce the driver." >&2; exit 1; }
rm -rf "$BIN_DIR.old"
[ -d "$BIN_DIR" ] && mv "$BIN_DIR" "$BIN_DIR.old"
mv "$BIN_DIR.new" "$BIN_DIR"
rm -rf "$BIN_DIR.old"

# ---- 7. Mudlib ----
# The repository is the source of truth for the world. Wizards' home directories (wiz/) live only on the
# server, so they are never overwritten or deleted here. Build output (bin/, obj/ at the top) is not copied.
say "Installing the mudlib"
rsync -a --delete \
    --exclude '/bin/' --exclude '/obj/' --exclude '/Mudlib.csproj' \
    --exclude '/wiz/*/' \
    "$SRC_DIR/mudlib/" "$MUDLIB_DIR/"
mkdir -p "$MUDLIB_DIR/wiz"

# ---- 8. Ownership ----
# The game may always write its save files and the wizards' homes. Everything else is root's unless
# MUDLIB_WRITABLE=all.
chown -R root:root "$BIN_DIR" "$SRC_DIR"
chown -R "$APP_USER:$APP_USER" "$SAVE_DIR"
if [ "$MUDLIB_WRITABLE" = "all" ]; then
    chown -R "$APP_USER:$APP_USER" "$MUDLIB_DIR"
    RW_PATHS="$SAVE_DIR $MUDLIB_DIR"
else
    chown -R root:root "$MUDLIB_DIR"
    chown -R "$APP_USER:$APP_USER" "$MUDLIB_DIR/wiz"
    RW_PATHS="$SAVE_DIR $MUDLIB_DIR/wiz"
fi
chmod 750 "$SAVE_DIR"

# ---- 9. The admin secret ----
# The first level 10 wizard is made in the game with:  promote <secret>
# It works only while no level 10 wizard exists. The secret is never in the repository.
NEW_SECRET=""
if [ ! -f "$ENV_FILE" ]; then
    say "Generating the admin secret"
    # Not 'tr < /dev/urandom | head': with pipefail, head closing the pipe kills tr and the script.
    NEW_SECRET="$(head -c 256 /dev/urandom | od -An -tx1 | tr -d ' \n' | cut -c1-32)"
    printf 'LESMUDOS_ADMIN_SECRET=%s\n' "$NEW_SECRET" > "$ENV_FILE"
fi
# The network limits and the login throttle (docs/DEPLOY.md, docs/specs/login-throttle.md). Added once, to a new or an existing file, with every
# line commented out: the defaults shown are the ones in force. The bridge listener stays OFF (no LESMUDOS_BRIDGE_PORT) until the website's
# browser client is deployed; when it is, run  BRIDGE_PORT=4001 bash setup-droplet.sh  (writes --bridge-port 4001 into the unit; loopback only, never open it in the firewall). LESMUDOS_BRIDGE_PORT in this file would override it.
if ! grep -q '^# ---- network limits' "$ENV_FILE"; then
    cat >> "$ENV_FILE" <<'LIMITS'

# ---- network limits and login throttle: defaults shown, uncomment to change; 0 = no limit for a count or a rate ----
# LESMUDOS_BRIDGE_PORT=0                      # overrides --bridge-port in the unit. 0 = no bridge listener; 4001 = the browser client may connect (127.0.0.1 only)
# LESMUDOS_MAX_CONNECTIONS=200                # all connections together
# LESMUDOS_MAX_PER_ADDRESS=10                 # per address (IPv6 by /64); a web player counts under his real address
# LESMUDOS_CONNECTS_PER_MINUTE=20             # new connections per address per minute
# LESMUDOS_MAX_LINES_PER_SECOND=20            # typed lines per second per connection (burst LESMUDOS_INPUT_BURST=60); lines over it are delayed, never dropped
# LESMUDOS_THROTTLE_NAME_FAILURES=5           # wrong passwords on one name within 15 minutes lock the name ...
# LESMUDOS_THROTTLE_NAME_LOCK_SECONDS=300     # ... for 5 minutes, doubling each time up to LESMUDOS_THROTTLE_NAME_LOCK_MAX_SECONDS=3600
# LESMUDOS_THROTTLE_ADDRESS_FAILURES=40       # wrong passwords from one address (any names) within 15 minutes lock the address ...
# LESMUDOS_THROTTLE_ADDRESS_LOCK_SECONDS=900  # ... for 15 minutes, doubling up to LESMUDOS_THROTTLE_ADDRESS_LOCK_MAX_SECONDS=14400
# LESMUDOS_THROTTLE_NEW_PER_HOUR=3            # new characters per address per hour
# LESMUDOS_THROTTLE_NEW_PER_DAY=10            # and per day
LIMITS
fi
chown root:root "$ENV_FILE"
chmod 600 "$ENV_FILE"

# ---- 10. The service ----
say "Installing the systemd service"
cat > "$UNIT_FILE" <<UNIT
[Unit]
Description=Legends of Hollowmere (LesMudOS game driver)
After=network-online.target
Wants=network-online.target

[Service]
User=$APP_USER
Group=$APP_USER
WorkingDirectory=$APP_DIR
EnvironmentFile=-$ENV_FILE
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1
ExecStart=$BIN_DIR/LesMudOS.Driver --mudlib $MUDLIB_DIR --save $SAVE_DIR --port $GAME_PORT --bridge-port $BRIDGE_PORT
# The in-game "shutdown" command exits cleanly (status 0), and the game then stays down until it is
# started again. A crash is restarted automatically.
Restart=on-failure
RestartSec=5

# Confine the process: the mudlib sandbox is not a security boundary.
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
PrivateDevices=true
ProtectKernelTunables=true
ProtectKernelModules=true
ProtectControlGroups=true
RestrictSUIDSGID=true
ReadWritePaths=$RW_PATHS
MemoryMax=768M
MemorySwapMax=0
# .NET's own heap limit, below the ceiling (hex percent of it: 0x46 = 70%): an allocation past it fails as an error in the code
# that made it, instead of the kernel killing the process; the driver saves everyone at three quarters of it.
Environment=DOTNET_GCHeapHardLimitPercent=0x46

[Install]
WantedBy=multi-user.target
UNIT
systemctl daemon-reload
systemctl enable --quiet lesmudos

# ---- 11. Firewall and brute-force protection ----
say "Configuring the firewall"
ufw allow OpenSSH >/dev/null
ufw allow "$GAME_PORT/tcp" comment 'Legends of Hollowmere' >/dev/null
ufw --force enable >/dev/null
systemctl enable --quiet --now fail2ban
# Security updates install themselves.
cat > /etc/apt/apt.conf.d/20auto-upgrades <<'APT'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
APT
systemctl enable --quiet --now unattended-upgrades

# ---- 12. Nightly backup of characters, history and wizards' work ----
say "Installing the nightly backup"
cat > /usr/local/sbin/lesmudos-backup <<BACKUP
#!/usr/bin/env bash
# Copies the save directory and the wizards' homes into one dated archive, and drops old archives.
set -euo pipefail
stamp="\$(date +%Y-%m-%d)"
tar -czf "$BACKUP_DIR/lesmudos-\$stamp.tar.gz" -C "$APP_DIR" save mudlib/wiz
find "$BACKUP_DIR" -name 'lesmudos-*.tar.gz' -mtime +$BACKUP_KEEP_DAYS -delete
BACKUP
chmod 750 /usr/local/sbin/lesmudos-backup
chmod 700 "$BACKUP_DIR"
echo '17 4 * * * root /usr/local/sbin/lesmudos-backup' > /etc/cron.d/lesmudos-backup

# ---- 13. Start ----
say "Starting the game"
systemctl restart lesmudos
started=no
for _ in $(seq 1 60); do
    if (exec 3<>"/dev/tcp/127.0.0.1/$GAME_PORT") 2>/dev/null; then started=yes; break; fi
    sleep 1
done

IP="$(curl -fsS --max-time 5 http://169.254.169.254/metadata/v1/interfaces/public/0/ipv4/address 2>/dev/null || hostname -I | awk '{print $1}')"
echo
echo "=================================================================="
if [ "$started" = yes ]; then
    echo " The game is up:   telnet $IP $GAME_PORT"
else
    echo " The game did NOT answer on port $GAME_PORT within a minute."
    echo " Look at:          journalctl -u lesmudos -n 50"
fi
echo " Logs:             journalctl -u lesmudos -f"
echo " Mudlib writable:  $MUDLIB_WRITABLE"
echo " Backups:          $BACKUP_DIR (nightly, kept $BACKUP_KEEP_DAYS days)"
if [ -n "$NEW_SECRET" ]; then
    echo
    echo " ADMIN SECRET (shown once; it is also in $ENV_FILE):"
    echo "     $NEW_SECRET"
    echo " Log in, create your character, and type:   promote $NEW_SECRET"
    echo " Then remove the secret:  rm $ENV_FILE && systemctl restart lesmudos"
    echo " (Restarting drops everyone who is connected; do it before you invite players.)"
fi
echo "=================================================================="
[ "$started" = yes ]
