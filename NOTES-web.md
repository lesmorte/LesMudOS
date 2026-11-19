# NOTES-web (resumable state)

Branch work/web, worktree /home/claude/wt-web. Task: public website under web/ (see docs/specs/website.md).

## Decisions
- Reader never touches players/*.json in production: a sync job (deploy/lesmudos-web-sync, run as the game user) writes a sanitized
  directory (chronicle.json, renown.json, wizlist.json, roster.json without password hashes) that the website user reads.
  Reader also accepts a raw save dir (secure/*.json, players/*.json) for local dev and tests.
- Font: IM Fell English (OFL) regular only, 56 KB, in wwwroot/fonts. Body: system sans.
- Real fixtures captured with web/tools/capture_fixtures.py -> web/fixtures/real (copied from /tmp/webfix-real).
- Entries whose text/headline/area contain a forbidden term are hidden (so Overseer / Clear the Cellar entries vanish: they name the Forgotten Cellars).

## Done
(see git log)

## Next
- project, data layer, pages, css, svg, tests, deploy script, docs
