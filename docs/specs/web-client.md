# The browser client ("Play now")

Owner's request (November 2026): a way to play Legends of Hollowmere in a web browser, on a phone as well as on a desktop. Agreed direction (the owner's adviser recommended it, the owner asked for this spec): a web client, yes; no native phone app (instead the web client must work well on a phone and be installable to the home screen); no custom desktop client (Mudlet covers that, see [../MUDLET.md](../MUDLET.md)).

This spec is written to be built from without questions. Every name of a file, setting, command and GMCP field below was checked against the code in this repository (the checks are listed in section 1); where the code leaves something unknown, it says "unknown" and does not guess. Related: [gmcp.md](gmcp.md) (the data the client uses), [website.md](website.md) and [../WEBSITE.md](../WEBSITE.md) (the site it lives in), [../DEPLOY.md](../DEPLOY.md), [play-feel.md](play-feel.md) (prompt, idle and timeout), [score.md](score.md) (the 48-column score box), [light.md](light.md) (a later `light` field).

## Words used in this document

- **WebSocket**: a lasting two-way connection that a web page can open to a server, over the same port and the same HTTPS as the page itself. Either side can send at any moment. It is what makes a live game in a browser possible.
- **Telnet**: the game's own plain-text protocol on port 4000. A browser cannot open one (it can only speak HTTP and WebSocket), so something must translate.
- **Bridge**: the translator. It accepts the WebSocket from the page and opens an ordinary telnet connection to the game for that player, then copies text both ways.
- **GMCP**: the side channel the game already speaks (telnet option 201): small JSON messages (`Room.Info`, `Char.Vitals`, ...) beside the text. See [gmcp.md](gmcp.md).
- **ANSI colour / SGR**: the way the game marks colour: the byte `ESC` then `[`, numbers, and `m` (for example `ESC[1;96m` for a bold bright-cyan command word). "SGR" is that family of codes (set graphics rendition).
- **Origin**: the header a browser attaches to a WebSocket request naming the site the page came from. A server can refuse sockets from pages on other sites.
- **CSP (Content-Security-Policy)**: a header that tells the browser which scripts, styles and connections a page may use; anything else is blocked even if an attacker injects it.
- **Service worker / web app manifest**: two small files that let a browser install a site to the phone's home screen (the manifest: name, icons, colours) and open it without the browser's address bar; the service worker can keep a copy of the page shell for offline start.
- **PROXY header**: one text line, `PROXY TCP4 <client address> ...`, that a relay writes first on a new connection to tell the server who the real client is (the "PROXY protocol, version 1", an open convention used by many servers).
- **NAWS**: the telnet option by which a client tells the server its window width in characters. The game does not use it today.
- **Link-dead**: a character whose connection dropped without `quit`. Today its body is saved and removed at once; a feature coming to main keeps the body in the room for 60 seconds so a reconnect resumes it.
- **Keepalive**: a tiny message sent only to keep a connection from being closed for silence.

## Decisions for the owner

Each has a recommendation (the one this spec is written for) and the alternative. Change any of them and the named sections change with it.

1. **Transport: a bridge inside the website's own ASP.NET process** (section 2.1: the page opens a WebSocket to `wss://legendsofhollowmere.com/play/ws`; the website opens a telnet connection to the game for each player). Alternative: a WebSocket listener inside the game driver itself; or the same bridge as a separate small service. The recommended code is written so it can be run as a separate service later with no code change (only a second systemd unit), which removes its one weakness (a website restart drops web players).
2. **Where the client lives: the client is `/play`; the existing "How to play" instructions page moves to `/how-to-play`** (the nav's "Play Now" and the hero's "Play Now" then open the game, which is what they promise). Alternative: keep `/play` as the instructions and put the client at `/play/online`, which changes no existing link but makes "Play Now" lead to a page that is not the game. (Today `/play` is the instructions page: `web/LesMudOS.Web/Pages/Play.cshtml`.)
3. **Wire format: every WebSocket frame is a small JSON object in a text frame, and the bridge, not the browser, parses telnet; colour is sent through as the game's own ANSI codes and drawn by the page** (section 2.5, 2.8). Alternative: raw telnet bytes in binary frames with the telnet and ANSI parsing in JavaScript (less C#, but a second telnet parser to get right in the one place that handles hostile input), or colour converted to structured spans by the bridge (more C#, no parser in the page, but the page then cannot cope with text from anywhere else).
4. **Real client address: the game opens a second listener bound to `127.0.0.1` only (default port 4001) that requires a PROXY version 1 line first; the public port 4000 never interprets such a line** (section 2.6, 4.1). Alternative: accept the line on port 4000 but only from loopback peers (one listener, but a first-bytes sniff on every local connection and a 1 second wait), or a trusted HTTP-style header (the driver does not speak HTTP).
5. **The driver defends itself too: a total connection cap, a per-address cap, a cap on queued output, and an input rate limit, all in the driver, for telnet and web alike** (section 4.2 to 4.4). Alternative: limits in the bridge only (smaller driver change, but raw telnet on port 4000 keeps having no limits at all, which is what the code does today).
6. **Client technology: vanilla JavaScript and CSS served from `wwwroot`, no framework, no build step, two script files** (section 5.1). Alternative: Blazor (WebAssembly or Server). Why not: WebAssembly ships a multi-megabyte runtime to a phone, Blazor Server would keep every keystroke and every game line on a server circuit (a second persistent connection per player, and a UI that dies with the connection), and in both a hostile line of game text would pass through a rendering framework that is much harder to audit than 40 lines that call `textContent`.
7. **"Stay signed in": no in the first version; the player types the name and password every time they connect** (section 8.5). Alternative: an opt-in "keep me signed in while this tab is open" that holds the password in the page's memory only (never in storage) and retypes it after a dropped connection. It is the thing phone players will want most (a locked screen drops the connection), which is why it is listed as the first follow-up; a safe token-based resume needs a driver feature and is out of scope.
8. **What the browser stores: display settings, command buttons and the map; the command history and the remembered character name are OFF by default behind two switches** (section 3.8). Alternative: history and name on by default (friendlier, but a shared computer then keeps the last tells a player typed).
9. **Links in game text: not clickable** (section 3.5). Alternative: only `http://` and `https://` addresses become links, with `rel="noopener noreferrer nofollow"`, opening in a new tab after a "you are leaving the game" confirmation. Why not by default: any player can put any address in a `say`, `title` or channel line, and a phone player taps without reading.
10. **Phone width: a font size chosen so at least 48 columns always fit, long lines wrap by CSS, and no change to the game; asking the game for a narrower layout (NAWS) is a later, optional step** (section 6.4). Alternative: do the NAWS change now so the score sheet and column lists re-wrap to the phone's width. Finding that makes this cheap: only `/secure/scoresheet` wraps at all (at 80); room descriptions and everything else are one long line the browser wraps by itself (section 1).
11. **Capacity at launch: 60 web players at once** (section 11), a setting, not a limit of the code. Alternative: lower while the first weeks measure the real memory per player, or higher once measured.

## 1. What exists today (checked against the code)

These facts shape the design. Each was read in the named file.

| Fact | Where | Consequence |
|---|---|---|
| The game listens on port 4000 (dual-stack IPv6/IPv4). One `Connection` per client, a line reader over the telnet state machine, one unbounded output queue (`Channel.CreateUnbounded`). | `src/LesMudOS.Driver/Net/TelnetServer.cs` | There is **no output cap**: a client that never reads makes the driver's queue grow without limit. The "outbox cap" the brief mentions does not exist yet; section 4.4 adds it. |
| **No connection limit of any kind**: no total cap, no per-address cap, no rate limit on connects or on typed lines. | `TelnetServer.cs`, `Kernel.cs` (`OnConnect` adds to `_connections` without a check) | Raw telnet today is the weakest door. Section 3.9 compares it with the web path. |
| Line limit: 4096 bytes; bytes beyond are silently dropped; bytes below 32 (except tab) are dropped; backspace/DEL edit the line; CR and NUL ignored; `0xFF` is the telnet escape. | `Connection.ReadLinesAsync` | A typed ESC byte never reaches the game (tested in `tests/ansi_test.py`). One line per `\n`: a frame with a newline inside it would run two commands, so the bridge must refuse that (section 2.5). |
| The text is UTF-8; `\n` is sent as `\r\n`. | `Connection.Send` | The bridge decodes with an incremental UTF-8 decoder (a character can be split across two reads). |
| At connect the driver sends `IAC WILL 201` (GMCP) and the login text. A client answering `IAC DO 201` gets GMCP; then `Core.Supports.Set` selects packages (`Wants`); with none selected everything is sent. Incoming GMCP: 4096 bytes, token bucket 30 burst, 15 per second. | `Kernel.OnConnect`, `Net/Gmcp.cs` | The bridge speaks GMCP on the player's behalf with `Core.Hello` and `Core.Supports.Set ["Room 1","Char 1","Comm.Channel 1"]` (the same three the Mudlet package asks for). The browser never sends GMCP. |
| Password prompts: the login text is written, then `InputTo(handler, true)` sends `IAC WILL ECHO` (`FF FB 01`); when the line arrives the driver sends `IAC WONT ECHO` (`FF FC 01`) and then `\n`. The driver never echoes anything itself. | `Kernel.InputTo`, `Kernel.OnLine`, `mudlib/secure/login.cs` | The signal for "mask this input" is the standard telnet one, and it comes from the driver already. The page must **echo typed commands itself** (the game does not), and must not add a newline for a masked line (the driver sends it). |
| The prompt (`Prompt()` of the body) is sent after every line and after unprompted output, **with no trailing newline**, no `IAC GA`, no end-of-record. If the player is looking at a prompt when news arrives, the driver starts the news with `\n` (`AtPrompt`). A line typed within 300 ms of a prompt that followed unprompted output makes the next reply start with `\n` (`PromptCrossingMs`). | `Kernel.SendPrompt`, `Kernel.Receive`, `Kernel.OnLine` | The page treats the last, unfinished line of output as the prompt and appends the player's echoed command to it, which reproduces what a telnet terminal shows. The crossed-prompt rule already assumes a client that echoes. |
| Idle: `LastInputMs` is updated only by a **non-empty** line. An empty line (a client keep-alive) and telnet negotiation do not count. Idle after 5 minutes, disconnect after 10 (settings `--idle-minutes`, `--timeout-minutes`, `LESMUDOS_IDLE_MINUTES`, `LESMUDOS_TIMEOUT_MINUTES`); a warning two minutes before. | `Kernel.OnLine`, `DriverSettings.cs`, play-feel.md section 7 | A keepalive that never reaches the game cannot touch the idle timer. The bridge answers keepalives itself (section 2.7). |
| The driver has **no notion of terminal width**: no NAWS, no width setting. Only `/secure/scoresheet` wraps (`MaxColumns = 80`, `Wrap`, `Columns`); room descriptions (`LongDesc`) are single long lines; the score box is 48 columns. | `grep` over `src` and `mudlib`; `mudlib/secure/scoresheet.cs`; `docs/specs/score.md` | The browser wraps prose itself. Only the score sheet and columns lists are laid out by the game. |
| A login with a name that is online is asked for the password like any other; the right password **takes the body over**: the old connection is told ("Your character has been taken over from another connection.") and closed, the new one continues where the character stands (login-throttle.md, "Taking over"; built 27 November 2026, which replaced the old "`{name} is already here. Another name? `" refusal). | `mudlib/secure/login.cs` (`FindOnline`, `EnterGame`) | Two tabs cannot play one character at the same time (section 8.3): the second takes over from the first, which is closed. |
| After a dropped connection the body is saved and removed at once, even in a fight. | `Player.NetDead`; `docs/specs/play-feel.md` ("Where a login begins") | Until the 60-second link-dead feature ships, a dropped web connection is the same as `quit` in the middle of a fight. Section 8.2. |
| `RemoteAddress` is a string built at accept time and only used in log lines: `connection from {RemoteAddress}` and `connection ended: {RemoteAddress}, player ..., lasted ...`. `tests/return_test.py` parses `connection ended:`. No mudlib code can read the address. | `TelnetServer.cs`, `Kernel.OnDisconnect`; `grep` over `mudlib` | The true address can be added without exposing it to players. The log line formats must stay as they are. |
| `sync TOKEN` (letters and digits, 16 at most) answers `[sync TOKEN]`, taking no game time. | `mudlib/std/player.cs` (`CmdSync`) | Tests use it to find where a reply ends. The client does not need it. |
| `Room.Info`, `Char.Vitals`, `Char.Status`, `Comm.Channel.Text`: fields are in section 5.4. `Char.Vitals` carries only a `fighting` boolean: **no target name or target health**. There is no GMCP package for skills. `say` and `whisper` lines are **not** sent as `Comm.Channel.Text` (only `tell`, `shout` and channel lines are). | `mudlib/secure/gmcp.cs`, `comm.cs`, `channels.cs` | The status panel shows an "in a fight" badge, not a target bar. The chat pane cannot hold `say`. The command-button row cannot be filled from the character's skills; the player adds buttons. |
| Colour: the game's markup becomes SGR codes only for these: `0` reset, `1` bold, `4` underline, `31` to `37`, `91` to `97`, and the pair `1;96` for `{cmd}`. A style still open at a newline is reset before it. With `ansi off` (a saved per-character setting, default on) no escape byte is sent at all. Player-typed braces are doubled so a player cannot inject a tag; typed ESC bytes never arrive. | `mudlib/std/thing.cs` (`StyleCode`, `Markup`, `Esc`), `tests/ansi_test.py` | The client's parser needs only SGR; everything else is discarded. `{cmd}` words (exits, commands) are exactly the words worth making tappable. |
| The website: ASP.NET Core Razor Pages on .NET 10, Kestrel on `127.0.0.1:5080`, Caddy in front (`reverse_proxy`), systemd unit `lesmudos-web` with `MemoryMax=300M`, user `lesmudos-web`. Site-wide headers set in `Program.cs`: `Content-Security-Policy: default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; base-uri 'self'; form-action 'none'; frame-ancestors 'none'`, HTML cached 60 s. No cookies, no sessions, no data protection keys on disk. Settings: `SiteOptions` (`Site:` section, env `Site__X`, file `/etc/lesmudos-web.env`). | `web/LesMudOS.Web/Program.cs`, `SiteOptions.cs`, `deploy/setup-website.sh` | `default-src 'none'` means a WebSocket, a manifest and a worker are all **blocked** until the play page's policy adds `connect-src`, `manifest-src` and `worker-src` (section 3.6). |
| `/play` is today the "How to play" instructions page (telnet address, Mudlet note, steps for each platform, the "not encrypted" notice, the "Download Client (coming later)" button). It is linked from the nav, the footer ("How to Play"), the home page (`Play Now`, `Create Account` as `/play#first-login`, `Enter Hollowmere`), `World`, `Support`, the sitemap and one news post. Tests: `SiteTests.cs` (`Fixed` list, `Play_page_uses_the_configured_address_and_port`, the Mudlet-link test). | `Pages/Play.cshtml`, `SitePages.cs`, `Pages/Shared/_Layout.cshtml`, `Pages/Index.cshtml`, `Pages/World.cshtml`, `Pages/Support.cshtml`, `web/content/news/2026-11-18-the-gates-are-open.md` | Decision 2. Section 10.2 lists every link to change. |
| Reverse proxy is **Caddy** (not nginx). `setup-website.sh` runs `systemctl reload-or-restart caddy` every time it runs. The game's unit is written by `setup-droplet.sh` (also run by `update-droplet.sh`), `MemoryMax=768M`; `ufw` opens SSH, the game port, 80 and 443. | `deploy/setup-website.sh`, `deploy/setup-droplet.sh`, `deploy/update-droplet.sh` | Section 10.4 gives the exact additions. |
| Licensing: `src/`, `tests/`, `deploy/` are MIT (engine); `web/` is "the game", all rights reserved. | `LICENSING.md` | The driver changes are engine code; the bridge and client in `web/` are game code. A client meant for other LesMudOS games would belong in `clients/` instead (not planned). |

Things the code leaves **unknown** (the spec works whichever way they turn out, and says how):

- The exact text and behaviour of the 60-second link-dead resume (coming on main). Section 8.2 only depends on "a reconnect with the same name and password resumes the body".
- The default of the coming `charset ascii|utf8` setting. The client always decodes UTF-8 and works with either (section 5.9).
- Whether Cloudflare counts WebSocket ping frames as traffic for its 100-second idle limit. The design sends both ping frames and a small application message, so it does not depend on the answer (section 2.7).
- How much memory one logged-in player costs inside the game. The cap is a setting; section 11 says what to measure.
- Whether Android's and iOS's monospace fonts draw the box characters of the score sheet at an exact one-column width (manual test, section 12.3).

## 2. Transport

### 2.1 The three options

| | (a) WebSocket listener inside the driver | **(b) Bridge in the website process (recommended)** | (c) Separate small bridge service |
|---|---|---|---|
| How it works | The driver opens a second listener that speaks HTTP upgrade and WebSocket framing; `Connection` is abstracted over telnet and WebSocket. Caddy proxies `/play/ws` to it. | The website (Kestrel already has WebSocket support) accepts the socket and opens `127.0.0.1:4001` per player. | Same code as (b) in its own process and systemd unit, started on another loopback port; Caddy proxies `/play/ws` to it. |
| New code | About 500 to 700 lines in `src/`: the HTTP upgrade (SHA-1 accept key), framing, masking, fragmentation, ping/pong, close, Origin, limits. The driver must stay free of NuGet packages, so all of it is hand-written (`HttpListener` is in the box but is a poor fit for a game loop's accept path). | About 450 lines of C# in `web/`, using ASP.NET's tested WebSocket middleware; the driver gets about 120 lines (the loopback listener, section 4). | (b)'s code plus one unit file and one Caddy route. |
| Driver stays dependency-free and small | No: it grows an HTTP and WebSocket implementation, in `Net/TelnetServer.cs` and `Kernel.cs`, which other branches are editing. | Yes. | Yes. |
| Real client address | Caddy passes a header; the driver would need an HTTP header parser to read it. | The bridge reads it from Caddy (section 2.6) and hands it to the driver in a PROXY line. | Same as (b). |
| A website restart drops web players | No. | **Yes** (the one weakness; section 2.9 softens it). | No. |
| A game restart drops web players | Yes. | Yes. | Yes. |
| One more process on a 2 GB droplet | No. | No. | Yes (a small .NET process, roughly 40 to 60 MB; a measurement, not a promise). |
| Testing | Python tests against the driver; the framing is hand-written, so it needs the most tests. | .NET tests with the framework's `WebSocketClient` and a fake telnet server, plus one Python end-to-end test. | Same as (b). |

Recommendation: **(b)**. It is the least code in the place the owner knows best (C#, Razor Pages, `WebApplicationFactory` tests), it keeps the driver a plain, dependency-free program, and it needs no new process. The code goes in its own folder (`web/LesMudOS.Web/Play/`) and switches on by one setting (`Site:Play:Enabled`) so that option (c) is a deployment change only: run a second copy of the published site with `Site__Play__Enabled=true` on another port and a Caddy route for `/play/ws`, and the website itself never restarts for a game player. Do that if website deploys (news posts are files and need no restart, but code changes do) start to annoy players.

### 2.2 The path of a web player

```
Browser                    Cloudflare               Droplet
page  ──wss (TLS #1)──►  proxy  ──https (TLS #2)──►  Caddy :443
                                                       │ plain HTTP, 127.0.0.1 only
                                                       ▼
                                             Kestrel 127.0.0.1:5080  (lesmudos-web)
                                             /play/ws  = the bridge
                                                       │ plain telnet + one PROXY line, 127.0.0.1 only
                                                       ▼
                                             Game driver 127.0.0.1:4001  (lesmudos)
                                             (port 4000 stays the public telnet door)
```

- **TLS #1** (browser to Cloudflare) uses Cloudflare's certificate for `legendsofhollowmere.com`. **TLS #2** (Cloudflare to the droplet) uses the certificate Caddy fetched (Cloudflare's SSL/TLS mode must be **Full (strict)**). The last two hops are plaintext, but never leave the machine: both ends are bound to `127.0.0.1`.
- Everything a web player types, **including the password, is readable by Cloudflare** (it ends TLS #1 and starts TLS #2). That is the same exposure every site behind Cloudflare has, and better than raw telnet, which is readable by every router on the way. The page says so plainly (section 9.3).
- The public telnet port stays exactly as it is.

### 2.3 Endpoints (all new, all under `/play`)

| Request | What |
|---|---|
| `GET /play` | The client page (Razor page, section 5). |
| `GET /play/ws` | The WebSocket. Subprotocol `hollowmere.v1` is required; the bridge answers with it. |
| `GET /play/status` | `{"v":1,"enabled":true,"full":false}`, `Cache-Control: no-store`. The page reads it before showing the "Enter" button, so a full or switched-off site says so without opening a socket. It reveals no player count and no name. |
| `GET /play/manifest.webmanifest`, `GET /play/sw.js` | Static files from `wwwroot/play/` (sections 7.1, 7.2). |

The client opens a socket **only when the player presses "Enter Hollowmere"**, never on page load. (Search-engine renderers, link-preview fetchers and prefetching browsers would otherwise each open a login prompt on the game.) The page is `noindex`.

### 2.4 What the bridge does for one player

1. **Handshake checks** (before upgrading; any failure is an HTTP error and a log line, no socket): the Play switch is on and the kill-switch file does not exist; `Origin` is in `Site:Play:AllowedOrigins`; the subprotocol is `hollowmere.v1`; the client address header is valid (section 2.6); the per-address and global limits allow one more (section 3.2). Statuses: 404 (disabled), 403 (origin, subprotocol or address header), 429 (a rate or per-address limit). A browser script cannot read these codes, so for a **full** site (the global cap) the bridge instead upgrades, sends `{"t":"sys","code":"full",...}` and closes with 1013 (section 2.5), which the page can show.
2. **Connect to the game**: TCP to `Site:Play:GameBridgeHost:GameBridgePort` (default `127.0.0.1:4001`), 5 second timeout; on failure send `sys unavailable` and close 1013. The first bytes written are the PROXY line (section 2.6).
3. **Negotiate**: when the game's `IAC WILL 201` arrives, answer `IAC DO 201`, then send `Core.Hello {"client":"Hollowmere Web","version":"1"}` and `Core.Supports.Set ["Room 1","Char 1","Comm.Channel 1"]`. The game's `IAC WILL ECHO` is answered `IAC DO ECHO` (harmless: the driver ignores replies) and reported as a `mask` frame. Every other offer or request from the game (`WILL` or `DO` of any other option) is refused (`DONT` or `WONT`).
4. **Two pumps**, one per direction, ending together (when either side ends, the other is closed).
   - **Game to browser**: read bytes, run them through the telnet parser (section 2.8), turn the result into frames, put frames on a bounded queue (`MaxQueuedBytes`, section 3.4), a writer task sends them.
   - **Browser to game**: read frames (max 8192 bytes each), validate, and for `in` write `UTF-8(line) + "\r\n"` to the game.
5. **Ending.** If the game closes the telnet connection (the player typed `quit`, the idle timeout, a game shutdown), the bridge sends `{"t":"sys","code":"game_closed"}`, then closes the WebSocket with 1000. If the WebSocket closes, the bridge closes the telnet connection, which the driver sees as a dropped connection (`NetDead`). It logs one line (section 3.10).

### 2.5 Wire format

Every frame, both ways, is a WebSocket **text** frame holding one JSON object with a short type field `t`. Binary frames are refused (close 1003). The bridge encodes with `System.Text.Json`'s **default encoder** (which escapes `<`, `>`, `&`, `'` and `+` as `\uXXXX`, a second layer of defence if the text ever reaches a place it should not), never the relaxed one the driver uses for GMCP.

**Server to browser**

| Frame | Meaning |
|---|---|
| `{"t":"hello","v":1,"max_in":4096,"ping":25}` | First frame, sent when the game connection is up. `max_in` is the longest input line in bytes; `ping` is the seconds between the client's pings. |
| `{"t":"out","d":"You are in a clearing.\n"}` | Game text. `d` is UTF-8 text with `\n` line ends (the bridge turns `\r\n` into `\n`, removes NUL and every other control character except `\n`, tab and ESC, so SGR colour codes survive), at most 16 KiB per frame (a longer read is cut into several frames; the page's parser holds an unfinished escape sequence across frames). The last, unfinished line of output is **not** held back: the prompt arrives as `{"t":"out","d":"Alice (45/58) (106/108) (20/24) > "}`. |
| `{"t":"gmcp","p":"Room.Info","d":{"num":1234,"name":"Village Square",...}}` | A GMCP message: `p` the package name, `d` its JSON (the bridge parses it and re-writes it, so it is guaranteed to be well formed, at most depth 8 and 16 KiB, which is the driver's own limit). A message with no payload has no `d`. |
| `{"t":"mask","on":true}` / `{"t":"mask","on":false}` | The game sent `IAC WILL ECHO` / `IAC WONT ECHO`: the next typed line is secret (section 2.10). Order with `out` is exact: the prompt text `Password: ` comes in an `out` frame before the `mask` frame, and the page processes both in the same turn. |
| `{"t":"pong","n":17}` | Answer to the client's ping, echoing its number. |
| `{"t":"sys","code":"...","msg":"..."}` | A message from the bridge, not the game. `msg` is plain English for the player. Codes: `full`, `unavailable`, `disabled`, `game_closed`, `restart` (the website is restarting), `rate` (a typed line was dropped), `too_long`, `bad_frame`, `address` (too many connections from this address), `slow` (the page was not reading). |

**Browser to server**

| Frame | Meaning |
|---|---|
| `{"t":"in","d":"look"}` | One line the player typed. `d` is at most 4096 UTF-8 bytes (the driver's own line limit; the bridge refuses longer lines with `sys too_long` and sends nothing, rather than letting the driver silently cut them), and contains no `\r`, `\n` or NUL (refused with `sys bad_frame`: one frame must be one command). An empty `d` is allowed (an empty line is how a telnet player asks for a fresh prompt). The bridge writes it with `\r\n`. |
| `{"t":"ping","n":17}` | Keepalive (section 2.7). Never forwarded to the game. |
| `{"t":"size","cols":52,"rows":30}` | The visible size in characters. Accepted and remembered; sent on as telnet NAWS only if the game asks (section 6.4, a later step; today the driver never asks, so it is dropped). |

Anything else, or any frame that is not valid JSON of this shape, is `sys bad_frame`; three in a row close the socket with 1008. The bridge never forwards a browser-written GMCP message, a telnet byte or an arbitrary string to the game.

Examples, in order, of a first minute (`\n` is a real newline in JSON):

```
S→C {"t":"hello","v":1,"max_in":4096,"ping":25}
S→C {"t":"out","d":"\nWelcome to Legends of Hollowmere.\n\nBy what name are you known? "}
C→S {"t":"in","d":"alice"}
S→C {"t":"out","d":"Password: "}
S→C {"t":"mask","on":true}
C→S {"t":"in","d":"correct horse battery"}
S→C {"t":"mask","on":false}
S→C {"t":"out","d":"\n\n\u001b[1mVillage Square\u001b[0m\nA cobbled square...\nObvious exits: \u001b[1;96mnorth\u001b[0m, ...\n"}
S→C {"t":"gmcp","p":"Char.Status","d":{"name":"Alice","calling":"Scout","level":5,"experience":142,"next_level":300,"gold":18,"points_to_place":0}}
S→C {"t":"gmcp","p":"Room.Info","d":{"num":1,"name":"Village Square","area":"Vincent's Hollow","environment":"outdoors","exits":{"north":2,"lane":3},"doors":{}}}
S→C {"t":"out","d":"Alice (45/58) (106/108) (20/24) > "}
C→S {"t":"ping","n":1}
S→C {"t":"pong","n":1}
```

Why JSON frames and not telnet bytes in the socket: the page never sees a telnet command, an `IAC` or a subnegotiation, so the page's only input parser is the colour one; the telnet parser lives in C#, next to its tests, and is the same component that is tested with split and hostile input in section 12.1. The cost is a few bytes per frame, which does not matter at game-text volumes.

Close codes the bridge uses: 1000 (the game ended the session: the page does **not** reconnect on its own), 1001 (the page is going away), 1003 (binary frame), 1008 (policy: bad frames, rate abuse, `slow`), 1009 (frame too big), 1012 (the website is restarting: the page retries), 1013 (full or the game is unreachable: the page retries). The close reason string is the same plain English as the `sys` message.

### 2.6 The real client address

Chain and rule, end to end:

1. **Cloudflare** puts the visitor's address in `CF-Connecting-IP` and connects from one of its published ranges (https://www.cloudflare.com/ips-v4 and `ips-v6`).
2. **Caddy** treats those ranges as trusted proxies and names that header as the source of the client address. Only a connection that really comes from a Cloudflare range can set it; from any other peer Caddy ignores the header and uses the TCP peer's address. Then Caddy writes the result into **one header of our own**, replacing anything the client sent under that name: `X-Hollowmere-Client-Ip`. Exact configuration in section 10.4.
3. **Kestrel** listens on `127.0.0.1` only, so only local processes can reach it. The bridge accepts `X-Hollowmere-Client-Ip` only when the TCP peer is loopback (always true in production; checked anyway), takes exactly one valid IP address (no list, no port, no zone), and if the header is missing or invalid **refuses the connection** (403) when `RequireClientIpHeader` is true (the default; only Development sets it false). A request that reaches the origin directly, around Cloudflare, simply carries the address Caddy saw.
4. The bridge opens the telnet connection to **`127.0.0.1:4001`** and writes, as its first bytes, one PROXY version 1 line: `PROXY TCP4 203.0.113.7 127.0.0.1 0 4001\r\n` (IPv6: `PROXY TCP6 2001:db8::1 ::1 0 4001\r\n`; an IPv4-mapped address is written as IPv4). The source port is `0`: the real port is not known behind Cloudflare. At most 107 bytes, as the convention says.
5. The **driver's bridge listener** (section 4.1) requires that line, reads it one byte at a time (so no game bytes are ever swallowed), and stores the address on the `Connection` as `ClientIp`; from then on the driver's log lines, the connection limits and any future ban use `ClientIp`, not the loopback address.

**How this cannot be forged from the public telnet port.** The PROXY line is understood only on the second listener, which is bound to `127.0.0.1` (a remote machine cannot connect to it at all) and refuses any peer that is not loopback. Port 4000 never looks for such a line: a remote player who types `PROXY TCP4 ...` at the name prompt is just typing an invalid name. `ufw` does not open port 4001 and the line is never needed to be open anywhere else. The one party that can claim any address is a process already running on the droplet, which is inside the trust boundary already (it could also read the save directory if it were the game's user).

If Cloudflare's ranges in the Caddyfile go stale, the failure is the safe one: a real Cloudflare address is not trusted, every visitor then looks like that Cloudflare address, and the per-address limit refuses the surplus. It is never possible to claim someone else's address.

### 2.7 Keepalive and the Cloudflare idle timeout

Cloudflare closes a proxied WebSocket that carries nothing for 100 seconds. The game is often silent for longer than that (an idle prompt). So:

- **The bridge sends a WebSocket ping frame every `PingSeconds` (25)**, using Kestrel's `WebSocketOptions.KeepAliveInterval` and `KeepAliveTimeout` (the timeout option exists from .NET 9; confirm the name on the installed SDK, otherwise the same check is done with a timer in the bridge). Browsers answer ping frames by themselves, with no page code.
- **The page also sends `{"t":"ping","n":N}` every 25 seconds** if nothing has been received for 20 seconds, and the bridge answers `pong`. This is for the page, not for Cloudflare: browser code cannot see WebSocket ping frames, so without it the page cannot tell a dead network from a quiet game.
- **Neither is input.** Both are answered by the bridge itself and never written to the game, so the game's idle timer (`LastInputMs`, which only a non-empty line moves) is untouched: a player who walks away from an open tab is idle after 5 minutes and disconnected after 10, exactly like a telnet player. (Even an empty line, which a naive bridge might send as a keepalive, would not count as activity in the driver; the bridge does not rely on that.)
- **Dead-connection rule, both sides:** the bridge closes with 1001 if it has received no frame at all from the page for `ClientSilenceSeconds` (75); the page treats the socket as dead if nothing at all has arrived for 75 seconds and starts the reconnect procedure (section 8.1). The bridge's closing is what lets the game start the link-dead timer for a phone that vanished.

### 2.8 Telnet, UTF-8 and colour in the bridge and the page

**Bridge telnet parser** (`TelnetParser`, a pure class with no I/O so it can be tested byte by byte): states data, IAC, option (after WILL/WONT/DO/DONT), subnegotiation, subnegotiation-IAC; `IAC IAC` is a literal `0xFF` (never present in valid UTF-8, so in practice it is dropped); an incoming GMCP subnegotiation (`IAC SB 201 name SP json IAC SE`, with doubled `0xFF`) is limited to 17 KiB (a little over the driver's 16 KiB payload limit) and an oversized one is discarded whole; a split anywhere (even one byte at a time) gives the same result; text goes through `System.Text.Decoder` for UTF-8 so a character split across two reads is joined, and an invalid byte becomes U+FFFD. Output: a list of events (`Text`, `Gmcp(name, json)`, `Mask(bool)`).

**ANSI is sent through and drawn by the page.** The player's own `ansi` setting decides whether the game sends colour at all (`ansi off` means plain text, which the page then shows plain), so the page respects that choice for free. The page's colour parser (`parseAnsi`) is a small state machine over the text:

- `ESC [` parameters `m` is SGR. Handled: `0` and empty (reset), `1` (bold: font weight, **not** brighter colour), `4` (underline), `22`, `24`, `30` to `37` and `39` (foreground, 39 default), `40` to `47` and `49` (background), `90` to `97`, `100` to `107`. Any other number inside an SGR is ignored; `38;5;n` and `38;2;r;g;b` are skipped properly (their parameters consumed) and not drawn.
- Any other escape sequence (`ESC [ ... <letter>`, `ESC ]` ... up to BEL or `ESC \`, a lone `ESC x`) is **consumed and discarded**: the page never writes an `ESC` into the document and no sequence can move the cursor, clear the pane or set the window title.
- An unfinished sequence at the end of a frame is kept and continued by the next frame. A sequence longer than 64 characters is abandoned (and its characters dropped).
- The style resets at every `\n` (the game does this itself for its own markup).
- Output is grouped into runs of equal style: one `<span class="...">` per run, classes `fg31`, `fg91`, `bg41`, `b`, `u` and the like, never inline styles (the site's CSP forbids them, section 3.6).
- A run with exactly bold and foreground 96 (`1;96`, the game's `{cmd}`) also gets the class `cmd`, which makes it tappable (section 6.3).

### 2.9 Restarts

| Event | What the player sees | What happens in the game |
|---|---|---|
| The player closes the tab or loses the network | Nothing (page gone) or "Connection lost" | The bridge sees the close (or silence after 75 s) and drops the telnet connection; the game sees a dropped connection (`NetDead`; with the coming rule, the body stays in the room 60 seconds). |
| The website restarts (a deploy, `systemctl restart lesmudos-web`) | "The website is restarting. Reconnecting." then the login screen | On shutdown the bridge sends `sys restart` and closes every socket with 1012 within `ShutdownGraceSeconds` (5), which drops every telnet connection. Same as a dropped connection. The page reconnects with back-off. A deploy therefore costs every web player a retyped password: **do website deploys when few are online** (section 10.5), or move to option (c). |
| Caddy reloads (`systemctl reload caddy`, which `setup-website.sh` runs on every run) | Nothing, if Caddy keeps the sockets | Caddy by default may close upgraded connections on a reload; the Caddyfile in section 10.4 sets `stream_close_delay` so they stay up. Unverified on the installed Caddy version: test. |
| The game restarts (`shutdown`, then `update-droplet.sh`) | "The game closed your connection" with a **Reconnect** button; then, while the game is down, "The game is not running; trying again" with back-off | `shutdown` saves everyone and closes all connections ("closed by the game (shutdown)"). The bridge cannot connect to 4001 until the game is up: `sys unavailable`, close 1013. |
| A game crash | Same as a restart | The unit restarts the driver after 5 s (`Restart=on-failure`). |

### 2.10 The password prompt

The signal is the game's own telnet `IAC WILL ECHO`, parsed by the bridge into `{"t":"mask","on":true}`, and `IAC WONT ECHO` into `{"t":"mask","on":false}`. This is already exactly right for every masked prompt in `login.cs` (the password, the wrong-password retry, "Choose a password", "Type it again"), because each one calls `InputTo(handler, true)`. No driver change and no GMCP is needed. Rules for the page:

- On `mask on`: the input element becomes `type="password"` with `autocomplete="off"`, `autocapitalize="off"`, `autocorrect="off"`, `spellcheck="false"`, `enterkeyhint="go"`; the field is emptied of anything already typed; nothing typed is echoed to the output pane; the line is **not** added to the command history, not to "repeat last command", not to the screen-reader region, not to any storage; the page does not add a newline after it (the game sends one).
- On `mask off`, on socket close, and when the page is hidden, the field is cleared and returned to `type="text"`.
- The password is only ever in the input element until Enter, then in one `in` frame, then nowhere. The bridge never logs frames (section 3.10).
- If the `mask` frame were missing (a bug), the password would show as plain text for one line. Defence in depth: the input element is switched to password mode whenever the last unfinished line of output ends with `assword: ` or `again: ` (case-insensitive) **in addition to** the frame. This is a safety net only and never un-masks.
- Browsers' password managers are not engaged (`autocomplete="off"` on a field that has no username field next to it); that is acceptable for the first version.

## 3. Security

### 3.1 Origin check

The bridge compares the `Origin` header, exactly (scheme, host, port; host case-insensitive), with `Site:Play:AllowedOrigins`. Default when the list is empty: the origin of `Site:BaseUrl` (`https://legendsofhollowmere.com`). In the Development environment `http://localhost:5090` and `http://127.0.0.1:5090` are added. `https://www.legendsofhollowmere.com` is **not** added (Caddy redirects `www` to the bare name, so a page is never served from it). A missing `Origin` is refused (every browser sends one on a WebSocket; only scripts and scanners omit it); the test clients set it. A mismatch is a 403 and the log line names the (truncated, sanitised) value.

### 3.2 Limits

| Limit | Where | Value (setting) | When exceeded |
|---|---|---|---|
| Web players at once | bridge | 60 (`MaxConnections`) | upgrade, `sys full`, close 1013; `/play/status` says `full` |
| Web connections per address | bridge | 5 (`MaxPerAddress`; IPv6 counted by /64) | 429 before upgrade; log `refused address` |
| New web connections per address per minute | bridge | 10 (`ConnectsPerMinutePerAddress`, refused attempts count) | 429 |
| New web connections per minute, all addresses | bridge | 120 (`ConnectsPerMinute`) | 429 |
| Typed lines per second | bridge | 10 sustained, burst 20 (`InputLinesPerSecond`, `InputBurst`) | the line is dropped with `sys rate` (at most one notice per 5 s); 100 dropped lines in a row close with 1008 |
| Frame from browser | bridge | 8192 bytes (`MaxFrameBytes`) | close 1009 |
| Input line | bridge | 4096 UTF-8 bytes (`MaxInputBytes`), no CR, LF, NUL | `sys too_long` or `bad_frame`, nothing sent |
| Queued output per player | bridge | 262,144 bytes (`MaxQueuedBytes`) | `sys slow`, close 1008, the telnet side is closed so the game's queue is released |
| Silence from the page | bridge | 75 s (`ClientSilenceSeconds`) | close 1001 |
| Total game connections | driver | 200 (`--max-connections`, `LESMUDOS_MAX_CONNECTIONS`) | the driver writes "The game is full. Please try again in a few minutes." and closes |
| Game connections per address | driver | 10 (`--max-per-address`, `LESMUDOS_MAX_PER_ADDRESS`); counted on `ClientIp`, so web players count under their real address; a loopback peer with no header is exempt (tests, the admin on the droplet) | "Too many connections from your address." and close |
| Typed lines | driver | 20 per second sustained, burst 60 (`LESMUDOS_MAX_LINES_PER_SECOND`) | the surplus lines are **delayed, never dropped** (the reader waits for the next token; the rest stays in the socket buffer); 100 seconds (about 2,000 lines) of continuous backlog disconnects with "Too much input. Goodbye." |
| Queued output | driver | 1 MiB per connection (`LESMUDOS_MAX_QUEUED_BYTES`) | the connection is closed ("closed by the game (client too slow)") |
| Line and GMCP sizes | driver | unchanged: 4096-byte line, 4096-byte incoming GMCP, 16 KiB outgoing GMCP | as today |

The bridge limits are checked per web player; the driver limits are the last line, and the only ones that protect raw telnet. Section 3.9 compares the two doors.

### 3.3 Frame and line sizes

See the table: browser to bridge frames are at most 8 KiB (an input line is at most 4 KiB plus JSON), game to browser `out` frames at most 16 KiB of text, GMCP frames at most the driver's own 16 KiB payload. The page also limits what it keeps: scrollback (section 5.2) and each rendered line (at most 4096 characters per line element; longer lines are cut into continuation lines).

### 3.4 A slow or non-reading client

The page not reading (a frozen tab, a throttled background phone, a hostile script) fills the bridge's per-player queue. At 256 KiB the bridge sends `sys slow` if it can, closes the WebSocket with 1008, and closes the telnet connection, which frees the driver's queue. Because the bridge reads from the game as fast as it can into a bounded queue (rather than stopping its reads), the driver never accumulates more than the socket buffers for a web player; the driver's own 1 MiB cap (section 4.4) is the same protection for raw telnet.

### 3.5 Hostile text: everything from the game is untrusted

Players can put arbitrary text into `say`, `tell`, `shout`, channel lines, titles, names of things they name, and a wizard's descriptions. The game strips ESC and control characters from what players type and doubles their braces, but the page must not rely on any of that.

Rules for every script file of the page:

- Game text enters the document **only** through `textContent`, `Text` nodes or `document.createTextNode`, on elements created with `document.createElement`. The words `innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`, `eval`, `new Function`, `setTimeout("string")`, `srcdoc`, `javascript:` and `DOMParser` do not appear in `play*.js` (a test greps for them, section 12.1).
- Game text is never used as an element name, `id`, `class`, `href`, `src`, `style`, event handler, or CSS value. Class names come from a fixed table (colour numbers 30 to 107 map to fixed class names). The one attribute that carries game text is `data-cmd` on an exit button or tappable word, set with `dataset`, and it is only ever read back and sent as a typed command.
- **URLs are not made into links** (Decision 9): `http://evil.example` appears as plain text. The player can copy it. If the owner chooses the alternative, the rule is: match only `https?://` followed by characters in a conservative set, at most 200 characters, create the `<a>` from `createElement`, set `href` after parsing with `new URL()` and checking `protocol` is `http:` or `https:`, `rel="noopener noreferrer nofollow"`, `target="_blank"`, and show a confirmation naming the host first.
- A command sent from a tapped word or button is exactly what the player could have typed; the page adds nothing hidden (no suffix, no second command).
- Names from GMCP (`name`, `area`, exit keys, `talker`, `channel`) are shown with `textContent` and cut to their documented lengths (`Room.Info.name` 120, `area` 80, an exit key 32, `talker` 40, `channel` 32, `text` 600), whatever the message says.
- Numbers from GMCP are checked (`Number.isFinite`, clamped to 0 to 1e9) before they set a bar width; bar widths are set with `style.setProperty('--w', ...)` from CSSOM (permitted under the CSP, unlike an inline `style` attribute), clamped to 0 to 100 percent.
- Hostile cases the tests must cover: `<img src=x onerror=alert(1)>`, `</div><script>`, `"><svg onload=...>`, a line of 100,000 characters, 10,000 escape sequences in a row, `ESC]0;title BEL`, `ESC[2J`, `ESC[999999999D`, a lone `ESC`, invalid UTF-8, a surrogate pair split across frames, and a room name made of 120 `W` characters.

### 3.6 Content-Security-Policy and headers for the play page

The site-wide policy in `Program.cs` starts at `default-src 'none'`. For `/play` (and `/play/*`) the middleware adds, and the page needs, exactly:

```
default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self';
connect-src 'self' wss://legendsofhollowmere.com; manifest-src 'self'; worker-src 'self';
base-uri 'self'; form-action 'none'; frame-ancestors 'none'
```

- `connect-src 'self'` covers same-origin `wss:` in current browsers; the explicit `wss://` entry (built from `Site:BaseUrl`, `https` to `wss`) is for older Safari versions that do not treat `'self'` as including WebSocket. Nothing else may be contacted: no other site, no analytics.
- **No inline script** (`script-src 'self'`, no `'unsafe-inline'`, no nonce needed) and **no inline style** (`style-src 'self'`; no `style="..."` attributes in the markup, no `<style>` blocks; JavaScript sets styles through the CSSOM). All page configuration (the socket path, limits) is read from `data-*` attributes on the `<main>` element and from `/play/status`.
- `Cache-Control`: the page may keep the site's 60-second HTML default; `/play/status` and anything under `/play/ws` are `no-store`; `/play/sw.js` is `no-cache` and carries `Service-Worker-Allowed: /play`; hashed assets keep the site's existing one-year `?v=` rule.
- `Permissions-Policy` stays as it is (camera, microphone, geolocation off). Screen Wake Lock needs no permission policy entry. `Referrer-Policy` and `X-Content-Type-Options: nosniff` stay.
- `robots.txt` gains `Disallow: /play/ws` and `Disallow: /play/status`; the play page carries `<meta name="robots" content="noindex">` (the layout already supports `ViewData["NoIndex"]`) and is removed from the sitemap; `/how-to-play` takes its place there.

### 3.7 No third-party content, no secrets

The play page loads nothing from any other origin: no scripts, fonts, styles, images, analytics, tag managers, or CDN libraries. It uses the system monospace fonts only (no web font download on the play page: `font-family: ui-monospace, "SF Mono", "Cascadia Mono", Menlo, Consolas, "Liberation Mono", "DejaVu Sans Mono", monospace`). The page contains no secret and no API key: there is nothing to put in it. The page's HTML is the same for everyone and contains no game data and no player data. Nothing on the play page reveals anything beyond what the game sends to that player: the map and the panels are built only from that player's own `Room.Info`, `Char.*` and `Comm.*` messages.

### 3.8 What is stored in the browser

Only `localStorage` (never cookies; the site sets none) under keys that start with `hm.`:

| Key | Holds | Default |
|---|---|---|
| `hm.settings.v1` | Display settings (font size, theme, scrollback, announcement mode, tap action, wake-lock, numpad), the command-button list, the dismissed "how to play" strip flag. At most 16 KiB. | written on first change |
| `hm.name.v1` | The character name, **only if** the player ticks "Remember my character name on this device" | off, nothing stored |
| `hm.history.v1` | The last 100 commands, **only if** the player ticks "Remember my commands on this device" | off; in memory only, lost when the tab closes |
| `hm.map.v1.<name>` | The automap for one character (section 5.8) | stored once the map exists (stage 4); "Clear map" and "Forget everything" delete it |

- The **password is never stored**: not in storage, not in the history, not in a variable that outlives the single `in` frame, not in the log, not in the screen-reader region, not echoed. Input typed while masked is excluded from every code path that records anything.
- Everything is parsed defensively (`try/catch`, schema check, unknown keys dropped, wrong types replaced by defaults); a corrupt value resets to defaults and never stops the page starting.
- "Forget everything stored by this site on this device" (in the settings dialog) removes every `hm.` key and the service worker's cache.
- A visible line in the settings dialog says what is stored and that command history can contain private tells if "remember" is on.

### 3.9 Denial of service: the web path against raw telnet

| | Raw telnet, port 4000 (today) | Web path |
|---|---|---|
| Who can reach it | Anyone on the internet, directly, no proxy | Cloudflare absorbs floods of bad requests before the droplet; but the droplet's own port 443 is reachable directly too (see below) |
| Connection cap | none (before section 4) | bridge 60 total, 5 per address, 10 new per minute per address; then the driver's own caps |
| Cost per connection | a socket, a `Connection`, a login object, an unbounded output queue | the same inside the game, plus a WebSocket, two tasks and a 256 KiB-capped queue in the website |
| Input flood | unlimited lines per second reach the single game thread | 10 lines per second per player at the bridge, plus the driver's limit |
| Slow reader | queue grows without limit | queue capped at 256 KiB (bridge) and 1 MiB (driver) |
| Password guessing | 3 tries per connection, unlimited reconnects | the same 3 tries per connection; at most 10 new connections per minute per address through the bridge (so about 30 guesses a minute from one address) |
| Where an attacker hides | their own address | behind Cloudflare, but the true address is logged and counted |

A web player is never cheaper for an attacker than a telnet connection, and the bridge adds limits that raw telnet lacks; the driver changes in section 4 close the same gaps for telnet. An attacker who finds the droplet's address can still reach Caddy and port 4000 directly; closing that is optional hardening (allow ports 80 and 443 in `ufw` only from Cloudflare's ranges), not part of this spec because it complicates certificate renewal and the owner's own access. A login-attempt throttle (counting wrong passwords per address and delaying new connections) would help both doors and needs a driver feature: out of scope, listed in section 14.

### 3.10 Logging

One line per web connection when it opens and one when it closes, in the website's journal (`journalctl -u lesmudos-web`), using the true address and nothing typed:

```
play: open 203.0.113.7 origin=https://legendsofhollowmere.com web=14/60
play: close 203.0.113.7 lasted=312s lines_in=41 bytes_out=18432 reason=client-closed code=1001
play: refused 203.0.113.7 reason=origin|address-header|rate|address|full|disabled|subprotocol
play: dropped 203.0.113.7 reason=too-long|bad-frame|rate|slow
```

The game's journal gets its own lines, with the true address instead of 127.0.0.1: `connection from 203.0.113.7 (web)` and `connection ended: 203.0.113.7 (web), player Alice, lasted 312s, 4s since last input, client closed` (the existing format, so `tests/return_test.py` keeps working). **No typed input, no frame body, no game text, no password and no player command is ever logged** by either side (the existing driver rule, `Ended` "never anything a player typed", extended to the bridge). The `Origin` and `User-Agent` are logged only on a refusal and truncated to 100 characters with control characters removed. The addresses in these lines are personal data: the site's rules page (and any privacy text) should say that connection addresses are kept in the system journal (rotated by `journald`, by default a size cap, not a time cap; the owner decides whether to set `MaxRetentionSec`).

## 4. Driver changes

> **Built (stage 1, driver side, branch `work/net-limits`): 4.1, 4.2 and 4.3. Section 4.4 (the output cap) is NOT built here: the sandbox branch supplies it** (a connection whose queued output passes 4,000,000 bytes is closed, logged "closed by the game (outbox limit)"; the 1 MiB figure and `LESMUDOS_MAX_QUEUED_BYTES` below were not used). 4.5 stands: no keepalive feature was needed, an empty line is still not activity and the bridge never sends one.
>
> Where it lives: `Net/NetSettings.cs`, `Net/ProxyLine.cs`, `Net/BridgeListener.cs`, `Net/ConnectionLimits.cs` (the limits and a rate-limited log), `Net/LineBucket.cs`, `Net/ConnectionNet.cs` (the network half of `Connection`, which is now `partial`), `Net/NetPolicy.cs` (the entry points). `TelnetServer.cs` and `Program.cs` have one-line hooks only. One test file, `tests/net_test.py`, covers what the spec split into `bridge_listener_test.py` and `limits_test.py`. Settings and defaults: [../DEPLOY.md](../DEPLOY.md). The login throttle, which the spec only mentions in section 14, is built too: [login-throttle.md](login-throttle.md).
>
> **Deviations from the text below, and why.**
> 1. **No loopback exemption** (4.2 exempted a loopback peer on the public port). The owner asked for none in production; the test suites switch the caps off by environment instead (`tests/smoke_test.py`, which every suite imports). Consequence: the admin on the droplet and any local tool count against the per-address cap like anyone else.
> 2. **A connect-rate limit in the driver** (4.2 had it only in the bridge): 20 new connections per address per minute (`LESMUDOS_CONNECTS_PER_MINUTE`), refused attempts count. Refusal text: "Too many connection attempts from your address. Please wait a minute."
> 3. **The settings are environment variables**; only `--bridge-port`, `--max-connections` and `--max-per-address` also exist as flags (the environment wins). `LESMUDOS_MAX_LINES_PER_SECOND`, `LESMUDOS_INPUT_BURST`, `LESMUDOS_INPUT_BYTES_PER_SECOND`, `LESMUDOS_INPUT_OVERFLOW_SECONDS`, `LESMUDOS_BRIDGE_HANDSHAKE_SECONDS` and `LESMUDOS_BRIDGE_BIND` are new names. `LESMUDOS_BRIDGE_BIND` accepts only a loopback address; any other value stops the listener from starting (so a non-loopback bind is refusable and testable, and cannot be widened).
> 4. **Input limit**: besides the line bucket (20/s, burst 60) there is a byte bucket (65,536 bytes/s, burst four seconds' worth) so one very long line repeated cannot get round it; a chunk with no tokens is **delayed**, not dropped (changed at the merge, net-int: a wizard pasting a file into the in-game editor would otherwise lose lines silently and corrupt code; `tests/net_test.py` pastes 500 lines with the production limits on and reads the file back). The first delay of an overflow sends one line, "[Input is arriving faster than the game reads it. Nothing is lost: it is being read a little at a time.]"; a backlog that lasts `LESMUDOS_INPUT_OVERFLOW_SECONDS` (default 100 s, about 2,000 lines at 20 a second; the buckets not back to half full) sends "Too much input. Goodbye." and closes ("closed by the game (too much input)"). The backlog is not queued in the driver: the reader stops reading and the socket's own buffer holds the rest, so memory is bounded by TCP. Nothing typed is logged.
> 5. **Log lines** (all rate limited to 10 a minute per kind, with a "left out" note): `connection refused: <reason>: <address>`, `bridge connection refused: bad PROXY line (<reason>)`, `input limit: ignoring input from <address>` / `disconnecting <address>`. The bridge log line is `connection from 203.0.113.7 (web)`, the ended line `connection ended: 203.0.113.7 (web), player ...`; public connections are logged exactly as before (`127.0.0.1:51620`).
> 6. **PROXY line**: stricter than the convention's minimum: ASCII only, single spaces, CR LF (a bare LF is refused), no leading zeros, no abbreviated IPv4 (`1.2`), no zone id, and an unspecified, multicast or broadcast source is refused. The reader stops at the first wrong byte of `PROXY ` rather than waiting. At most 32 handshakes may be pending. An IPv4-mapped IPv6 source is counted as IPv4.
> 7. **The slot** in the connection limits is given back by a wrapper around `RunAsync` (`RunTrackedAsync`, in a `finally`), not inside `RunAsync`.
> 8. **Peak input rate of the test suites, measured** with `LESMUDOS_INPUT_STATS=<file>` (one line per connection: peak lines in any one second, total lines and bytes) over smoke, return, playfeel, gmcp, ansi, comm, class, area, rest and timing (165 connections in the first six): the busiest connection reached **at least 256 lines in one second** (the measuring ring holds 256, so it saturated) and several others 145 to 232; no connection sent more than about 13 KB in total. A test client sends its next command the moment the reply arrives, so it runs 10 to 15 times faster than the limit's 20 lines/s sustained. The suites therefore start the driver with `LESMUDOS_MAX_LINES_PER_SECOND=0` (off; set in `tests/smoke_test.py`); `tests/net_test.py` tests the limit itself, including that 50 pasted lines and a steady 10 per second pass untouched, and that 55 `sync` commands pasted at once all run. A human or a Mudlet speedwalk (a burst of at most 60 lines, then a few per second) is inside the limit.


Small, each behind a setting, each with its own test. `src/LesMudOS.Driver/Kernel.cs` and `Net/TelnetServer.cs` are being changed on other branches, so every hook is a call of one or two lines into **new files** under `src/LesMudOS.Driver/Net/` and `DriverSettings.cs`; the logic is not written in those two files.

### 4.1 The bridge listener and the PROXY line (new: `Net/BridgeListener.cs`, `Net/ProxyLine.cs`)

- Setting `--bridge-port N` / `LESMUDOS_BRIDGE_PORT` (environment wins, like the idle settings). **Default 0 = no second listener**; the deploy script sets 4001.
- A `TcpListener` on `IPAddress.Loopback` (127.0.0.1) only. Started where the public listener is started (`TelnetServer.Start`: one added line).
- For each accepted socket: if the peer address is not loopback, close it. Otherwise read the first line, **one byte at a time**, until `\n`, at most 107 bytes, within 5 seconds. The line must be `PROXY TCP4 <ipv4> <ipv4> <port> <port>\r\n` or `PROXY TCP6 <ipv6> <ipv6> <port> <port>\r\n` (addresses parsed with `IPAddress.TryParse`, address family matching the word, no zone id, ports 0 to 65535). Anything else (wrong word, `UNKNOWN`, too long, timeout, bad address): close with no reply and log `bridge connection refused: bad PROXY line`. A valid line: build the `Connection` as usual, with `ClientIp` set to the source address, `RemoteAddress` set to `"<address> (web)"`, and carry on exactly as for a public connection (the `Log.Info("connection from ...")` line is written after the header, with the true address).
- `Connection` gains `public IPAddress? ClientIp` (for a public connection: the peer address, IPv4-mapped unmapped, as `RemoteAddress` is built today) and a constructor path that accepts the pre-read header. That is the only change inside `TelnetServer.cs` beyond two or three call lines.
- Nothing in the mudlib can read `ClientIp` (there is no efun); a later ban list reads it in the driver.
- **Tests** (`tests/bridge_listener_test.py`): a loopback client with a valid TCP4 line plays a login and the log shows the claimed address; a valid TCP6 line; no line (waits, then is closed after 5 s with the log line); a line that is 108 bytes; `UNKNOWN`; an address that does not parse; the line split across several packets; game bytes sent right after the line in the same packet (not lost); the **public** port given the same line as its first input is treated as a login name (`PROXY TCP4 ...` fails the name rule and nothing changes in the log address); the listener refuses a non-loopback peer (unit-level, since the test host cannot dial from another address); with `--bridge-port 0` nothing listens on 4001.

### 4.2 Connection limits (new: `Net/ConnectionLimits.cs`)

- Settings `--max-connections` (`LESMUDOS_MAX_CONNECTIONS`, default 200) and `--max-per-address` (`LESMUDOS_MAX_PER_ADDRESS`, default 10), parsed in `DriverSettings` like the idle settings (a bad value is ignored with a log note; 0 means no limit).
- `ConnectionLimits.TryAdmit(Connection)` is called once per accepted connection **after** `ClientIp` is known (public: at accept; bridge: after the PROXY line). It counts active connections in total and per address (IPv6 by /64; a loopback `ClientIp` that came from the public listener, not from the bridge, is exempt from the per-address rule so tests and the droplet's own admin are not limited). Refusal: write the one-line text ("The game is full. Please try again in a few minutes.\n" or "Too many connections from your address.\n"), close, log `refused: <reason>: <address>`, and never call `Kernel.OnConnect` (so no login object is made). The matching release is in `Connection.RunAsync`'s end (`finally`), not in the kernel.
- **Tests** (`tests/limits_test.py`): the 11th connection from one address is refused with the text, the 10 others unaffected; a connection closing frees a slot; the total cap; loopback exemption; a web (bridge listener) connection with a claimed address counts under that address, not under 127.0.0.1; refused connections create no login object (object count unchanged); a flood of 500 connect/disconnect cycles leaves the count at zero.

### 4.3 Input rate limit (new: `Net/LineBucket.cs`)

- In the line reader, before a completed line is posted to the kernel, take a token from the connection's bucket (20 per second, burst 60). No token: **wait for one** (the line is delayed, never dropped; built this way at the merge so that an editor paste arrives whole). If the reader has been waiting continuously for `LESMUDOS_INPUT_OVERFLOW_SECONDS` (100 s), write "Too much input. Goodbye." and close. Empty lines count too (they cost the game thread a prompt).
- Mudlet speedwalks and aliases that send up to 60 lines at once are within the burst. The number is a named constant with an environment override.
- **Tests**: 50 lines in one burst all arrive; 1000 lines in a burst arrive only about 60 + 20 per second; a steady 10 per second is untouched; sustained overflow disconnects; the byte stream of a normal client is unchanged.

### 4.4 Output cap (in `Net/ConnectionLimits.cs` and one counter in `Connection`)

- `Connection` keeps an interlocked count of bytes queued and not yet written (added in `Send`, `SendRaw` and `SendGmcp`, subtracted in the writer loop). Above `LESMUDOS_MAX_QUEUED_BYTES` (default 1 MiB) the connection is closed with the reason "closed by the game (client too slow)" instead of queuing more. Today the queue is unbounded (`Channel.CreateUnbounded`); a normal client never has more than a few KiB queued.
- **Test**: a raw socket that logs in, asks for a flood of output (`help` repeated, a shout loop by a second client) and never reads: after the cap the connection is closed and the process's memory does not grow without bound; a client that reads slowly but steadily is not cut.

### 4.5 Not changed

No keepalive feature is needed in the driver (the bridge answers pings itself and an empty line is already not activity). No password-prompt signal is needed (telnet ECHO exists). No width setting is needed for the first version (section 6.4 describes the optional NAWS step, which would be a fifth small change). `Comm`, `Room.Info` and the other GMCP packages are unchanged.

## 5. The client page

### 5.1 Technology and files

Vanilla JavaScript and CSS, **no framework and no build step** (Decision 6): the files in `wwwroot` are exactly the files the browser runs, which keeps the page small (a few tens of KB, no runtime download), auditable (the whole input-handling code can be read in one sitting, which matters because it handles hostile text), and fast to start on a phone. Two classic scripts (not ES modules, so each can carry the site's `?v=` hash and be cached for a year without a mismatched pair): `play.js` (stages 1 to 3) and `play-map.js` (stage 4, loaded after it). Each ends with `if (typeof module !== 'undefined') module.exports = {...}` so its pure functions (`parseAnsi`, the line model, history, the map layout) can be tested with `node --test` where Node is installed, without changing how the browser loads them.

| File | What |
|---|---|
| `web/LesMudOS.Web/Pages/Play.cshtml` (replaced) | The client page, `@page "/play"`. Static HTML skeleton, `ViewData["NoIndex"] = true`, `ViewData["PlayPage"] = true` (the layout adds the play stylesheet, the manifest link and the script). When `Site:Play:Enabled` is false or the kill-switch file exists, it shows a short explanation and a link to `/how-to-play` instead of the client. |
| `web/LesMudOS.Web/Pages/HowToPlay.cshtml` (new) | The old `Play.cshtml`, moved: `@page "/how-to-play"`, plus a "Play in your browser" panel at the top (section 10.2). |
| `web/LesMudOS.Web/wwwroot/js/play.js`, `wwwroot/js/play-map.js` | The client. |
| `web/LesMudOS.Web/wwwroot/css/play.css` | Its styles, using the site's colour variables where they make sense and its own terminal palette (section 7.5). |
| `web/LesMudOS.Web/wwwroot/play/manifest.webmanifest`, `wwwroot/play/sw.js` | Install files (section 7). |
| `web/LesMudOS.Web/wwwroot/img/play-192.png`, `play-512.png`, `play-maskable-512.png`, `apple-touch-icon.png` (180) | Icons, drawn from `favicon.svg` by a new tool `web/tools/make_icons.py` (Pillow, run by the owner, the PNGs committed, as the other art is). |
| `web/LesMudOS.Web/Play/PlayOptions.cs`, `PlayEndpoints.cs`, `BridgeSession.cs`, `TelnetParser.cs`, `Frames.cs`, `ConnectionGate.cs`, `ClientAddress.cs` | The server side (section 10.1). |

### 5.2 Desktop layout

```
┌───────────────────────────────────────────────┬───────────────────────────┐
│ Legends of Hollowmere   [status: connected]   │ Alice                     │
├───────────────────────────────────────────────┤ Scout • level 5           │
│                                               │ Vitality   ████████░░ 45/58│
│  output pane (role="log", monospace, ANSI)    │ Endurance  ██████████ 106/108│
│                                               │ Focus      ████░░░░░░ 20/24│
│                                               │ [In a fight] [Resting]    │
│                                               ├───────────────────────────┤
│                                               │ Village Square            │
│                       ▼ 12 new lines below    │ Vincent's Hollow, outdoors│
│                                               │  [NW][ N ][NE]            │
│                                               │  [ W ][   ][ E ]  [Up]    │
├───────────────────────────────────────────────┤  [SW][ S ][SE]   [Down]   │
│ Alice (45/58) (106/108) (20/24) > look█       │  other: [lane] [shed]     │
├───────────────────────────────────────────────┤ ──────────────────────── │
│ [look][inventory][score][rest][flee][help] ↻  │ Chat │ Map  (stage 4)     │
└───────────────────────────────────────────────┴───────────────────────────┘
```

**The output pane** (`<div id="log" role="log" tabindex="0">`):

- Monospace, `white-space: pre-wrap; overflow-wrap: anywhere; tab-size: 8;`, so a long paragraph wraps to the pane and the score box (48 columns) and the shop columns keep their layout whenever the pane is at least as wide as the line.
- Lines are `<div class="ln">` containing the span runs of section 2.8. The **unfinished last line** is the live prompt line: later text continues it in place (a prompt is followed by the echoed command, then the game's `\n`).
- **Scrollback limit**: 3000 lines on desktop and 1500 on a phone (the setting, 500 to 10,000). When exceeded the oldest lines are removed in batches of 100. Because Safari has no scroll anchoring, the page measures `scrollHeight` before removing and subtracts the removed height from `scrollTop` after, so a reader scrolled up does not jump.
- **Scroll lock**: "stuck to the bottom" is true while the pane is within 4 px of the end. New lines keep it stuck. When the reader scrolls up, it unsticks and new lines are counted; a button at the bottom edge reads "12 new lines below" (a real `<button>`); pressing it, pressing End, or scrolling to the bottom re-sticks. Sending a command always re-sticks.
- Appending is batched in `requestAnimationFrame`, so a burst of 500 lines is one layout.
- **Text selection** works normally (copy of a line is the line's text, with no colour codes).

**The input line** (`<input id="cmd" type="text">`, a single-line field, with a visible label "Command" for screen readers):

- Enter sends. Up and Down walk the history (100 entries, consecutive duplicates collapsed, the line being typed is kept while walking and restored at the bottom). Escape clears the field. Page Up and Page Down scroll the log from the input (and End scrolls to the bottom). Tab keeps its normal meaning (focus moves on); there is no command completion.
- **Local echo**: the game never echoes, so the page appends the sent text to the prompt line, styled `echo` (a subtler weight, not a colour), and then ends the line. A masked line echoes nothing and adds no newline (section 2.10). A line with a newline in it cannot be typed (a single-line field); text pasted with newlines is joined with spaces.
- **The prompt is not duplicated**: the prompt arrives once, as the unfinished last line; the echo continues that same line. The driver's own `\n` rules (news arriving while a prompt is showing starts on a new line; a crossed prompt gets a fresh line) give the same picture a telnet terminal gives.
- The field refuses more than 1000 characters (`maxlength`, so at most 4000 UTF-8 bytes, under the 4096 limit), is `autocapitalize="off" autocorrect="off" autocomplete="off" spellcheck="false" enterkeyhint="send"`, and keeps focus after sending (section 6.2 for phones).
- **"Repeat last command"**: a button (and the key `Alt+R`, section 7.4) that sends the most recent non-masked command again.

**Number pad movement** (optional setting, off by default): with Num Lock on and the input empty, 8/2/4/6/7/9/1/3 send north/south/west/east/northwest/northeast/southwest/southeast, 5 sends `look`, `+` up, `-` down. Off by default so a player typing numbers into a name is never surprised.

**Page states**: Before connecting: the heading, the how-to-play strip, the "Enter Hollowmere" button, a line "Playing from a phone? Add this page to your home screen" (section 7), and links to `/how-to-play`. After the game closes the session: the log stays readable and a "Reconnect" button appears. While reconnecting: a banner above the input (not in the log) "Connection lost. Trying again in 4 s ... [Try now] [Stop]".

### 5.3 The status panel (stage 2) and plain-text fallback

Driven only by GMCP; the panel is hidden until the first GMCP message arrives, so a session without GMCP (a bridge or driver problem, or a future `Core.Supports` mismatch) is simply the plain text client, fully playable.

- **Identity** (`Char.Status`): `name`, `calling`, `level` as "Alice, Scout, level 5". Experience bar `experience` toward `next_level` (hidden when `next_level` is 0, which is what the game sends at the maximum level), `gold`, and a small note "N attribute points to place" when `points_to_place` is above 0.
- **Bars** (`Char.Vitals`): **Vitality** `vitality`/`maxvitality`, **Endurance** `endurance`/`maxendurance`, **Focus** `focus`/`maxfocus` (the game's own names; the duplicates `hp`, `maxhp`, `mp`, `maxmp` are ignored). Each is a `<div role="meter">` with `aria-valuemin="0"`, `aria-valuemax`, `aria-valuenow`, `aria-valuetext="Vitality 45 of 58"`. The visible label and `45/58` numbers are always shown beside the bar; a bar below 25% adds the word "low" and a hatched fill (so it is not colour alone); the three bars differ by label and position, not only hue.
- **Badges**: "In a fight" when `fighting` is true, "Resting" when `resting`, "Sneaking" when `sneaking`. Text, not colour alone. There is no target or enemy-health bar because `Char.Vitals` has none (section 1); the panel does not invent one.
- **Room** (`Room.Info`): `name`, `area`, `environment` as plain words, and when the light feature ships its `light` field (`"lit"`, `"dim"` or `"dark"`) as a small badge and a dimmed room title. Unknown extra fields are ignored; a missing field leaves its widget empty, never an error.
- **Exits**: see 5.5.
- The panel never displays anything computed from game prose.

### 5.4 GMCP fields the client reads (exact, from `mudlib/secure/gmcp.cs`)

| Package | Fields | Notes |
|---|---|---|
| `Room.Info` | `num` (int, stable across restarts; at least 1,000,000,000 means "not saved", a wizard's home or a clone), `name` (string, at most 120), `area` (at most 80; `"Workshop of <Name>"` for a wizard's home; `"Elsewhere"` when none), `environment` (`"outdoors"`, `"indoors"`, `"underground"`, `"road"`, `"water"`), `exits` (object: exit word to the destination's `num`; the word is a compass direction or a named exit such as `lane`; at most 32; hidden exits only after the character found them), `doors` (object: exit word to `"open"`, `"closed"` or `"locked"`; only exits that have a door). Later: `light`. Sent on entering a room, on `look`, and when exits or doors change. | |
| `Char.Vitals` | `vitality`, `maxvitality`, `endurance`, `maxendurance`, `focus`, `maxfocus` (ints), `resting`, `sneaking`, `fighting` (bools), and the duplicates `hp`, `maxhp`, `mp`, `maxmp`. At most one message per 250 ms. | |
| `Char.Status` | `name`, `calling`, `level`, `experience`, `next_level`, `gold`, `points_to_place`. Sent on login and on change. | |
| `Comm.Channel.Text` | `channel` (`"tell"`, `"shout"`, or a channel id such as `"wizard"`; at most 32), `talker` (at most 40), `text` (at most 600; the message as the player sees it, not wrapped in the game's own `{`-markup). | `say` and `whisper` are not sent. |

### 5.5 Exits as compass buttons (stage 2)

A 3 by 3 grid of the eight compass directions around a centre showing the room name, plus `Up` and `Down`, built from the keys of `Room.Info.exits`: the keys `north`, `south`, `east`, `west`, `northeast`, `northwest`, `southeast`, `southwest`, `up`, `down` go to their places; any other key (`lane`, `shed`, `in`, `out`, `enter`) becomes a labelled button in an "other ways" row. A direction with no exit is a disabled, empty cell (kept so the grid does not jump). A door's state is added to the label ("north (closed)") and the accessible name ("north, door closed"). Tapping or pressing sends the exit key as a typed command, exactly as the player could (`north`, `lane`), and echoes it. A key longer than 32 characters or with a control character is not offered. The `{cmd}`-styled words in the text ("Obvious exits: north, up") are tappable too (section 6.3), so the panel is a convenience and the text works without it.

### 5.6 The chat pane (stage 4)

A tab beside Map: "Chat", a second `role="log"` fed by `Comm.Channel.Text` and nothing else. Lines are built as: `tell`: "Bob tells you: text"; `shout`: "Bob shouts: text"; any other channel: "[wizard] Bob: text" (the formats the game itself uses). It keeps its own scrollback (500 lines), its own scroll-lock marker, and an unread count on the tab. It is a copy: the same lines still appear in the main log, so the player misses nothing if GMCP is off. `say` and `whisper` stay in the main log only (not sent by GMCP). On a phone the pane is a tab under the log; an unread dot shows on the tab, and a `tell` also raises the screen-reader announcement (section 7.3).

### 5.7 Command buttons, repeat, history

A row of buttons under the input, defaults `look`, `inventory`, `score`, `rest`, `flee`, `help`; each is a label and one command (at most 60 characters, no newline). "Edit buttons" in the settings dialog adds, removes, reorders and renames (up to 12). Tapping sends the command and echoes it. A button for a skill or spell is added by the player typing its command (the game sends no skill list over GMCP; the `skills` command's text is not parsed). "Repeat last" shows the command it will repeat in its label (`↻ kill rat`), is disabled until there is one, and never repeats a masked line.

### 5.8 The automap (stage 4: what a first version can and cannot do)

What `Room.Info` gives: a stable room number, a name, an area name, the terrain, and exits as `word → destination number`. It gives **no coordinates**, no map of rooms the player has not entered, no names for rooms the player has not been in, and no direction for named exits.

What a first version does, in the same way as the Mudlet package ([../MUDLET.md](../MUDLET.md)), in `play-map.js` and drawn in an inline `<svg>` built with `createElementNS` (no library):

- Each room the player enters becomes a node keyed by `num`. Placement is greedy: a new room is put one grid step from the room the player came from in the direction taken (`north` up the screen, `up` and `down` as separate levels); if that cell is occupied, the nearest free cell is used so two rooms are never drawn on top of each other; a room reached with no known exit leading to it (a respawn, a teleport) is placed to the right of everything else in its area; the first room of an area is at 0,0. Layout never rearranges rooms already placed.
- Exits are drawn as lines. An exit to a room not yet visited is a short stub with no name. The way back is linked when the player stands in that room and the game lists it. A named exit (`lane`) is drawn as a dashed link to a free cell beside the room. Doors are marked with a small bar (closed or locked).
- The map is per area (a drop-down lists areas visited, current one by default), centred on the current room, panned by dragging or arrow keys, zoomed with buttons, and each room is a focusable element with an accessible name (the room name and its exits) so the map is not a picture only.
- Persistence: one record per character in `localStorage` (`hm.map.v1.<name in lower case>`), rooms and links only (number, name, area, grid position, exit words with numbers, door states, environment), at most 4000 rooms, around 150 bytes each (well under the 5 MB storage limit). Rooms with `num` at or above 1,000,000,000 are never saved (they are new after every restart). A "Clear map" button. If the owner ever resets the game's world, room numbers start again and a stored map is wrong: Clear map is the remedy (the same caveat as the Mudlet map).
- It cannot: show rooms the player has not entered, know the real geography (loops that close badly leave long lines; the layout is not moved afterwards), show exits that disappear (it only adds and updates), show a different character's discoveries, or follow a wizard's workshop across restarts. A dark room (the `light` field) sends only the exits the character knows, so the map learns nothing the text did not.
- The map must work with GMCP only; without GMCP the map pane says "The map needs the game's map data (GMCP)".

### 5.9 Character set

The page decodes UTF-8 always. The score sheet's box and bullet are UTF-8 today (score.md); the coming `charset ascii|utf8` setting is per player and the client does not change it. A player who sees broken box characters (a font that does not have them) types `charset ascii` themselves; the how-to-play strip (section 9.2) does not mention it, the "If something looks wrong" line in the settings dialog does. The monospace stack is chosen for box-drawing coverage; whether Android and iOS draw it at exactly one column is a manual test (section 12.3).

## 6. Phone layout

### 6.1 One column, and the keyboard is the enemy

Below 900 px wide the page is one column, in this order: a slim header (status dot, menu button), the output pane filling the remaining height, an "exits strip" (a single row of the available exits as buttons, scrolling sideways if there are many), the command-button row, the input line. The status panel becomes a collapsible bar under the header showing the three bars in compact form ("V 45/58  E 106/108  F 20/24"); tapping it opens the full panel as a sheet. Chat and Map are sheets opened from the menu. All buttons are at least 44 by 44 CSS pixels with 8 px between them.

### 6.2 The on-screen keyboard and the visual viewport

When the keyboard opens, iOS Safari does not resize the layout viewport (it slides the page up and leaves the input half hidden, or scrolls the page) and Android Chrome resizes it (by default). The approach that works on both:

- The page is a fixed-height app: `html, body { height: 100%; overflow: hidden; }` and one container with `height: var(--vh)` (a CSS variable), `display: flex; flex-direction: column`, the log `flex: 1; min-height: 0`.
- JavaScript sets `--vh` from **`window.visualViewport.height`** on `visualViewport` `resize` and `scroll` events (and on `orientationchange`), and sets the container's top offset from `visualViewport.offsetTop` so the container follows the visible area when iOS scrolls it. Fallback where `visualViewport` is missing: `window.innerHeight`.
- The viewport meta is `width=device-width, initial-scale=1, viewport-fit=cover` and `interactive-widget=resizes-content` (Chrome then resizes the layout viewport itself; iOS ignores the key). Safe-area padding uses `env(safe-area-inset-*)` for the notch and the home bar.
- The input is `font-size: 16px` minimum on a phone (iOS zooms into smaller inputs on focus).
- After sending, the input keeps focus (the keyboard stays up): `event.preventDefault()` on `pointerdown` of the command buttons and of the send button, so tapping them does not blur the input; a tap on a command button sends without closing the keyboard.
- While the keyboard is open the exits strip and button row stay above it and the log shrinks; the log re-sticks to the bottom on every viewport change if it was stuck.
- A dedicated send button (a real `<button type="submit">`) sits beside the input; most phone keyboards have a Return key labelled "Send" through `enterkeyhint`, but players expect a button.

### 6.3 Tap a word, repeat, and the exits

- **Tap a word in the output to put it in the input line**: a tap on `{cmd}`-styled text (spans with class `cmd`: exits and the commands the game highlights) inserts that word at the cursor, followed by a space. A tap on any other word also inserts it, found with `document.caretPositionFromPoint` (or `caretRangeFromPoint` on Safari), taking the run of letters, digits, apostrophes and hyphens at that point, so a player can tap a creature's name to build `kill rat`. If neither API exists only `cmd` spans are tappable. Setting "Tapping a highlighted word": "puts it in the input" (default) or "sends it at once". A tap that follows a text selection or a scroll drag is ignored (the page distinguishes a tap from a drag by movement under 10 px and time under 500 ms).
- **Exits**: the exits strip (section 6.1) and the `{cmd}` words; a swipe on the strip scrolls it.
- **Repeat last command**: a button at the end of the command row.

### 6.4 Width: 80 columns on a 45 to 60 column screen

The game does not wrap prose (section 1), so most text needs nothing. The decision (Decision 10): **choose the font size so that at least 48 columns fit** (the score box is 48 wide, so it fits exactly with no wrapped line), let CSS wrap everything longer, and leave the game untouched.

- Default phone font size = the largest size in 11 to 16 px at which 52 columns of the monospace font fit in the pane's width (measured once with a hidden span of 52 characters, re-measured on resize and orientation change); the player's "A-" and "A+" override it (10 to 24 px, remembered). A 360 px portrait phone gets about 11.5 px; a 390 px one about 12.5 px; landscape gets a larger one.
- Lines wider than the pane wrap at any character (`overflow-wrap: anywhere`); a column list (shop, `who`) that is wider than the pane wraps messily, which is accepted. The page never scrolls horizontally: `overflow-x: hidden` on the log, nothing positioned wider than the viewport, tested at 320, 360, 390, 412 and 768 px.
- **NAWS (a later, optional step, only if column lists look bad on phones)**: the driver sends `IAC DO NAWS` (`FF FD 1F`) **only on bridge-listener connections** (so telnet clients see no change), the bridge answers `IAC WILL NAWS` and, for each `size` frame, `IAC SB 31 <width high> <width low> <height high> <height low> IAC SE` (a 255 byte doubled), the driver's existing subnegotiation reader stores `Connection.Columns` (clamped to 40 to 250, 0 meaning unknown), a new efun (`ClientColumns(player)`, documented in `docs/DRIVER_API.md`) returns it, and `/secure/scoresheet`'s `MaxColumns` constant becomes `Math.Clamp(columns, 48, 80)` for `Wrap` and `Columns` (the 48-column box itself never shrinks). The page reports `cols` but never fewer than 48. That is about 60 lines of driver, 10 of mudlib and one efun; it is not part of stages 1 to 3 and the `size` frame is already accepted so the page needs no change when it arrives.
- The `charset` setting does not change widths; the box characters are single-width, but if a phone font draws them wider (manual test) the box wraps; `charset ascii` is the remedy.

### 6.5 Orientation, font size, installability

Portrait and landscape both work (the container height follows the visual viewport, so a landscape keyboard leaves only a few lines of log; in landscape on a phone the status bar and exits strip collapse into one row to leave room, and the command row hides behind a "More" button while the keyboard is open). A font-size control ("A-", "A+") sits in the menu and in the settings dialog. Installing to the home screen is section 7.

## 7. Install, offline, accessibility

### 7.1 Installable (web app manifest)

Installing means the phone shows an icon on the home screen and opens the game in its own window without the browser's address bar, which on a phone is a quarter of the screen. It does not make the game work offline (the game needs the network) and it does not run in the background.

`/play/manifest.webmanifest` (served with `Content-Type: application/manifest+json`; the test checks the type and adds an explicit mapping in `Program.cs` if the static-file provider does not know the extension):

```json
{
  "id": "/play",
  "name": "Legends of Hollowmere",
  "short_name": "Hollowmere",
  "description": "Play Legends of Hollowmere in your browser.",
  "start_url": "/play",
  "scope": "/play",
  "display": "standalone",
  "orientation": "any",
  "theme_color": "#2f4a2b",
  "background_color": "#15120e",
  "icons": [
    { "src": "/img/play-192.png", "sizes": "192x192", "type": "image/png" },
    { "src": "/img/play-512.png", "sizes": "512x512", "type": "image/png" },
    { "src": "/img/play-maskable-512.png", "sizes": "512x512", "type": "image/png", "purpose": "maskable" }
  ]
}
```

The page head adds `<link rel="manifest" href="/play/manifest.webmanifest">`, `<link rel="apple-touch-icon" href="/img/apple-touch-icon.png">` and `<meta name="mobile-web-app-capable" content="yes">` / `apple-mobile-web-app-capable` (older iOS). The page explains in one line how to add it ("Share, Add to Home Screen" on iPhone; the menu's "Install app" on Android) and, where the browser fires `beforeinstallprompt`, offers an "Install" button (that event is Chromium only).

### 7.2 Service worker: the shell only

`/play/sw.js`, registered with `{ scope: '/play' }` (served with `Service-Worker-Allowed: /play`; if the installed browsers reject that scope, the fallback is root scope with a fetch handler that ignores everything but the allow-list below). It does exactly this:

- Handles only `GET` requests for the allow-list: `/play`, `/js/play.js`, `/js/play-map.js`, `/css/play.css`, `/css/site.css`, `/play/manifest.webmanifest`, `/img/play-*.png`, `/favicon.svg`. Every other request, including `/play/ws`, `/play/status` and any `Authorization` request, is not touched (the handler returns without calling `respondWith`).
- **Network first, cache as fallback**: it fetches from the network, stores a copy in cache `hm-shell-v1`, and uses the cached copy only if the network fails. So an online player always gets the newest page, and there is no version number to bump. On `activate` it deletes any other cache.
- WebSocket traffic is not visible to a service worker at all, so game traffic cannot be cached; the worker's code never mentions the socket.
- **Offline** (the page loads from the cache with no network): the page shows "You are offline. The game needs a connection." and the Enter button is disabled until `navigator.onLine` and `/play/status` succeed; the player's settings and (stage 4) map are readable in the settings dialog. Nothing else.
- Registered only after the page has loaded and the player has pressed Enter once (not before); unregistered by "Forget everything".

### 7.3 Screen readers

- The output pane is `role="log"` (implicitly `aria-live="polite"`, `aria-atomic="false"`, additions only), labelled "Game output". New lines are appended in batches, so a reader announces them in order without restating the pane.
- **A way to pause**: a button "Pause announcements" (`aria-pressed`) sets the pane to `aria-live="off"`; the text is still there to read by moving into the pane. A setting "Announcements" has three values: **All new text** (the default, the log's own live region), **Messages only** (the log is `aria-live="off"`; a separate visually hidden `role="status"` region announces `tell`, `shout` and channel lines from `Comm.Channel.Text`, and "Vitality low" once when vitality falls under 25% and again only after it recovered above 50%, at most once per 10 seconds), **Off**.
- The player's own echoed command is not read back: with the setting "Do not read back what I type" (default on) the echoed span is `aria-hidden="true"`; it stays visible and copyable.
- Focus: on entering the game, focus goes to the input; after a reconnect banner appears it does not steal focus; dialogs trap focus and return it to the control that opened them. The status bars have `aria-valuetext`. Exit buttons have names like "north, door closed".
- The live region is rate-limited: if more than 200 lines arrive in one second (a flood), only the last 20 are placed in the live region, and the rest are in the pane.

### 7.4 Keyboard only

Everything is reachable with Tab in a sensible order (input, send, command buttons, exits, status sheet, chat, map, menu). Shortcuts (all listed in the "Keys" section of the settings dialog, none stealing browser shortcuts or typing): `Enter` send; `Up/Down` history; `PageUp/PageDown/End` scroll the log from the input; `Escape` clear the input, or close a dialog; `Alt+M` menu, `Alt+C` chat, `Alt+P` map, `Alt+R` repeat last command; number pad directions only if the setting is on. The log pane itself is focusable (`tabindex="0"`) so arrows scroll it. Visible focus rings use the site's `:focus-visible` outline.

### 7.5 Colour, contrast, motion

Three themes: **Dark** (default for the play page, regardless of the site's light theme: a game reads better on a dark ground), **Light** (parchment: the site's own `--parch` and `--ink`), and **High contrast** (pure black and white). The setting "Theme" has "Match my device" (follows `prefers-color-scheme`, and `prefers-contrast: more` selects High contrast), Dark, Light, High contrast. `forced-colors: active` is respected: the page does not override system colours on text, buttons or focus rings, and the bar fills use `CanvasText` / `Highlight`.

The terminal palette, all pairs at least 4.5:1 against their theme's background (the High contrast set at least 7:1; computed values, and a unit test recomputes them as the site's `ContrastTests` does for its own pairs):

| SGR | Dark (bg `#15120e`, text `#e8dfc9`, 14.07:1) | Light (bg `#faf4e3`, text `#2b1d12`, 14.85:1) | High contrast (bg `#000000`, text `#ffffff`, 21:1) |
|---|---|---|---|
| 31 / 91 red | `#e0786a` 6.28 / `#ff9a8c` 9.12 | `#a3231a` 6.80 / `#b01c0f` 6.33 | `#ff6b5e` 7.52 / `#ff8a80` 9.20 |
| 32 / 92 green | `#7fc66f` 9.08 / `#a4e68f` 12.71 | `#2a6a22` 6.00 / `#1d6b18` 6.03 | `#5dff5d` 15.93 / `#8aff8a` 16.75 |
| 33 / 93 yellow | `#d9b45a` 9.45 / `#f0d27a` 12.63 | `#7a5a00` 5.81 / `#6e5000` 6.80 | `#ffe14d` 16.13 / `#fff176` 18.10 |
| 34 / 94 blue | `#7fa6ec` 7.62 / `#a3c2ff` 10.42 | `#2547a8` 7.51 / `#1a3e9e` 8.58 | `#7aa8ff` 8.87 / `#9bbdff` 11.12 |
| 35 / 95 magenta | `#c88fd8` 7.45 / `#e3b0f0` 10.43 | `#80308f` 6.98 / `#7a2a8a` 7.57 | `#ff7aff` 9.48 / `#ff9bff` 11.38 |
| 36 / 96 cyan | `#6fc6c6` 9.40 / `#8fe3e3` 12.68 | `#12666b` 6.09 / `#0d5c61` 7.03 | `#4dffff` 17.06 / `#80ffff` 17.67 |
| 37 / 97 white | `#e8dfc9` 14.07 / `#ffffff` 18.67 | `#4a4a4a` 8.07 / `#2b1d12` 14.85 | `#ffffff` 21.00 / `#ffffff` 21.00 |

Bold (`1`) is a heavier weight and never changes the colour; underline is an underline. Backgrounds `40` to `47` and `100` to `107` use the same table and, when a background is set, the text colour is replaced by the theme's default if the pair would fall under 4.5:1 (the game does not send backgrounds today). Nothing meaning is carried by colour alone: the bars have labels and numbers, a low bar says "low" and is hatched, a door is in the button's text, "In a fight" is a word.

Reduced motion: with `prefers-reduced-motion: reduce` there is no smooth scrolling, no bar transitions and no animated banners; the "new lines below" marker simply appears. Even without the preference nothing flashes and no animation lasts over 300 ms.

## 8. Sessions

### 8.1 Reconnecting when the socket drops

The page keeps a state: `idle` (before Enter), `connecting`, `open`, `reconnecting`, `closed`. If the socket closes without the page having asked, and the close code is not 1000, the page enters `reconnecting`: waits 1, 2, 4, 8, 15, then 30 seconds between tries (each with up to 20% random jitter, so a website restart does not bring every player back in the same second), forever, until the player presses **Stop** or the tab closes. It shows the banner "Connection lost. Trying again in N s [Try now] [Stop]" and, in the log, one local line in the `note` style (not from the game): `*** Connection lost ***`, and on success `*** Connected. Please sign in again. ***`. A successful socket is the login screen again (there is no session to resume, Decision 7); the page puts focus in the input and, if "Remember my character name" is on, shows a one-tap button with the name when the unfinished last line ends with `known? ` or `name? ` (the exact end of "By what name are you known? " and "Another name? " in `login.cs`; if the wording changes the button just does not appear). The backoff counter resets when a connection stays open for 60 seconds. On close 1000 (the game ended the session: `quit`, idle timeout, shutdown) there is no automatic retry, only the **Reconnect** button. On close 1013 the retry runs and the banner says what the bridge said (`full` or `unavailable`).

### 8.2 The link-dead body, and the idle timeout

- **Until the 60-second feature ships**, a dropped connection saves and removes the body at once (`NetDead`), so reconnecting is simply logging in again; nothing is left in the room. In a fight the body is also removed, so a phone that loses signal mid-fight ends the fight for the player (as it does for telnet).
- **With the feature**, the body stays in the room for 60 seconds and logging in again with the same name and password resumes it. The socket drop is detected by the bridge within at most about 75 seconds of silence (or at once on a clean close), so for a phone that went to sleep the sequence is: connection stops, up to 75 s later the bridge closes the telnet link, the 60-second timer starts, then the body goes. A player who comes back within the first 75 seconds used to see "`{name} is already here. Another name?`" (the old link is not yet closed); since the take-over (login-throttle.md) the password takes the character over from the old link, which is closed, so the note and the wait described next are no longer needed (the page's local note, if it still exists, never has a trigger). Closing the old socket faster is not possible from the page once the phone has lost it; the page does send a clean close when it is **deliberately** left (see 8.4) so those cases do not wait. (A closed tab, a navigation away and `pagehide` are the cases: the page closes with code 1000 and the bridge drops the game link at once.)
- The page cannot defend a character while it is link-dead: **it will be in the room, possibly attacked**. How the game treats a link-dead body in a fight is that feature's rule (unknown here); the page's job is to warn and to avoid the drop.
- The idle timeout is unaffected by the page: idle at 5 minutes, warning at 8, disconnect at 10 (the server's settings), counted from the last typed non-empty line. The warning arrives as game text. After the disconnect the page shows the Reconnect button (close 1000).

### 8.3 Two tabs, one character

The game's rule (`login.cs`, `FindOnline`) is: a login of a name that is online, with the right password, takes the character over from the old connection (login-throttle.md, "Taking over"). The page adds a courtesy on top, without changing the rule: it takes a Web Lock (`navigator.locks.request('hm-play', { ifAvailable: true }, ...)`) while its socket is open; a second tab of the same browser that cannot get the lock shows "Hollowmere is already open in another tab of this browser" with a "Connect anyway" button, so the common mistake is caught before the login. Another browser or device takes the character over, as a second telnet window does. Unsupported browsers skip the courtesy.

### 8.4 Phones sleeping, bfcache, hidden tabs

- **When a phone locks or the browser goes to the background, the socket dies** (iOS closes it within seconds to minutes; Android may keep it a little longer). **This will trigger the link-dead rule in the middle of a fight.** The page says so on the how-to-play strip and in the settings dialog ("Locking your phone disconnects your character after about a minute. Stay out of fights when you step away.").
- **Keep the screen awake**: the setting "Keep the screen on while playing" (default on, where `navigator.wakeLock` exists: Chrome on Android, Safari from iOS 16.4) requests a screen wake lock while the page is visible and connected, and requests it again on `visibilitychange` (the browser releases it when the page is hidden). A small indicator in the menu shows it is held. It prevents the automatic lock, not the power button or switching apps.
- **On wake** (`visibilitychange` to visible, `pageshow`, the `online` event): if the socket is not `open`, reconnect at once (skip the back-off wait); if it is open but nothing has been received for more than 30 seconds, send a ping and, if no `pong` arrives in 3 seconds, treat it as dead and reconnect.
- **Back/forward cache**: an open WebSocket makes a page ineligible for the bfcache in some browsers, and a restored page with a dead socket is worse than none. On `pagehide` the page closes the socket cleanly (code 1000, so the bridge drops the game connection immediately and the 60-second timer starts at once, not 75 seconds later); on `pageshow` with `persisted` true it shows the log as it was, the banner "You left this page; the game connection was closed" and a Reconnect button.
- **Leaving in a fight**: `beforeunload` asks for confirmation only while the last `Char.Vitals` said `fighting` is true; never otherwise.

### 8.5 "Stay signed in"

Not in the first version (Decision 7). The player types name and password every time; the page stores neither the password nor any token. The follow-ups, in order of safety: (1) the opt-in in-memory re-login for the life of the tab (the password held in one JavaScript variable, never written anywhere, sent again after a drop, cleared on close; it widens the window in which a script injection could read it, so it ships only if the owner accepts that with the strict CSP and the no-`innerHTML` rule in place); (2) a token resume, which needs a driver feature (the game issuing a one-time resume token at login, held by the bridge, valid for 5 minutes after a drop and tied to the true address) and is out of scope here.

## 9. Accounts and help

### 9.1 Account creation

For the first version the browser uses the game's own text login: type a name, get "Creating a new account for Alice.", choose and repeat a password (masked), choose a calling. The how-to-play page's "Your first login" steps apply unchanged. A friendlier form later (name, password, calling as three fields) would need: a driver-side or mudlib-side account-creation entry point that does not go through the interactive prompts (the login object is a chain of `InputTo` handlers), rate limiting per address, an email or recovery story (there is none today), and a CAPTCHA decision; none exists, so it is out of scope (section 14). Nothing in the page parses the login prose except the two cosmetic hints in section 8.

### 9.2 The "how to play" strip

A one-line dismissible strip above the log, shown until dismissed, remembered in `hm.settings.v1` (`stripDismissed`): "Type commands and press Enter. Try **look**, **north**, and **help**. Tap the glowing words (exits and commands) to use them. [Hide]". It is plain text with the three commands as `<code>` elements; tapping a command in the strip inserts it into the input. It is also in `/how-to-play`.

### 9.3 The page's notices

Under the Enter button, always visible: "Your password travels over HTTPS to Cloudflare, which carries it to the game. Use a password you use nowhere else." (the page replaces the telnet page's "not encrypted" notice, which stays on `/how-to-play` for telnet.)

## 10. Website changes

### 10.1 Settings (`SiteOptions`, new nested class `PlayOptions`, section `Site:Play`)

Bound with the rest of `SiteOptions`; set in `appsettings.json`, by environment (`Site__Play__MaxConnections=60`), or in `/etc/lesmudos-web.env`. A change needs a website restart (which drops web players) except the kill switch.

| Key | Default | Meaning |
|---|---|---|
| `Site:Play:Enabled` | `true` | Master switch. False: `/play` shows the explanation, `/play/ws` is 404, `/play/status` says `enabled:false`. |
| `Site:Play:KillSwitchFile` | `/var/lib/lesmudos-web/play.off` | If this file exists the client is off for **new** connections and new page views, checked at most every 5 seconds, **without a restart or deploy** (`touch` to turn off, `rm` to turn on). Existing sessions continue. |
| `Site:Play:GameBridgeHost` / `GameBridgePort` | `127.0.0.1` / `4001` | Where the bridge connects. Must be loopback unless `AllowRemoteBridge` is true (a safety check at startup: a non-loopback host is refused). |
| `Site:Play:AllowedOrigins` | empty (= the origin of `Site:BaseUrl`) | Exact origins allowed to open the socket (section 3.1). |
| `Site:Play:RequireClientIpHeader` | `true` | Refuse a connection with no valid `X-Hollowmere-Client-Ip` (false only in Development). |
| `Site:Play:MaxConnections` / `MaxPerAddress` | `60` / `5` | Section 3.2. |
| `Site:Play:ConnectsPerMinute` / `ConnectsPerMinutePerAddress` | `120` / `10` | Connect rate. |
| `Site:Play:InputLinesPerSecond` / `InputBurst` | `10` / `20` | Typed-line rate. |
| `Site:Play:MaxInputBytes` / `MaxFrameBytes` / `MaxQueuedBytes` | `4096` / `8192` / `262144` | Sizes. |
| `Site:Play:PingSeconds` / `ClientSilenceSeconds` | `25` / `75` | Keepalive. |
| `Site:Play:ShutdownGraceSeconds` | `5` | How long a stopping website waits after sending `sys restart`. |

`SiteOptions` gains `public PlayOptions Play { get; set; } = new();`. `appsettings.json` gains a `"Play": { "Enabled": true }` block inside `Site`. The existing `Site:GameHost` and `Site:GamePort` stay for the instructions page.

New server files (all under `web/LesMudOS.Web/Play/`): `PlayOptions.cs` (the settings and the startup check that the bridge host is loopback); `PlayEndpoints.cs` (`MapPlay(this WebApplication)`: `/play/ws`, `/play/status`); `BridgeSession.cs` (one player: handshake checks, the two pumps, ending); `TelnetParser.cs` (section 2.8); `Frames.cs` (the frame types and their JSON, with the size and shape checks); `ConnectionGate.cs` (counts, per-address and rate limits, with a pruned dictionary capped at 10,000 addresses); `ClientAddress.cs` (reads and validates `X-Hollowmere-Client-Ip`, IPv6 /64 key). `Program.cs` gains: `app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(25) })` (plus the timeout, section 2.7), `app.MapPlay()`, Kestrel's `Limits.MaxConcurrentUpgradedConnections = MaxConnections + 20`, a `.webmanifest` content-type mapping if needed, the per-path CSP (section 3.6) and cache headers in the existing header middleware, and a registration of an `IHostApplicationLifetime.ApplicationStopping` callback that sends `sys restart` and closes the sockets with 1012.

### 10.2 Pages, nav, links

- `Pages/Play.cshtml`: the client (section 5). `Pages/HowToPlay.cshtml`: the old page, moved, with a new first panel: "**Play in your browser** — no download, works on a phone. [Play now](/play)" above "Connect, step by step" (the telnet, Mudlet and phone-app instructions that are already there), and with the "Download Client (coming later)" block replaced by one sentence: "A native app is not planned: the browser version installs to your home screen." (Mudlet remains the recommended desktop client.)
- `Pages/Shared/_Layout.cshtml`: the nav's `Play Now` stays `/play` (now the client); the footer's "How to Play" becomes `/how-to-play`; on the play page the masthead is kept on desktop, and masthead and footer are hidden below 900 px and when installed (`display-mode: standalone`) so the log gets the whole screen (the play page's own menu button links back to the site). `ViewData["PlayPage"]` adds `<link rel="stylesheet" href="/css/play.css" asp-append-version="true">`, the manifest and icon links, `<script src="/js/play.js" asp-append-version="true" defer></script>` (and `play-map.js` in stage 4), and the viewport meta of section 6.2.
- `Pages/Index.cshtml`: the hero's `Play Now` (`/play`) and `Enter Hollowmere` (`/play`) now open the game (no change to the markup); `Create Account` (`/play#first-login`) becomes `/how-to-play#first-login`; the join section gains `Play in your browser` (class `btn`, `/play`) as the first button and `Other ways to connect` (`/how-to-play`) as a text link beside it.
- Links to change from `/play` to `/how-to-play`: `Pages/Support.cshtml` (the "How to play" sentence), `web/content/news/2026-11-18-the-gates-are-open.md`, `Pages/World.cshtml` keeps `Walk the road` at `/play` (it now opens the game: right). `SitePages.Static` replaces `/play` by `/how-to-play` (so the sitemap lists the instructions, not the noindex client).
- `Program.cs` `robots.txt`: add the two `Disallow` lines (section 3.6).
- `publicfilter.json`: unaffected (the client reads no Chronicle data). `docs/WEBSITE.md`: new rows in the configuration table, the new endpoints, the "What is not built" bullet about the web client removed.

### 10.3 Publicly visible hidden content

The play page and its scripts contain no world data (no room names, no map, no class lists): everything the player sees comes over the socket from the game, to that player. The sitemap does not list `/play`. The status endpoint returns no counts. Nothing from `publicfilter.json` is relevant.

### 10.4 Deploy changes (exact lines)

**`deploy/setup-droplet.sh`** (the game): add a setting `BRIDGE_PORT="${BRIDGE_PORT:-4001}"` beside `GAME_PORT`, and change the unit's `ExecStart` to

```
ExecStart=$BIN_DIR/LesMudOS.Driver --mudlib $MUDLIB_DIR --save $SAVE_DIR --port $GAME_PORT --bridge-port $BRIDGE_PORT
```

Do **not** add a `ufw` rule for 4001 (it is bound to `127.0.0.1`; the existing default-deny applies). The final "game is up" check stays on `$GAME_PORT`. `update-droplet.sh` already re-runs this script, which rewrites the unit, so existing droplets get the flag at the next game update.

**`deploy/setup-website.sh`**: in the settings block add `BRIDGE_PORT="${BRIDGE_PORT:-4001}"` (read from the game unit like `GAME_PORT`: `BRIDGE_PORT="$(grep -oP -- '--bridge-port \K[0-9]+' "$GAME_UNIT" || echo 0)"`; if it is 0 the script prints a warning that the browser client will be off and sets `Site__Play__Enabled=false`), add to the env file template (created once; an existing file is not overwritten, so the script appends missing `Site__Play__` lines with `grep -q ... || echo ... >>`):

```
# The browser client (section "Play" in docs/WEBSITE.md). Restart after editing: systemctl restart lesmudos-web
Site__Play__Enabled=true
Site__Play__GameBridgePort=$BRIDGE_PORT
# Site__Play__MaxConnections=60
# Site__Play__AllowedOrigins__0=https://$WEB_DOMAIN
```

The website's unit needs no change: it already binds `127.0.0.1:$WEB_PORT`, has `MemoryMax=300M` (raise to `400M` if the measurement in section 11 says so), no `RestrictAddressFamilies` (so the loopback connection to 4001 is allowed), and `ReadOnlyPaths=$DATA_DIR` plus `ProtectSystem=strict` still let it **read** `/var/lib/lesmudos-web/play.off` (the parent directory is `750 $GAME_USER:$WEB_USER`, so the website's group can see a file the owner creates there as root).

The **Caddyfile** the script writes (replacing the existing one between its markers) becomes:

```
# managed by setup-website.sh
{
	servers {
		trusted_proxies static <Cloudflare IPv4 and IPv6 ranges, one per word>
		client_ip_headers CF-Connecting-IP
	}
}

www.$WEB_DOMAIN {
	redir https://$WEB_DOMAIN{uri} permanent
}

$WEB_DOMAIN {
	encode zstd gzip
	reverse_proxy 127.0.0.1:$WEB_PORT {
		header_up X-Hollowmere-Client-Ip {client_ip}
		stream_close_delay 2m
	}
	header -Server
}
```

and a new step before it fetches the ranges: `CF_RANGES="$(curl -fsS https://www.cloudflare.com/ips-v4; echo; curl -fsS https://www.cloudflare.com/ips-v6)"`, checks each line looks like a CIDR (`^[0-9a-fA-F:.]+/[0-9]+$`), refuses to continue if none came back and no previous list exists, otherwise reuses the previous list. `caddy validate` (already in the script) checks the syntax. **Unverified on the installed Caddy**: `trusted_proxies` and `client_ip_headers` need Caddy 2.7 or newer, `{client_ip}` is the placeholder that respects them, and `stream_close_delay` is the option that keeps upgraded connections open across a reload; the script prints `caddy version` and the manual test (section 12.3) confirms the logged address is the phone's, not Cloudflare's. Caddy proxies WebSocket upgrades with no extra option.

### 10.5 Cloudflare settings

- **Network, WebSockets: on** (the default on all plans). **SSL/TLS mode: Full (strict).**
- **Caching**: a Cache Rule "bypass cache" for the path starting with `/play/ws` and `/play/status` (the response headers already say `no-store`, the rule makes it certain). Do not enable "Cache Everything" on `/play`. 
- **Rocket Loader, Auto Minify, Mirage, Email Obfuscation, and Web Analytics / Browser Insights auto-injection: off** for `/play` (they rewrite or inject scripts; the CSP would block the injected beacon and log errors, and Rocket Loader can break a script that is not meant to be deferred that way).
- **Bot protection**: a managed challenge or JavaScript challenge cannot be solved by a WebSocket handshake. The page itself, loaded in a browser, would pass a challenge and receive its `cf_clearance` cookie, which the browser then sends with the handshake, so a normal player is usually fine; but **Bot Fight Mode / Super Bot Fight Mode** or a WAF rule that challenges unusual requests can still block `/play/ws` for some networks. Create a rule that skips challenges for `/play/ws` and `/play/status` (the Origin check, the limits and the caps are the protection there), and test from a phone on mobile data.
- **Rate limiting**: a Cloudflare rate-limiting rule on `/play/ws` (for example 30 requests per minute per address, block for a minute) is a good extra layer; the bridge's own limits do not depend on it.
- **Idle timeout**: 100 seconds with no data; the keepalive of section 2.7 sends a frame at least every 25 seconds.
- The telnet name `play.legendsofhollowmere.com` stays a **DNS-only** record. The web client must never be given that name (the browser page connects to the same origin as the page).

### 10.6 The update procedure

1. Merge. On the droplet, as root: `UPDATE_SOURCE=yes WEB_DOMAIN=legendsofhollowmere.com bash /opt/lesmudos/src/deploy/setup-website.sh` (publishes the site, rewrites the Caddyfile, reloads Caddy, restarts `lesmudos-web`). **This disconnects every web player** (they get "The website is restarting" and reconnect, retyping their password). Do it when few are online (`who` in the game); a content-only change (a news post: a Markdown file) needs no restart. For a deploy without dropping players, run the bridge as its own service (option (c), section 2.1).
2. For a change to the driver (4.x): the usual game update (`shutdown` in the game, then `bash /opt/lesmudos/src/deploy/update-droplet.sh`), which rewrites the game's unit with `--bridge-port`. The order for the first release: update the game (new listener), then the website (bridge on). In the other order the website would find nothing on 4001 and the page would say "The game is not running".
3. To turn the client off without a deploy: `touch /var/lib/lesmudos-web/play.off`; on: `rm` it.
4. Roll back: `Site__Play__Enabled=false` and a restart, or the kill switch.

## 11. Capacity on the 2 GB droplet

What one web player costs, with the figures that are estimates marked as such (the owner should measure them with the check at the end of this section):

| Part | Per player | Notes |
|---|---|---|
| Browser to Cloudflare to Caddy | one connection | Caddy about 20 to 40 MB in total whatever the count (estimate). |
| Caddy to Kestrel | one socket | |
| The bridge (website process) | a WebSocket (a few KB of buffers), a TCP client, two tasks, a bounded queue: typically under 100 KB, at most about 600 KB if the queue is full (estimate) | `MemoryMax=300M` on the website. |
| Bridge to game | one loopback socket | |
| Inside the game | one `Connection`, one login object then one player body and its items | Unknown: it is the same as a telnet player; nothing in the repository measures it. `MemoryMax=768M` on the game. |
| File descriptors | four sockets per player across Caddy, the website and the game | far below the limits of the unit files. |

So 60 players cost the website a few MB and the game whatever 60 telnet players cost. The cap of **60 web players** (`MaxConnections`) is a number chosen to be safely inside 300 MB for the website and to leave the game's 768 MB for the world, not a measured limit; the driver's own cap of 200 total connections covers telnet and web together. **When the site is full** the page says, in the log and above the button: "The game is full right now. Please try again in a few minutes." (from `sys full`, close 1013); `/play/status` reports `full:true` so the Enter button is replaced by "The game is full" before anyone tries; the page checks again every 30 seconds while it is open and enables the button when there is room. A player who is already in is never disconnected to make room. A telnet player is unaffected by the web cap and sees the driver's "The game is full" text only at 200.

To measure before raising the cap: with the game and the site running, open N web sessions with the Python harness (`tests/web_client_test.py --load N`), then read `systemctl status lesmudos-web lesmudos` (the `Memory:` lines) at N = 0, 20, 60, 120 and write the numbers into this section.

## 12. Test plan

### 12.1 Automated

**The website's .NET tests** (`web/LesMudOS.Web.Tests/PlayTests.cs`, `TelnetParserTests.cs`, with a small fake game: a `TcpListener` the test controls, to which the bridge connects; and `WebApplicationFactory`'s `WebSocketClient`):

- Handshake: allowed origin accepted; wrong, missing and `null` origins 403; wrong subprotocol refused; missing or malformed `X-Hollowmere-Client-Ip` 403 when required, accepted when not; a comma list or a port in the header refused; kill-switch file stops new connections without a restart; `Enabled=false` gives 404 and the explanation page.
- The fake game receives exactly one PROXY line first, with the claimed address, in TCP4 and TCP6 form, then nothing until the player types.
- Telnet to frames: `IAC WILL 201` answered `DO 201` and the `Core.Hello` and `Core.Supports.Set` messages sent; `IAC WILL ECHO` gives `mask true`, `WONT ECHO` gives `mask false`, in order with the text before them; other options refused; GMCP `Room.Info` becomes a `gmcp` frame with the JSON intact; an oversized GMCP message dropped; text and GMCP interleaved keep their order.
- **Parser fuzz**: the same recorded byte stream (a login, a room, 200 lines with colour, GMCP with `0xFF` doubled, multi-byte characters) cut at every possible single position and at random boundaries gives identical frames; random bytes never throw and never produce a control character other than `\n`, tab and ESC in an `out` frame.
- Input: a line is written with `\r\n`; embedded `\n`, `\r`, NUL refused; 4097 bytes refused with `too_long`; empty line allowed; a binary frame closes 1003; an 8193-byte frame closes 1009; 3 bad frames close 1008; `ping` answered `pong` and **never reaches the fake game** (the fake game's received bytes unchanged).
- Limits: the 6th connection from one address refused, the 61st upgrade gets `full` and 1013; connect-rate limit; input-rate limit drops lines and then closes; a client that does not read is closed at 256 KiB with the game side closed too; a client silent for `ClientSilenceSeconds` (set to 2 s in the test) is closed 1001; the fake game closing gives `game_closed` and close 1000; `ApplicationStopping` gives `restart` and 1012.
- **Logging**: the captured log contains the open and close lines with the address and **none** of the typed text or game text (the test types `hunter2-secret-marker` as a command and as a masked password and asserts it appears nowhere in the captured log output).
- The pages: `/play` has `connect-src` and `manifest-src` in its CSP, has **no** `<script>` without `src`, **no** `style=` attribute and no `<style>`, no inline event handler (`on...=`), loads no URL outside the site; `/how-to-play` has the old content (the existing `SiteTests` for the address, the Mudlet link and the notice move to it); `/play` is `noindex` and not in the sitemap; the nav and footer links; the manifest parses, has the fields of section 7.1, the right content type; `sw.js` has `Service-Worker-Allowed` and `no-cache`; `/play/status` is `no-store` and has no counts.
- **Static checks of the scripts**: `play.js` and `play-map.js` contain none of the forbidden words of section 3.5, make no `fetch` or `XMLHttpRequest` call except to `/play/status`, and open `WebSocket` only with a same-origin URL built from `location`.
- `PlayContrastTests` recomputes every pair of section 7.5 from the CSS variables in `play.css` as `ContrastTests` does for the site.
- **JavaScript unit tests** (`web/LesMudOS.Web.Tests/js/*.test.mjs`, run by `node --test` through one xUnit test that is skipped when `node` is not installed): `parseAnsi` (every SGR the game sends, `38;5;n`, unknown sequences, OSC, an unfinished sequence split across calls, a 100,000-character line, 10,000 escapes), the line model (prompt continuation, echo, `\n` handling, masked line adds nothing), history (walk, duplicates, masked lines excluded, 100 limit), the scrollback trim, the map layout (placement, collisions, jumps, named exits, doors), the settings loader (corrupt values reset to defaults).

**End to end in the existing Python style** (`tests/web_client_test.py`, stdlib only: a small WebSocket client of about 80 lines doing the handshake, masking and ping/pong; it starts the driver on a temporary copy of the mudlib with `--bridge-port` and the published website with `Site__Play__...` pointed at it, as `tests/gmcp_test.py` starts the driver; Caddy and Cloudflare are not involved, so the test sends `Origin` and `X-Hollowmere-Client-Ip` itself): connect and see the welcome; a new account through the masked password (the `mask` frames, the echo not present anywhere in the `out` frames); play a few commands and see the `Room.Info`, `Char.Vitals`, `Char.Status` frames match what `tests/gmcp_test.py` expects of telnet; `sync TOKEN` to find the end of a reply; the game's log shows `connection from <claimed address> (web)`; a `tell` between a web and a telnet player arrives as text and as `Comm.Channel.Text`; `quit` gives `game_closed` and 1000; killing the website closes the game connection and the character is saved; a second web connection with the same name is asked for the password and takes the character over (and a telnet player takes over from the web player and the reverse); the idle timeout with `LESMUDOS_IDLE_MINUTES=10s` and `LESMUDOS_TIMEOUT_MINUTES=20s` is **not** reset by 30 seconds of pinging, and the player is disconnected on schedule; 100 pings leave `LastInputMs` untouched (observed through `who`'s idle); the driver's line-rate and queue caps with a raw socket; a flood of 70 connections shows the bridge refusing and the game healthy; `--load N` mode for section 11.

### 12.2 What the existing tests need

`SiteTests.cs`: `Fixed` loses `/play` and gains `/how-to-play` (the page list test that expects each to be indexable), the Mudlet and address tests point at `/how-to-play`, a new test for `/play`. `tests/return_test.py` keeps parsing `connection ended:` unchanged. Any Python test that opens more than 10 loopback connections at once is unaffected by the per-address cap (loopback is exempt).

### 12.3 Manual checklist (the owner, before announcing it)

For each of desktop Chrome, Firefox, Safari, and iOS Safari, Android Chrome (and the installed home-screen app on both phones):

1. Page loads, no console errors, no blocked-by-CSP messages, no request to any other host (Network tab).
2. Enter: login screen appears; type a name; the password field is masked and nothing is echoed; wrong password retry is masked again; a new account is created; a calling chosen.
3. Walk with the keyboard and with the exit buttons; the text, the room name, the exits and the bars agree; a fight shows the badge; `rest` shows it.
4. Output: colours match the table (try `ansi off`, then on); a long run of output keeps up; scroll up during a fight and see "new lines below"; the marker button returns to the end; the scrollback limit trims without a jump (Safari).
5. `tell` another player, `shout`: the chat pane has it; say a line with `<b>x</b>` and `http://example.test` in it: it appears as plain text, no link.
6. Close the tab mid-game: the character leaves as `quit` does today (or lingers 60 s once that feature ships); reopen and log in.
7. Turn off the Wi-Fi for 20 seconds: banner, retry, login again; on the idle timeout: warning text, then Reconnect button.
8. Leave the page open without typing for 11 minutes: it is idle at 5 and disconnected at 10 even though pings continue (this proves the keepalive is not input).
9. Phones: portrait and landscape; the keyboard opens and closes without the page jumping; the input stays visible and keeps focus after sending; no horizontal scroll at 320, 360, 390, 412 px; the score sheet (the 48-column box) is aligned and complete on both phones (the monospace box-character check, section 1); tap a word, an exit, a command button, repeat; font size A- and A+; lock the phone for 30 seconds and unlock: it reconnects; with "Keep the screen on" the phone does not lock by itself.
10. Install to the home screen (iPhone: Share, Add to Home Screen; Android: Install app): opens without the address bar, icon is right, offline start shows the offline message; after a deploy the installed app loads the new version at the next online start.
11. Accessibility: VoiceOver (iOS) and TalkBack read new lines and not the echo; pause and "Messages only" work; keyboard only on desktop; 200% zoom; the High contrast and Light themes; reduced motion on.
12. Behind Cloudflare: the game logs the **phone's real address**, not a Cloudflare one (compare with a "what is my IP" page); the socket survives 5 minutes of silence (the 100-second limit); Bot Fight Mode does not block it from mobile data; a website restart reconnects the page; `touch /var/lib/lesmudos-web/play.off` stops new connections; the full message appears at a cap lowered to 2.
13. Sleep a laptop with the page open for a minute and wake it: reconnects.

## 13. Staged build order

Each stage is merged and playable, and leaves the previous stage's behaviour unchanged. Sizes are rough lines of new code (without tests) and the main risks.

| Stage | What | Rough size | Notes |
|---|---|---|---|
| **1. Transport and a plain text client** | Driver: bridge listener and PROXY line (4.1), connection limits (4.2), input rate limit (4.3), output cap (4.4), settings, deploy flag. Website: `PlayOptions`, `/play/ws`, `/play/status`, the bridge, the telnet parser, the gate, frames, the kill switch, CSP and headers, `/how-to-play` move and link changes, `robots`. Client page: connect button, output pane with ANSI, input with history and local echo, masked password, reconnect with back-off, scroll lock, plain-text fallback, how-to-play strip, accessibility basics (`role="log"`, labels, focus). Deploy script and Caddyfile changes, Cloudflare checklist. GMCP is negotiated and forwarded but the page ignores it. | driver about 250 lines; website C# about 600; JS about 700; CSS about 200; tests about 1500 | The largest stage and the one with the risk (the Caddy and Cloudflare chain, the true address). Everything else builds on it. Ends with: the owner logs in from a phone browser and plays. |
| **2. Status panel and exits** | GMCP-driven panel (identity, three bars, badges, room, area, light badge ready), compass buttons and other-ways row, the number pad option, the keyboard shortcuts, the rest of the accessibility (announcement modes, `role="meter"`). | JS about 450; CSS about 150; tests about 300 | No server change. |
| **3. Phone layout and install** | One-column layout, visual-viewport handling, exits strip, command-button row and its editor, tap-a-word, repeat, font fitting and A-/A+, wake lock, themes and the high-contrast theme, settings dialog with storage switches, manifest, icons, service worker, `beforeunload` in a fight, bfcache handling, Web Locks courtesy, the Add to Home Screen hint. Optional here: the NAWS step (6.4). | JS about 600; CSS about 250; manifest and worker about 120; icons tool about 60; tests about 300 | The manual phone checklist is the real test; budget time for iOS Safari quirks. |
| **4. Chat pane and map** | `Comm.Channel.Text` pane with unread count; `play-map.js` (layout, drawing, panning, persistence, clear); the `light` field when it exists. | JS about 700; CSS about 100; tests about 400 | The map is the part most likely to need a second pass after real play: a greedy layout never rearranges. |

## 14. Out of scope and future

- **Native phone apps** (agreed: the installed web page is the phone app) and a custom desktop client (Mudlet covers it).
- **Push notifications** (a tell while the phone is asleep): needs the service worker, a push server and a way to keep a character present while the socket is dead; not possible without token resume.
- **Sound** (no audio at all in the first versions).
- **Token resume / "stay signed in"**: section 8.5.
- **Account forms and in-page registration**: section 9.1.
- **Clickable links** in game text (Decision 9) and **tap to target** from output.
- **A skills or spell bar** filled from the game: needs a GMCP package for skills (`Char.Skills`), which does not exist.
- **A target or enemy health bar**: needs the game to send the enemy in GMCP; `Char.Vitals` has only `fighting`.
- **A login-attempt throttle** per address in the driver (3 wrong passwords per connection today, unlimited reconnects), and a ban list using `Connection.ClientIp`.
- **Restricting ports 80 and 443 to Cloudflare's ranges** on the droplet.
- **The NAWS width step** (6.4) if column lists look bad on phones.
- **The separate bridge service** (option (c)) if website deploys disturb players.
- **A reusable client for other LesMudOS games** (it would move from `web/`, which is the game's, to `clients/web/`, the engine's, see `LICENSING.md`).
- **Translations** and a touch-first redesign beyond the single column.
