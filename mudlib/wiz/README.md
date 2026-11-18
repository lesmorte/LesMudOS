# /wiz: wizard home directories

Every wizard (wizard level 1 or higher) has a home directory `/wiz/<name>`,
where `<name>` is the player's name in lower case. The game creates it
automatically when a player becomes a wizard and when a wizard logs in.

In your home, `~` stands for it (`cd ~`, `list ~/rooms`, `edit ~/rooms/hall`),
and `cd` with no argument goes there. You may create, edit, copy, move,
rename, delete and remove anything under your home, and `update`, `clone` and
`goto` the rooms, creatures and items you write there, at any wizard level.

Other wizards' homes can be read by every wizard; changing them needs wizard
level 5. The home directory itself, `/wiz`, and files placed directly in `/wiz`
can be changed only by a level-10 wizard.

Files that wizards create here are game content and are not part of the
repository's build: `Mudlib.csproj` leaves `wiz/**` out.
See docs/DRIVER_API.md section 6 and docs/BUILDING.md.
