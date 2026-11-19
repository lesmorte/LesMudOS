#!/usr/bin/env bash
# Installs the public website (web/LesMudOS.Web) beside the game on a droplet set up by setup-droplet.sh.
#
# Run as root, AFTER setup-droplet.sh, with the domain's DNS already pointing at the droplet:
#     WEB_DOMAIN=legendsofhollowmere.com bash /opt/lesmudos/src/deploy/setup-website.sh
#
# It is safe to run again (that is also how the site is updated): every step checks what is already there.
# What it does, and why, is explained in docs/WEBSITE.md (section "Deployment").
#
# WHAT THE WEBSITE CAN SEE OF THE GAME (read this once)
#   The game's save directory ($SAVE_DIR) is mode 750 and owned by the game's user, and the character files in it
#   (players/NAME.json) hold each character's password hash next to its level and calling. The website's user is NOT
#   given any access to that directory. Instead a small job (lesmudos-web-sync, below) runs as the GAME's user every
#   30 seconds and writes a sanitized copy into $DATA_DIR, which only the website's user (read) and the game's user
#   (write) can enter:
#       chronicle.json, renown.json, wizlist.json   exact copies of the game's files, checked to be whole JSON first
#       roster.json                                 name, level, calling and advanced class of each character: nothing else
#   Password hashes, inventories, locations and every other field of a character never leave the save directory.
#   If the website were taken over, the most an attacker would learn is what the public pages already show, plus
#   the names of wizards (the wizard list), which the site needs in order to hide them.
#
# NOT TESTED ON A SERVER. This script was written and syntax-checked (bash -n) but never run on a droplet.

set -euo pipefail

# ---- Settings (override by exporting before you run the script) ----
WEB_DOMAIN="${WEB_DOMAIN:-legendsofhollowmere.com}"   # the site is served at https://$WEB_DOMAIN (and www redirects to it)
WEB_USER="${WEB_USER:-lesmudos-web}"
WEB_PORT="${WEB_PORT:-5080}"                          # the site listens on 127.0.0.1 only; Caddy is the public face
APP_DIR="${APP_DIR:-/opt/lesmudos}"
GAME_USER="${GAME_USER:-lesmudos}"
GAME_UNIT="${GAME_UNIT:-/etc/systemd/system/lesmudos.service}"
WEB_DIR="${WEB_DIR:-/opt/lesmudos-web}"               # the published site
DATA_DIR="${DATA_DIR:-/var/lib/lesmudos-web/data}"    # the sanitized copy of what the site may read
WEB_ENV="${WEB_ENV:-/etc/lesmudos-web.env}"           # site settings: Discord link, contact line, and so on
BRANCH="${BRANCH:-main}"
UPDATE_SOURCE="${UPDATE_SOURCE:-no}"                  # yes: fetch the latest source first (like update-droplet.sh does)

SRC_DIR="$APP_DIR/src"
SAVE_DIR="$APP_DIR/save"
WEB_UNIT="/etc/systemd/system/lesmudos-web.service"
SYNC_BIN="/usr/local/sbin/lesmudos-web-sync"

say() { printf '\n==> %s\n' "$*"; }
die() { echo "$*" >&2; exit 1; }

[ "$(id -u)" -eq 0 ] || die "Run this script as root."
[[ "$WEB_DOMAIN" =~ ^[A-Za-z0-9.-]+$ ]] || die "WEB_DOMAIN must be a plain host name such as legendsofhollowmere.com."
[[ "$WEB_PORT" =~ ^[0-9]+$ ]] || die "WEB_PORT must be a number."
export DEBIAN_FRONTEND=noninteractive DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# ---- 0. The game must be set up first ----
[ -d "$SRC_DIR/.git" ] || die "$SRC_DIR is missing: run setup-droplet.sh first."
[ -f "$GAME_UNIT" ] || die "$GAME_UNIT is missing: run setup-droplet.sh first."
id "$GAME_USER" >/dev/null 2>&1 || die "The game's user '$GAME_USER' does not exist: run setup-droplet.sh first."
GAME_PORT="$(grep -oP -- '--port \K[0-9]+' "$GAME_UNIT" || echo 4000)"

if [ "$UPDATE_SOURCE" = yes ] || [ ! -d "$SRC_DIR/web/LesMudOS.Web" ]; then
    say "Fetching the latest source ($BRANCH)"
    git -C "$SRC_DIR" fetch --quiet origin "$BRANCH"
    git -C "$SRC_DIR" checkout --quiet "$BRANCH"
    git -C "$SRC_DIR" reset --quiet --hard "origin/$BRANCH"
fi
[ -d "$SRC_DIR/web/LesMudOS.Web" ] || die "The source at $SRC_DIR has no web/ directory. Use a branch that contains the website (BRANCH=...)."
echo "Source is at commit $(git -C "$SRC_DIR" rev-parse --short HEAD)"

# ---- 1. Packages: python3 (for the sync job) and Caddy (the public web server, with automatic HTTPS) ----
say "Installing packages"
apt-get update -y
apt-get install -y python3 curl gpg debian-keyring debian-archive-keyring apt-transport-https ca-certificates
if ! command -v caddy >/dev/null 2>&1; then
    say "Installing Caddy from its official apt repository"
    curl -fsSL 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | gpg --dearmor --yes -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
    curl -fsSL 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' > /etc/apt/sources.list.d/caddy-stable.list
    apt-get update -y
    apt-get install -y caddy
fi

# ---- 2. .NET (setup-droplet.sh installed the SDK; it includes the ASP.NET Core runtime the site needs) ----
DOTNET="$(command -v dotnet || true)"
[ -n "$DOTNET" ] || die "dotnet was not found: run setup-droplet.sh first."
SDKS="$(dotnet --list-sdks 2>/dev/null || true)"
grep -q '^10\.' <<<"$SDKS" || die "The .NET 10 SDK was not found: run setup-droplet.sh first."

# ---- 3. The website's own user: no login, no home but its data directory, and no access to the game's files ----
if ! id "$WEB_USER" >/dev/null 2>&1; then
    say "Creating the user $WEB_USER"
    useradd --system --home-dir /var/lib/lesmudos-web --shell /usr/sbin/nologin "$WEB_USER"
fi
mkdir -p "$(dirname "$DATA_DIR")" "$DATA_DIR"
# $DATA_DIR: the game's user writes (through the sync job), the website's group reads, nobody else enters.
# The setgid bit makes every file created there belong to the website's group.
chown "$GAME_USER:$WEB_USER" "$(dirname "$DATA_DIR")" "$DATA_DIR"
chmod 750 "$(dirname "$DATA_DIR")"
chmod 2750 "$DATA_DIR"
# Make sure the website's user can not enter the game's save directory (setup-droplet.sh leaves it 750 for the game's user).
if [ "$(stat -c %a "$SAVE_DIR" 2>/dev/null || echo 750)" != 750 ]; then chmod 750 "$SAVE_DIR"; fi
if id -nG "$WEB_USER" | tr ' ' '\n' | grep -qx "$GAME_USER"; then gpasswd -d "$WEB_USER" "$GAME_USER" >/dev/null; fi

# ---- 4. The sync job: the only thing that reads the save directory on the website's behalf ----
say "Installing the sync job"
cat > "$SYNC_BIN" <<'SYNC'
#!/usr/bin/env python3
"""Copies what the public website may read out of the game's save directory. Runs as the game's user.
Usage: lesmudos-web-sync SAVE_DIR DATA_DIR
Copies chronicle.json, renown.json and wizlist.json (only if they are whole JSON) and writes roster.json with, for each
character, name, level, calling and advanced class. Password hashes and everything else in a character file are never read
into the output."""
import json, os, re, sys, tempfile

save, data = sys.argv[1], sys.argv[2]
NAME = re.compile(r"^[a-z]{2,16}$")

def write(name, payload):
    fd, tmp = tempfile.mkstemp(dir=data, prefix="." + name + ".")
    with os.fdopen(fd, "wb") as f:
        f.write(payload)
    os.chmod(tmp, 0o640)
    os.replace(tmp, os.path.join(data, name))

def whole_json(path):
    with open(path, "rb") as f:
        raw = f.read()
    json.loads(raw)
    return raw

for name in ("chronicle", "renown", "wizlist"):
    src = os.path.join(save, "secure", name + ".json")
    dst = os.path.join(data, name + ".json")
    if not os.path.exists(src):
        if os.path.exists(dst): os.remove(dst)
        continue
    try:
        write(name + ".json", whole_json(src))
    except Exception as e:                      # half-written or unreadable: keep the last good copy
        print(f"skipped {name}: {e}", file=sys.stderr)

previous = {}
try:
    with open(os.path.join(data, "roster.json")) as f:
        previous = json.load(f).get("characters", {})
except Exception:
    pass
roster = {}
players = os.path.join(save, "players")
for entry in sorted(os.listdir(players)) if os.path.isdir(players) else []:
    name = entry[:-5] if entry.endswith(".json") else ""
    if not NAME.match(name):
        continue
    try:
        with open(os.path.join(players, entry)) as f:
            c = json.load(f)
        level = c.get("Level", 1)
        roster[name] = {"level": level if isinstance(level, int) else 1,
                        "calling": c["archetype"] if isinstance(c.get("archetype"), str) else "",
                        "class": c["advancedClass"] if isinstance(c.get("advancedClass"), str) else ""}
    except Exception as e:
        print(f"skipped character {name}: {e}", file=sys.stderr)
        if name in previous:
            roster[name] = previous[name]
write("roster.json", json.dumps({"characters": roster}, separators=(",", ":")).encode())
SYNC
chmod 755 "$SYNC_BIN"

cat > /etc/systemd/system/lesmudos-web-sync.service <<UNIT
[Unit]
Description=Copy the public part of the game's save files for the website
After=lesmudos.service

[Service]
Type=oneshot
User=$GAME_USER
Group=$WEB_USER
UMask=0027
ExecStart=$SYNC_BIN $SAVE_DIR $DATA_DIR
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
PrivateDevices=true
ReadOnlyPaths=$SAVE_DIR
ReadWritePaths=$DATA_DIR
UNIT
cat > /etc/systemd/system/lesmudos-web-sync.timer <<UNIT
[Unit]
Description=Refresh the website's copy of the public game data every 30 seconds

[Timer]
OnBootSec=20s
OnUnitActiveSec=30s
AccuracySec=5s

[Install]
WantedBy=timers.target
UNIT

# ---- 5. Build and publish the site ----
say "Publishing the website (a minute or two)"
rm -rf "$WEB_DIR.new"
"$DOTNET" publish "$SRC_DIR/web/LesMudOS.Web" -c Release -o "$WEB_DIR.new" -v quiet
[ -f "$WEB_DIR.new/LesMudOS.Web.dll" ] || die "The build did not produce the website."
[ -f "$WEB_DIR.new/publicfilter.json" ] || die "The build is missing publicfilter.json (the secrets filter)."
[ -f "$WEB_DIR.new/content/world.json" ] || die "The build is missing content/world.json."
rm -rf "$WEB_DIR.old"
if [ -d "$WEB_DIR" ]; then mv "$WEB_DIR" "$WEB_DIR.old"; fi
mv "$WEB_DIR.new" "$WEB_DIR"
rm -rf "$WEB_DIR.old"
chown -R root:root "$WEB_DIR"
chmod -R go-w "$WEB_DIR"

# ---- 6. Settings the owner edits (created once, never overwritten) ----
if [ ! -f "$WEB_ENV" ]; then
    say "Creating $WEB_ENV"
    cat > "$WEB_ENV" <<ENV
# Settings for the website. Restart after editing:  systemctl restart lesmudos-web
Site__BaseUrl=https://$WEB_DOMAIN
Site__GameHost=$WEB_DOMAIN
Site__GamePort=$GAME_PORT
# Shown in the footer and on the support page only when filled in:
Site__DiscordUrl=
Site__SupportContact=
ENV
fi
chown root:"$WEB_USER" "$WEB_ENV"
chmod 640 "$WEB_ENV"

# ---- 7. The site's service: localhost only, confined, and unable to see the game's save directory ----
say "Installing the website's service"
cat > "$WEB_UNIT" <<UNIT
[Unit]
Description=Legends of Hollowmere website
After=network-online.target lesmudos-web-sync.service
Wants=network-online.target

[Service]
User=$WEB_USER
Group=$WEB_USER
WorkingDirectory=$WEB_DIR
EnvironmentFile=$WEB_ENV
Environment=ASPNETCORE_URLS=http://127.0.0.1:$WEB_PORT
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1
Environment=DOTNET_gcServer=0
Environment=Site__SaveDirectory=$DATA_DIR
ExecStart=$DOTNET $WEB_DIR/LesMudOS.Web.dll
Restart=on-failure
RestartSec=5

NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
PrivateDevices=true
ProtectKernelTunables=true
ProtectKernelModules=true
ProtectControlGroups=true
RestrictSUIDSGID=true
InaccessiblePaths=-$SAVE_DIR
ReadOnlyPaths=$DATA_DIR
MemoryMax=300M

[Install]
WantedBy=multi-user.target
UNIT
systemctl daemon-reload

# ---- 8. Caddy: the public server, with automatic HTTPS, and www redirected to the bare domain ----
say "Configuring Caddy for $WEB_DOMAIN"
CADDYFILE=/etc/caddy/Caddyfile
MARK="# managed by setup-website.sh"
if [ -f "$CADDYFILE" ] && ! grep -qF "$MARK" "$CADDYFILE" && [ ! -f "$CADDYFILE.before-hollowmere" ]; then
    cp "$CADDYFILE" "$CADDYFILE.before-hollowmere"
    echo "The Caddyfile that was there is saved as $CADDYFILE.before-hollowmere"
fi
cat > "$CADDYFILE" <<CADDY
$MARK
www.$WEB_DOMAIN {
	redir https://$WEB_DOMAIN{uri} permanent
}

$WEB_DOMAIN {
	encode zstd gzip
	reverse_proxy 127.0.0.1:$WEB_PORT
	header -Server
}
CADDY
caddy validate --config "$CADDYFILE" --adapter caddyfile

# ---- 9. Firewall: the web ports (the game's port and SSH were opened by setup-droplet.sh) ----
say "Opening ports 80 and 443"
ufw allow 80/tcp comment 'Website (and the certificate challenge)' >/dev/null
ufw allow 443/tcp comment 'Website' >/dev/null
ufw allow 443/udp comment 'Website (HTTP/3)' >/dev/null

# ---- 10. Start ----
say "Starting"
systemctl enable --quiet lesmudos-web lesmudos-web-sync.timer caddy
systemctl start lesmudos-web-sync.service || echo "The first sync failed; the site will show empty sections until it works. See: journalctl -u lesmudos-web-sync"
systemctl start lesmudos-web-sync.timer
systemctl restart lesmudos-web
systemctl reload-or-restart caddy
ok=no
for _ in $(seq 1 30); do
    if curl -fsS --max-time 3 "http://127.0.0.1:$WEB_PORT/healthz" >/dev/null 2>&1; then ok=yes; break; fi
    sleep 1
done

echo
echo "=================================================================="
if [ "$ok" = yes ]; then
    echo " The website is up on 127.0.0.1:$WEB_PORT and Caddy serves it at:  https://$WEB_DOMAIN"
else
    echo " The website did NOT answer within 30 seconds. Look at:  journalctl -u lesmudos-web -n 50"
fi
echo " Certificates:     Caddy fetches them by itself once $WEB_DOMAIN (and www.$WEB_DOMAIN) point at this droplet."
echo "                   Check with:  journalctl -u caddy -n 30"
echo " Settings:         $WEB_ENV   (Discord link, contact line; then: systemctl restart lesmudos-web)"
echo " News:             add a Markdown file to $WEB_DIR/content/news/ , or to web/content/news in the repository and run this script again"
echo " Data the site reads: $DATA_DIR   (refreshed every 30 s from the game by lesmudos-web-sync)"
echo " Logs:             journalctl -u lesmudos-web -f"
echo "=================================================================="
[ "$ok" = yes ]
