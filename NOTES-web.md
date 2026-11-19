# NOTES-web (resumable state)

Branch work/web, worktree /home/claude/wt-web. Task: public website under web/ (see docs/specs/website.md, docs/WEBSITE.md).

## Done
Project, data layer, secrets filter, all pages, CSS, SVG art, 42 tests passing, deploy/setup-website.sh (with sync job), docs/WEBSITE.md, LICENSING/README.

## Decisions
- Website user never reads players/*.json: sync job (in setup-website.sh) writes sanitized roster.json etc.
- Entries naming forbidden terms are hidden, so Overseer / Clear the Cellar entries vanish (they name the Forgotten Cellars).
- Font: IM Fell English regular (OFL), font-display optional.

## Next
Nothing required; final report only.
