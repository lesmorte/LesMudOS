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

## 11. Not done yet

- **A clean stop from outside.** The driver does not yet treat a stop signal as a planned shutdown. Until it does, always use the in-game `shutdown`.
- **Encrypted connections** (TLS) and a web client.
- **Copying live-world edits back to the repository** automatically, for `MUDLIB_WRITABLE=all`.
- **Off-droplet backups** on a schedule.
