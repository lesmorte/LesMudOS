# Running Legends of Hollowmere on a DigitalOcean droplet

How to create the droplet, set it up with one script, make the first wizard, point the domain at it, and keep it updated and backed up.

**Status.** The build, install and start steps of the script were tested on a Linux machine: the driver was published with its own runtime, the mudlib copied, the game started and a connection accepted. The script has not yet been run end to end on a real droplet, so the firewall, service, swap and backup steps are untested there. Expect to fix something small on the first run, and tell the repository what it was.

## 1. What you are setting up

| Thing | Where | Notes |
|---|---|---|
| The droplet | DigitalOcean, Ubuntu 24.04 LTS | 2 GB of memory recommended |
| The source | `/opt/lesmudos/src` | A clone of the public repository |
| The driver | `/opt/lesmudos/bin` | Built on the droplet, with its own copy of the .NET runtime |
| The world | `/opt/lesmudos/mudlib` | Copied from the source; wizards' homes in `mudlib/wiz` exist only here |
| Characters and history | `/opt/lesmudos/save` | Characters, the wizard list, lost property, the Chronicle, Renown |
| The service | `lesmudos` (systemd) | Runs as its own user with no login; restarts after a crash |
| Players connect to | port 4000 (telnet) | Change with `GAME_PORT` |
| Backups | `/var/backups/lesmudos` | Nightly, kept 14 days |

## 2. Create the droplet

### Size

The driver compiles the world's C# files as it loads them, and the compiler is the hungry part: about 180 MB with the small sample world and one player, growing with the number of files loaded.

| Droplet | Use |
|---|---|
| 1 GB, 1 vCPU (`s-1vcpu-1gb`) | Enough to try it out. The script adds a 1 GB swap file, which this size needs during the build. |
| 2 GB, 1 vCPU (`s-1vcpu-2gb`) | Recommended to start with real players. |
| 4 GB, 2 vCPU (`s-2vcpu-4gb`) | When the world is large or many wizards are building at once. |

A droplet can be resized later from the control panel. Prices are on DigitalOcean's pricing page; check them there, because they change.

The service is limited to 768 MB (`MemoryMax` in the unit file). On a 4 GB droplet raise it.

### In the control panel

1. **Create**, then **Droplets**.
2. **Region:** the one nearest your players (New York or Toronto for the eastern United States).
3. **Image:** Ubuntu 24.04 (LTS) x64.
4. **Size:** Basic, Regular, 2 GB / 1 CPU.
5. **Authentication:** SSH key. Add your public key if it is not there. Do not use a password.
6. **Options:** turn on Monitoring. Turn on Backups if you want DigitalOcean's weekly image of the whole droplet in addition to the nightly game backup (it costs extra).
7. **Hostname:** `hollowmere`.
8. **Create Droplet**, and note the IP address.

### Or from the command line

With `doctl` installed and signed in (`doctl auth init`):

```bash
doctl compute ssh-key list                      # find your key's ID or fingerprint
doctl compute droplet create hollowmere \
  --region nyc3 \
  --size s-1vcpu-2gb \
  --image ubuntu-24-04-x64 \
  --ssh-keys <your key id or fingerprint> \
  --enable-monitoring \
  --wait
doctl compute droplet get hollowmere --format Name,PublicIPv4,Status
```

`doctl compute size list` and `doctl compute region list` show what is available.

## 3. Populate it

Sign in as root and run the setup script:

```bash
ssh root@<droplet ip>
curl -fsSL https://raw.githubusercontent.com/lesmorte/LesMudOS/main/deploy/setup-droplet.sh -o setup-droplet.sh
less setup-droplet.sh        # read it before you run it
bash setup-droplet.sh
```

It takes about five to ten minutes, most of it the system update and the build. At the end it prints the address to connect to and, the first time only, the admin secret.

### What the script does

1. Updates Ubuntu and installs git, rsync, a firewall (`ufw`), `fail2ban` and automatic security updates.
2. Adds a swap file if the droplet has none.
3. Installs the .NET 10 SDK from Ubuntu's own packages, or with Microsoft's install script if the package is missing.
4. Creates the `lesmudos` user, which cannot log in.
5. Clones the repository to `/opt/lesmudos/src`.
6. Builds the driver with its own runtime into `/opt/lesmudos/bin`.
7. Copies the world to `/opt/lesmudos/mudlib`, leaving wizards' homes alone.
8. Sets ownership so that the game can write only where it should (see the next section).
9. Generates the admin secret into `/etc/lesmudos.env`, readable by root only.
10. Installs and enables the `lesmudos` service.
11. Opens SSH and the game port in the firewall and closes everything else.
12. Installs the nightly backup.
13. Starts the game and checks that it answers.

It is safe to run again. Running it again is also how an update is applied (section 7).

### Settings

Set any of these before running the script, for example `MUDLIB_WRITABLE=all bash setup-droplet.sh`.

| Setting | Default | Meaning |
|---|---|---|
| `GAME_PORT` | `4000` | The port players connect to |
| `MUDLIB_WRITABLE` | `wiz` | `wiz`: the running game can write only to wizards' homes. `all`: it can write to the whole world. |
| `BRANCH` | `main` | Which branch to run |
| `REPO_URL` | the public repository | Use your fork's address if you have one |
| `SWAP_MB` | `1024` | Swap file size; `0` for none |
| `BACKUP_KEEP_DAYS` | `14` | How long nightly backups are kept |

### Which should `MUDLIB_WRITABLE` be?

- **`wiz` (default).** Wizards can create and edit files in their own home directories from inside the game, and test them in their workshop (docs/SANDBOX.md). Nobody can change the live world from inside the game: `edit`, `copy`, `delete` and the rest fail outside `/wiz`, and approving a publication fails cleanly (the register reports the refused write and leaves nothing behind). The world changes only when you update from the repository.
- **`all`.** The game may write the whole mudlib, as its own rules allow: level 10 wizards may edit the live world, and approving a wizard's work (docs/PUBLISHING.md) publishes it under `/domains`. Level 5 to 9 wizards cannot write outside `/wiz` whatever this setting is. Changes made this way exist only on the droplet until someone copies them back into the repository: an update replaces the mudlib with the repository's version (`rsync --delete`), which removes published areas that are not in the repository. Copy `/opt/lesmudos/mudlib/domains/<name>` into the repository (and the publication register, `save/secure/publish.json`, is kept with the saves) before updating.

The driver needs no setting for the sandbox: it is the sample mudlib's policy, always on. There is no test-only switch to turn it off.

### Idle players

Two server settings of the driver (not of the setup script), given as `--idle-minutes N` / `--timeout-minutes N` on its command line or, taking precedence, as environment variables, for example in the systemd unit:

| Setting | Environment | Default | Meaning |
|---|---|---|---|
| `--idle-minutes` | `LESMUDOS_IDLE_MINUTES` | `5` | Minutes without typed input until a player is marked idle in `who` and earns no experience |
| `--timeout-minutes` | `LESMUDOS_TIMEOUT_MINUTES` | `10` | Minutes without typed input until the player is saved and disconnected, after a warning two minutes before (half the time, if shorter). Wizards are never disconnected |

A value is minutes (`0.5` is allowed), or a number with `s` or `m` (`30s`); the seconds form is for tests. Empty lines and telnet keep-alives do not count as activity. A timeout shorter than the idle time is raised to it.

### Wizard accounts

Give a wizard account (level 1 and up) only to someone you trust with the machine. A wizard's code runs inside the driver process; the sandbox (SANDBOX.md) keeps it out of the game's state, and the limits below keep a mistake from taking the game down for long, but no in-process sandbox can hold a determined stranger (memory, in particular, cannot be bounded per wizard). That is the owner's decision after three independent reviews.

### A wizard's code that stalls the game

| Setting | Environment | Default | Meaning |
|---|---|---|---|
| `--sandbox-stall` | `LESMUDOS_SANDBOX_STALL` | `5` | Seconds one step of the game may run with a wizard's sandbox code on the stack before the watchdog stops the driver |

When it fires (SANDBOX.md section 7, "A stall"), the log says `sandbox: STALL: code of wiz:<name> ...`, the driver exits with code 3 and systemd starts it again. The wizard's home is closed: `save/sandbox_quarantine.txt` holds one line per closed home (`wiz:<name> <time> ...`), and the driver makes no object of a listed home. To reopen it, read the code that stalled, delete its line, and restart the service.

### A wizard's home

| Environment | Default | Meaning |
|---|---|---|
| `LESMUDOS_HOME_BYTES` | `3000000` | Bytes a home (`/wiz/<name>`) may hold |
| `LESMUDOS_HOME_FILES` | `1000` | Files a home may hold |
| `LESMUDOS_HOME_DIRS` | `64` | Directories a home may hold |
| `LESMUDOS_HOME_DEPTH` | `8` | How deep a home's directories may go |

### Network limits, the bridge port and the login throttle

Settings of the driver, all environment variables (put them in `/etc/lesmudos.env`; `setup-droplet.sh` adds a commented block with these defaults to a new or an existing file, and an install that never edits it runs on the defaults below). A bad value is ignored with a note in the log; `0` switches a count or rate off. Design: [specs/web-client.md](specs/web-client.md) section 4 and [specs/login-throttle.md](specs/login-throttle.md).

| Environment | Default | Meaning |
|---|---|---|
| `LESMUDOS_BRIDGE_PORT` (`--bridge-port`) | `0` (off) | `setup-droplet.sh` writes `--bridge-port $BRIDGE_PORT` into the unit (`BRIDGE_PORT=4001 bash setup-droplet.sh` turns it on; `update-droplet.sh` keeps the value). The second listener for the website's browser client: **127.0.0.1 only**, and each connection must begin with a PROXY line giving the visitor's address. Leave it off until the website side is deployed; then `4001`. |
| `LESMUDOS_BRIDGE_BIND` | `127.0.0.1` | Only `127.0.0.1` or `::1` are accepted. Anything else is refused: the driver logs `bridge listener NOT started` and runs without it. There is no way to make it listen on a public address. |
| `LESMUDOS_BRIDGE_HANDSHAKE_SECONDS` | `5` | How long the PROXY line may take (it is at most 107 bytes). |
| `LESMUDOS_MAX_CONNECTIONS` (`--max-connections`) | `200` | All connections together. A newcomer gets "The game is full." |
| `LESMUDOS_MAX_PER_ADDRESS` (`--max-per-address`) | `10` | Per address; an IPv6 address counts as its /64; a web player counts under the visitor's real address. Loopback is **not** exempt. |
| `LESMUDOS_CONNECTS_PER_MINUTE` | `20` | New connections per address per minute (refused attempts count). |
| `LESMUDOS_MAX_LINES_PER_SECOND` | `20` | Typed lines per second per connection, sustained; `LESMUDOS_INPUT_BURST` (`60`) may arrive at once. The surplus is dropped with one warning; `LESMUDOS_INPUT_OVERFLOW_SECONDS` (`5`) of continuous overflow ends the connection. |
| `LESMUDOS_INPUT_BYTES_PER_SECOND` | `65536` | The same for bytes (burst: four seconds' worth). |
| `LESMUDOS_INPUT_STATS` | off | `1` logs each connection's peak input rate when it ends (to measure; not for production). |
| `LESMUDOS_THROTTLE_*` | see [login-throttle.md](specs/login-throttle.md) | Wrong-password locks per name (5 in 15 minutes: 5 minutes, doubling to 1 hour) and per address (20 in 15 minutes: 15 minutes, doubling to 4 hours), the 1-second pause on every "no", 3 new characters per address per hour and 10 per day. In memory only. |

Refused connections are sent one plain line and closed, and logged as `connection refused: <reason>: <address>`; these log lines, and the input-limit and throttle lines, are rate-limited so that a flood cannot fill the journal.

**The bridge port must never be exposed.** It is bound to 127.0.0.1, so the firewall is a second lock, not the first. `ufw` must not list 4001 (`ufw status` shows only SSH, the game port, 80 and 443), a DigitalOcean Cloud Firewall must not allow it, and `ss -ltnp | grep 4001` must show `127.0.0.1:4001`, never `0.0.0.0` or `[::]`. The port believes any address a PROXY line gives it, so anything that can reach it can pretend to be any visitor and get past the per-address limits and the login throttle. The public port 4000 never reads a PROXY line.

The output cap of the spec (section 4.4) is not part of this change: the sandbox branch supplies it (a connection with more than 4,000,000 bytes queued is closed, logged as "closed by the game (outbox limit)").

## 4. Make yourself the first wizard

1. Connect: `telnet <droplet ip> 4000` (or any MUD client).
2. Create your character and choose a calling.
3. Type `promote <the secret the script printed>`. You are now a level 10 wizard. This works only while no level 10 wizard exists.
4. Remove the secret from the droplet, since it has done its job:

   ```bash
   rm /etc/lesmudos.env && systemctl restart lesmudos
   ```

   Do this before you invite anyone: restarting from outside drops everyone connected without saving (section 7).

If you lose the printed secret before using it, it is in `/etc/lesmudos.env`.

## 5. Point the domain at it

At the registrar or DNS host for `legendsofhollowmere.com`, add:

| Type | Name | Value |
|---|---|---|
| A | `@` | the droplet's IP address |
| A | `play` | the droplet's IP address (optional: `play.legendsofhollowmere.com`) |

Players then connect with `telnet play.legendsofhollowmere.com 4000`. The game needs its own name, `play.legendsofhollowmere.com`, as an A record pointing straight at the droplet ("DNS only"): if the main name goes through a web proxy such as Cloudflare, the proxy passes web traffic only and port 4000 never arrives. If the domain's DNS is hosted at DigitalOcean: `doctl compute domain records create legendsofhollowmere.com --record-type A --record-name @ --record-data <ip>`.

To keep the address when you rebuild or replace the droplet, assign a Reserved IP to it in the control panel (Networking, Reserved IPs) and point the domain at that.

The connection is plain telnet and is not encrypted, passwords included, as with most MUDs. Encrypted connections are not built yet.

## 6. Day to day

| To | Run |
|---|---|
| See whether it is up | `systemctl status lesmudos` |
| Follow the log | `journalctl -u lesmudos -f` |
| See the last 100 lines | `journalctl -u lesmudos -n 100` |
| Start it | `systemctl start lesmudos` |
| Stop it properly | in the game, as a level 10 wizard: `shutdown` |
| Take a backup now | `/usr/local/sbin/lesmudos-backup` |
| See memory in the game | `mudstat` (wizards) |

**Stop the game from inside it.** The in-game `shutdown` command saves every character and moves what is lying in corpses to lost property. `systemctl stop` and `systemctl restart` do neither: they end the process at once, and anything not yet saved is lost. After an in-game `shutdown` the game stays down until you start it; after a crash it restarts by itself in five seconds.

## 7. Updating to a new version

1. Tell players, then in the game type `shutdown`.
2. On the droplet:

   ```bash
   bash /opt/lesmudos/src/deploy/update-droplet.sh
   ```

It takes a backup, fetches the latest commit, rebuilds, installs the world and starts the game. It refuses to run while the game is up, so that step 1 is not skipped by accident; `--force` overrides that, at the cost described above.

Wizards' homes and everything in `save` are kept. With `MUDLIB_WRITABLE=all`, files that wizards changed in the live world outside their homes are replaced by the repository's versions: copy such work into the repository first.

## 8. Backups and restoring

Every night at 04:17 the droplet writes `/var/backups/lesmudos/lesmudos-YYYY-MM-DD.tar.gz`, holding the `save` directory and the wizards' homes, and deletes archives older than 14 days. The world itself is in the repository and is not backed up separately.

These backups are on the same droplet. If the droplet is destroyed they go with it, so also do one of these:

- turn on DigitalOcean Backups for the droplet (weekly images), or
- copy the archives somewhere else from your own machine now and then:

  ```bash
  scp root@<droplet ip>:/var/backups/lesmudos/lesmudos-$(date +%Y-%m-%d).tar.gz .
  ```

To restore:

```bash
systemctl stop lesmudos
cd /opt/lesmudos
tar -xzf /var/backups/lesmudos/lesmudos-2026-11-18.tar.gz      # restores save/ and mudlib/wiz/
chown -R lesmudos:lesmudos save mudlib/wiz
systemctl start lesmudos
```

## 9. Security notes

- The game runs as a user that cannot log in, and systemd lets it write only to the save directory and the writable part of the mudlib. Wizards write C# that runs inside the driver; the driver's sandbox limits what that code can do, but it is not a security boundary, which is why the process itself is confined.
- Only SSH and the game port are open. `fail2ban` blocks addresses that keep failing SSH logins.
- SSH is by key only if you created the droplet that way. Check `PasswordAuthentication no` in `/etc/ssh/sshd_config.d/` if you are unsure.
- Security updates install themselves. A kernel update needs a reboot now and then: `shutdown` in the game first, then `reboot`; the game starts again by itself.
- The admin secret is only for making the first level 10 wizard. Remove it once that is done.

## 10. If something goes wrong

| Symptom | Look at |
|---|---|
| The script ends with "The game did NOT answer" | `journalctl -u lesmudos -n 50`. A line beginning `FATAL` or `Mudlib directory ... not found` says why. |
| The build fails or is killed | Memory. Check `free -m`; make sure the swap file exists (`swapon --show`); use a 2 GB droplet. |
| Players cannot connect but `systemctl status` says running | The firewall: `ufw status` should list the game port. Also any DigitalOcean Cloud Firewall attached to the droplet. |
| `promote <secret>` says no | A level 10 wizard already exists, or the service was started without the secret: check `/etc/lesmudos.env` exists and restart. Five wrong guesses also disable it until a restart. |
| In-game `edit` or `mkdir` fails outside a wizard's home | That is `MUDLIB_WRITABLE=wiz` working as intended (section 3). |
| The game restarts over and over | It is crashing at start: read the log. `systemctl stop lesmudos` to stop the loop while you look. |

## 11. The browser client ("Play in your browser")

Players can also play from a web page, on a phone or a computer: the page at `https://legendsofhollowmere.com/play`. It is built into the website (`lesmudos-web`), which opens one ordinary telnet connection to the game for each web player (the "bridge"). The design, every setting and the test plan are in [specs/web-client.md](specs/web-client.md); the website's own settings are in [WEBSITE.md](WEBSITE.md).

**It is switched off until you switch it on.** Deploying the code changes nothing: `Site__Play__Enabled` is `false`, `/play` says the client is switched off, and "Play Now" still leads to the instructions. Telnet on the game port is never touched.

### Switching it on, step by step

The order matters: the game first (it gets the second, loopback-only listener), then the website.

1. **Update the game.** `shutdown` in the game, then `bash /opt/lesmudos/src/deploy/update-droplet.sh`. This rewrites the game's service so it starts with `--bridge-port 4001` (`BRIDGE_PORT=4001`, a setting of `setup-droplet.sh`; the driver reads `--bridge-port` or `LESMUDOS_BRIDGE_PORT`). That listener is bound to `127.0.0.1` only, requires a PROXY version 1 line as its first bytes, and is **not** opened in `ufw`: do not open it. Check: `journalctl -u lesmudos -n 20` shows it listening, and `ss -ltn | grep 4001` shows `127.0.0.1:4001`, never `0.0.0.0` or `*`.
2. **Update the website.** `UPDATE_SOURCE=yes WEB_DOMAIN=legendsofhollowmere.com bash /opt/lesmudos/src/deploy/setup-website.sh`. It publishes the site, appends the new `Site__Play__...` lines to `/etc/lesmudos-web.env` (an existing file is never overwritten), fetches Cloudflare's address ranges into `/etc/caddy/cloudflare-ranges.txt`, rewrites the Caddyfile (the client's true address, WebSocket pass-through) and restarts the site. It prints `caddy version`: the Caddyfile needs Caddy 2.7 or newer (`apt-get install --only-upgrade caddy` if it is older; `caddy validate` fails otherwise and the script stops). **This restarts the website, which disconnects any web player** (there are none yet while the client is off).
3. **Check Cloudflare** (dashboard for the domain). All of these are in the spec, section 10.5:
   - Network: **WebSockets on** (the default). SSL/TLS mode **Full (strict)**.
   - Caching: a Cache Rule "bypass cache" for paths starting with `/play/ws` and `/play/status`; do not "Cache Everything" on `/play`.
   - **Rocket Loader, Auto Minify, Mirage, Email Obfuscation and Web Analytics auto-injection off** for `/play` (they rewrite or inject scripts; the page's policy would block the injected beacon and the console would fill with errors).
   - Security: **Bot Fight Mode / Super Bot Fight Mode and any WAF challenge rule** must not challenge `/play/ws` and `/play/status` (a WebSocket handshake cannot solve a challenge): add a skip rule for those two paths, then test from a phone on mobile data. Optional extra layer: a rate-limiting rule on `/play/ws` (for example 30 requests a minute per address).
   - The telnet name `play.legendsofhollowmere.com` stays a **DNS-only** (grey cloud) record. The web client never uses that name.
4. **Switch it on.** In `/etc/lesmudos-web.env` change `Site__Play__Enabled=false` to `Site__Play__Enabled=true`, then `systemctl restart lesmudos-web`.
5. **Check it works.**
   - `curl -s https://legendsofhollowmere.com/play/status` answers `{"v":1,"enabled":true,"full":false}`.
   - Open `https://legendsofhollowmere.com/play` in a browser, press **Enter Hollowmere**, make or enter a character. Then compare the address in `journalctl -u lesmudos-web | grep 'play: open'` with the "what is my IP" page: it must be **your** address, not a Cloudflare one (if it is a Cloudflare address, the Caddy ranges are wrong or stale: re-run `setup-website.sh`, and keep the client off meanwhile). The game's log (`journalctl -u lesmudos`) shows the same address on its `connection from ... (web)` line.
   - Leave the page open without typing: after 5 minutes the game warns you of the idle time and after 10 it disconnects you, even though the page keeps pinging (the pings are not input).
   - Work through the manual checklist, [specs/web-client.md](specs/web-client.md) section 12.3, on a phone and a desktop before announcing it.
6. Tell nobody yet that the site can be told apart from telnet: players type their name and password in the page each time (it does not store them), and the page warns that Cloudflare carries the password.

### Switching it off, and back on, without a restart

- Off for new connections and new page views: `touch /var/lib/lesmudos-web/play.off` (looked at every 5 seconds; sessions already open continue). On again: `rm /var/lib/lesmudos-web/play.off`.
- Off for good (and for everyone): `Site__Play__Enabled=false` in `/etc/lesmudos-web.env` and `systemctl restart lesmudos-web`.

### Updating later

- A change to the website's code: run step 2 again. It restarts the site and **every web player must sign in again** (the page says so and reconnects by itself). Do it when few are online (`who` in the game). A news post is a Markdown file and needs no restart.
- A change to the game: the usual `shutdown` and `update-droplet.sh`. Web players see "The game closed your connection" with a Reconnect button, then "The game is not running" until it is back.
- If website deploys disturb players, the bridge can run as a second copy of the site on its own port with its own service (spec section 2.1, option c): no code change.

### Limits and what to watch

The limits are settings of the website (`Site__Play__MaxConnections=60` web players at once, 5 per address, connect and input rates; the full list is in WEBSITE.md). The page says "The game is full right now" at the cap. To find the real memory cost of a web player before raising the cap, run `python3 tests/web_client_test.py --load 20` on a test machine (or read `systemctl status lesmudos lesmudos-web` with 0, 20 and 60 players) and write the numbers into the spec's capacity section. Web connection addresses are written to the system journal (`journalctl`), never anything typed: say so in your privacy text and decide how long `journald` keeps them (`MaxRetentionSec` in `/etc/systemd/journald.conf`).

## 12. Not done yet

- **A clean stop from outside.** The driver does not yet treat a stop signal as a planned shutdown. Until it does, always use the in-game `shutdown`.
- **Encrypted connections** (TLS) on the telnet port itself. (The browser client is HTTPS from the page to Cloudflare and to Caddy; see section 11.)
- **Copying live-world edits back to the repository** automatically, for `MUDLIB_WRITABLE=all`.
- **Off-droplet backups** on a schedule.
