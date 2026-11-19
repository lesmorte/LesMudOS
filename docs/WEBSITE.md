# The public website

The owner's brief is [specs/website.md](specs/website.md). This page says how the website is built, run, configured and deployed. The website is part of the game (all rights reserved), not of the engine: see [../LICENSING.md](../LICENSING.md).

## What it is

An ASP.NET Core (.NET 10) Razor Pages site, server-rendered, with no JavaScript framework and no database. Everything is under `web/`:

| Path | What |
|---|---|
| `web/Web.sln` | The website's own solution. It is not part of `LesMudOS.sln`, so the game's build never builds the site. |
| `web/LesMudOS.Web/` | The site. `Pages/` are the pages, `Data/` the read-only data layer and the secrets filter, `wwwroot/` the stylesheet, font, icons and one 40-line script. |
| `web/LesMudOS.Web/publicfilter.json` | The secrets filter (below). |
| `web/content/world.json` | The copy for `/classes` and `/factions`. |
| `web/content/news/*.md` | The news posts. |
| `web/LesMudOS.Web.Tests/` | xUnit tests (in-memory test host). |
| `web/fixtures/` | Save files for tests and local runs: `real/` (written by the real driver), `mixed/` (hand-built in the same shape, with hidden discoveries, a wizard and traps), `synced/` (what the deployment's sync job makes of `mixed/`). |
| `web/tools/` | `capture_fixtures.py` (plays a few commands on a real driver and copies its save files), `make_mixed_fixture.py`, `make_art.py` (draws the two SVG illustrations). |

Pages: `/`, `/chronicle` (newest first, or `?view=top` for most significant; paged with `?page=N`), `/chronicle/{id}`, `/heroes`, `/world`, `/classes`, `/factions`, `/news`, `/news/{slug}`, `/play`, `/rules`, `/support`, plus `/sitemap.xml`, `/robots.txt`, `/healthz` and a 404 page. There are no forms, no cookies, no analytics, and nothing is loaded from another host (the Content-Security-Policy forbids it).

## Running it locally

```
export PATH=/opt/dotnet:$PATH            # wherever .NET 10 is
dotnet build web/Web.sln
dotnet test web/Web.sln
cd web/LesMudOS.Web && dotnet run         # http://localhost:5090, reading ../fixtures/mixed
```

In Development the site reads `web/fixtures/mixed`. To see a real game's data, point it at the game's save directory: `Site__SaveDirectory=/path/to/save dotnet run`. The reader understands both layouts (the game's own `secure/chronicle.json`, `secure/renown.json`, `secure/wizlist.json`, `players/*.json`, and the flat sanitized copy described under Deployment).

## Configuration

Set in `appsettings.json`, by environment variables (`Site__GamePort=4000`), or on the server in `/etc/lesmudos-web.env`.

| Key | Default | Meaning |
|---|---|---|
| `Site:BaseUrl` | `https://legendsofhollowmere.com` | Canonical links, Open Graph, sitemap |
| `Site:SaveDirectory` | empty | Where the data is read from (read only). Empty or missing: every dynamic section shows its empty state |
| `Site:ContentDirectory` | `content` | News and `world.json`. A relative path is looked up under the content root, then in `../content` |
| `Site:FilterFile` | `publicfilter.json` | The secrets filter |
| `Site:GameHost`, `Site:GamePort` | `legendsofhollowmere.com`, `4000` | Shown on `/play` |
| `Site:DiscordUrl` | empty | The Discord link in the footer and on `/support`. Empty: no link anywhere |
| `Site:SupportContact` | empty | The contact line on `/support` (an address, for example). Empty: "A contact address will be posted here soon." |
| `Site:CacheSeconds` | `45` | How long the game's files are cached (30 to 60 is right) |
| `Site:ChroniclePageSize` | `12` | Entries per page on `/chronicle` |

## The data layer

`Data/SaveReader.cs` reads the game's JSON read-only (shared access, so the game can write at the same time) and returns nothing at all for a file that is missing, empty, too large, not JSON or cut short. `Data/GameData.cs` builds one cached snapshot from the Chronicle, the Renown ledger, the wizard list and the characters. It never throws to a page. If a file cannot be read this time it falls back to the last good copy of that file, and with none the section is empty ("The Chronicle's first page is still blank. Yours could be the first name on it."). No entry or number is written in the code or the pages.

What is read, and where it comes from (the shapes are in `mudlib/secure/chronicle.cs`, `renown.cs`, `wizlist.cs`, and `src/LesMudOS.Driver/SaveStore.cs`):

| Shown | From |
|---|---|
| Entry title, text, significance, score (`ess`), date, category, class, featured, names | `chronicle.json` `entries[]`: `headline`, `text`, `significance`, `ess`, `date`, `category`, `classname`, `featured`, `participants` |
| Today's date in Hollowmere | `chronicle.json` `epochDay`, with the calendar ported from the game (`Data/HollowmereCalendar.cs`; one real day is one day, twelve months of thirty days, then the Turning) |
| Heroes: name, tier, title | `renown.json` `renown`, `titles`, `activeTitle`. The tier comes from the game's thresholds. The title is the active title, else the newest earned one |
| Heroes: calling or class | the character: `archetype` and `advancedClass` |
| Characters created | the number of characters (wizards excluded). Omitted if not computable or zero |
| Wizards (never shown) | `wizlist.json` `levels` |

Heroes are the top ten by Renown among characters that still exist (Renown outlives a deleted character, so a name with no character is skipped), excluding wizards.

## The secrets filter

The Chronicle records discoveries of hidden places and secret recipes, and their titles name them. The public site must never show them. The rules live in one file, `web/LesMudOS.Web/publicfilter.json`, and an entry is public only if all of these hold:

1. Its event id does not start with any of `hiddenEventPrefixes`: `discover.hidden`, `discover.secret_dungeon`, `discover.great_stump`, `craft.discover_`. Deny always wins.
2. Its event id is on the `publicEvents` list (exact, or a prefix when it ends in `*`). **An event type that is not listed is hidden.** A secret event type added to the game later therefore stays hidden until someone lists it.
3. None of the `forbiddenTerms` appears in its headline, text, significance, place, region, tags, subject or event id (case and punctuation ignored, so `great_stump` equals "Great Stump").
4. It names no wizard, and no wizard's name appears in its text.

An entry with no event id is hidden. A place name is shown on an entry only if it is in `publicPlaces`. If the file is missing or unreadable, nothing is public.

The default public list is `aggregate.goblin_purge`, `boss.dungeon`, `boss.goblin_overseer`, `boss.world`, `class.advanced`, `class.prestige`, `item.legendary`, `item.artifact`, `level.milestone`, `quest.local`, `quest.clear_the_cellar`, `quest.story`, `quest.overseers_orders`, `renown.*`, `travel.pass`. `discover.area` is deliberately not on it (an area's name can be a secret).

**Making a new event type public.** Add its id (or `prefix.*`) to `publicEvents` in `publicfilter.json` and restart the site. Check first that its headline and text name nothing the lore decisions ([areas/corridor-lore-decisions.md](areas/corridor-lore-decisions.md)) keep secret. If a type's text can mention a hidden place, add the place to `forbiddenTerms` instead: the entry is then hidden whatever its type.

**Known consequence.** The game's templates for `quest.clear_the_cellar` and `boss.goblin_overseer` name "the Forgotten Cellars" (directly, or through the area name), and that name is forbidden, so those entries are hidden from the website. If the owner wants them public, the templates in `mudlib/events/` should say "the cellars beneath the Hollowman's Rest" and the area should be left out; the filter needs no change.

Wizards: the site reads the wizard list and also drops every entry that names a wizard. The Circle of Ash is never named on the site, in copy, alt text, metadata or comments; the only files that name it are the filter's own configuration (which must, to forbid it) and the tests.

The tests (`SiteTests.cs`) scan every rendered page, the sitemap, the static files, the page sources, the copy and the news for the forbidden terms, using fixtures that contain such entries.

## Copy and news

The words on the pages are in the `.cshtml` files under `Pages/`, except the callings, class paths and factions, which are in `web/content/world.json`. Only public information belongs there.

**Posting news is adding a file** to `web/content/news/`, named `YYYY-MM-DD-some-slug.md`:

```
---
title: A headline
date: 2026-11-18
summary: One sentence for the list and for link previews.
---
The post, in Markdown. Raw HTML is not allowed.
```

Optional header fields: `slug` (default: the file name without the date) and `draft: true`. A post dated in the future stays hidden until that day. A malformed file is skipped. On the server, put the file in the repository and run `deploy/setup-website.sh` again, or drop it into `/opt/lesmudos-web/content/news/` (read within a minute).

## Design

"Greenvale frontier fantasy": a parchment ground with restraint, ruled lines and ornaments an old gazetteer might have. Palette: forest green (masthead), warm brown and dark oak (text, footer), parchment, copper (accents and the main button). Headings are set in IM Fell English (Igino Marini, SIL Open Font License; regular weight only, Latin subset, 56 KB, self-hosted with its licence in `wwwroot/fonts/`); the body is the system sans-serif. The Chronicle reads like a chronicle (the date in the margin, a drop cap on the entry), the heroes like a roll of honour. The two illustrations are original SVG drawn by `web/tools/make_art.py`: a forest road leading from a village toward the walled city, and an old-style map of Greenvale that shows only public places and fades into "unknown" at its edges. Nothing grimdark, neon or sci-fi.

Mobile first (designed at 360 px), no layout shift (the font is `font-display: optional` and preloaded, images are inline SVG with fixed aspect ratios), semantic landmarks, a skip link, one `h1` per page, visible focus, reduced motion respected, and no information by colour alone. Text contrast is checked numerically by a test (`ContrastTests.cs`); the test also writes the table to `hollowmere-contrast.txt` in the temp directory.

Contrast, WCAG 2 ratios (AA needs 4.5, the body text is held to 7):

| Use | Text | Background | Ratio |
|---|---|---|---|
| body text on the page | ink #2b1d12 | parch #f3e9d0 | 13.50:1 |
| body text on alternate bands | ink #2b1d12 | parch-2 #eadcb8 | 11.99:1 |
| body text on panels | ink #2b1d12 | card #faf4e3 | 14.85:1 |
| secondary text | ink-2 #5a432c | parch #f3e9d0 | 7.64:1 |
| secondary text on bands | ink-2 #5a432c | parch-2 #eadcb8 | 6.78:1 |
| secondary text on panels | ink-2 #5a432c | card #faf4e3 | 8.41:1 |
| headings | oak #3b2a1a | parch #f3e9d0 | 11.34:1 |
| headings on bands | oak #3b2a1a | parch-2 #eadcb8 | 10.07:1 |
| headings on panels | oak #3b2a1a | card #faf4e3 | 12.48:1 |
| links | link #2a5a2d | parch #f3e9d0 | 6.69:1 |
| links on bands | link #2a5a2d | parch-2 #eadcb8 | 5.94:1 |
| links on panels | link #2a5a2d | card #faf4e3 | 7.36:1 |
| eyebrows, dates, numerals | copper #8c4519 | parch #f3e9d0 | 5.83:1 |
| eyebrows on bands | copper #8c4519 | parch-2 #eadcb8 | 5.18:1 |
| badges on panels | copper #8c4519 | card #faf4e3 | 6.41:1 |
| masthead text | parch #f3e9d0 | green #2f4a2b | 8.14:1 |
| masthead hover | gilt #e8c98a | green #2f4a2b | 6.16:1 |
| primary buttons, Play Now | card #faf4e3 | copper #8c4519 | 6.41:1 |
| primary button hover | copper #8c4519 | card #faf4e3 | 6.41:1 |
| active tab | card #faf4e3 | oak #3b2a1a | 12.48:1 |
| footer text | parch #f3e9d0 | oak #3b2a1a | 11.34:1 |
| footer links | gilt #e8c98a | oak #3b2a1a | 8.59:1 |
| map wood label | green #2f4a2b | parch #f3e9d0 | 8.14:1 |
| plate captions | ink-2 #5a432c | card #faf4e3 | 8.41:1 |


## Deployment

`deploy/setup-website.sh` is run as root on the droplet after `setup-droplet.sh`, with the domain's DNS pointing at the droplet:

```
WEB_DOMAIN=legendsofhollowmere.com bash /opt/lesmudos/src/deploy/setup-website.sh
```

It is safe to run again, which is also how the site is updated (`UPDATE_SOURCE=yes` fetches the latest source first). **It has not been run on a server.** It was checked with `bash -n`, and the sync job inside it was run against the fixtures.

What it does:

- Publishes the site to `/opt/lesmudos-web`, owned by root.
- Creates the `lesmudos-web` user (no login) and a systemd unit `lesmudos-web` bound to `127.0.0.1:5080`, confined like the game's unit, with `InaccessiblePaths` on the game's save directory.
- **Data access.** The game's save directory is mode 750 owned by the game's user, and `players/NAME.json` holds the password hash next to the level and calling, so the website user is given *no* access to it. A timer (`lesmudos-web-sync`, every 30 seconds) runs as the game's user and writes a sanitized copy into `/var/lib/lesmudos-web/data` (mode 2750, group `lesmudos-web`): exact copies of `chronicle.json`, `renown.json` and `wizlist.json` (checked to be whole JSON first), and a `roster.json` with each character's name, level, calling and advanced class, and nothing else. The only changes to existing permissions: it makes sure the save directory is still 750 and that the website user is not in the game's group. Password hashes never reach the website's user.
- Installs Caddy from its official apt repository as the public web server with automatic HTTPS for the domain, a permanent redirect from `www`, and compression. An existing Caddyfile is saved once as `/etc/caddy/Caddyfile.before-hollowmere`.
- Opens ports 80, 443/tcp and 443/udp in `ufw`.
- Creates `/etc/lesmudos-web.env` once (Discord link, contact line, game host and port); edit it and `systemctl restart lesmudos-web`.

Needs: `setup-droplet.sh` already run; DNS for the domain and `www` pointing at the droplet (Caddy's certificates fail otherwise); `python3`, which the script installs.

## What is not built

- A web client. The game is played over telnet; `/play` explains how.
- Accounts or sign-up on the web. Accounts are made in the game at first login. The site collects no personal data.
- Discord: only a link, shown when `Site:DiscordUrl` is set.
- A downloadable client: shown as "coming later", visibly not a link.
- Character pages, search, comments, and a staff area.
- The rules page is a draft for the owner to edit.
