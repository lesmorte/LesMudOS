# The login throttle

Status: **built** (November 2026, branch `work/net-limits`; merged with the sandbox branch on `work/net-int`, which changed the address lock as described under "The owner behind a locked address"). Not part of [web-client.md](web-client.md); the owner asked for it because the game is on the internet: until now a connection had 3 password tries and reconnecting was free, so a password could be guessed without limit. Related: [web-client.md](web-client.md) section 4 (the connection limits the throttle stands behind), [../DEPLOY.md](../DEPLOY.md) (the settings).

## What it does, in one paragraph

Wrong passwords are counted per character name and per address. Five wrong passwords on one name within 15 minutes lock that name for 5 minutes; forty from one address, across any names, lock the address for 15 minutes; each lock doubles the next one up to a ceiling. A refusal always says the same thing, and every "no" comes after the same one-second pause. A name that is locked, or an address that is locked, still lets in a connection from an address its owner has used before, for that character only, with three tries of their own. A success wipes a name's count only when it comes from an address that had not failed on that name. New characters are limited to 3 per address per hour and 10 per day.

## Where the code is

| Part | File |
|---|---|
| The counting and the decisions (in memory) | `mudlib/secure/throttle.cs` (blueprint only, preloaded) |
| The asking and the delay | `mudlib/secure/login.cs` (thin calls: `Throttle().Call(...)`) |
| The last three addresses of a character | `mudlib/std/player.cs` (`knownAddresses`, `KnowsAddress`, `NoteAddress`) |
| The wizard command | `mudlib/std/player.cs` (`CmdThrottle`) and `Throttle.Command` |
| The driver side (four efuns and the settings) | `src/LesMudOS.Driver/KernelNet.cs`, `Net/ThrottleSettings.cs`; `MudObject.cs`, `Contracts.cs`; permissions in `mudlib/secure/master.cs` |
| Test | `tests/throttle_test.py` |

## The address

The login object needs the address of its own connection, and until now no mudlib code could read one. A new efun, `ConnectionAddress()`, returns it as the driver counts it: the IPv4 text, or for IPv6 the **/64 prefix** (`2001:0db8:0077:0000::/64`); for a web player, the true address the website's bridge gave in its PROXY line (web-client.md 2.6), not `127.0.0.1`. The master allows the call only to a **clone of `/secure/login`**, and the efun takes no argument, so it answers only about the login object's own connection. A wizard's code that calls it gets an error. There is no efun that lists connections or reads another player's address. Two more efuns exist for the throttle alone (`AddressTag`, `ThrottleSetting`, plus `ThrottleLog`); the master allows them only to the `/secure/throttle` blueprint.

IPv6 is counted by /64 everywhere (counts, locks, known addresses): one subscriber normally owns a whole /64 and can change the rest of the address at will.

## The rules, as built

All numbers are settings (section "Settings"); the values below are the defaults.

**Per name.** Each wrong password on an existing character is recorded with its time and the address it came from. When 5 are inside the last 15 minutes the name is **locked for 5 minutes**, the count is cleared, and the connection that made the fifth is ended ("Wrong password. Goodbye."). Each further lock of the same name doubles the time (5, 10, 20, 40, then 60 minutes at most). A name whose record has been quiet for 24 hours starts again at the first step.

**Per address.** Each wrong password is also recorded against the address. 40 inside 15 minutes lock the **address for 15 minutes**, doubling per lock up to 4 hours (the default was 20 and was raised: an address is often shared by a household, a school or a carrier, and a lock must not cost a real owner more than the guesser). A locked address is not turned away at connect. It gets the name prompt, and then: a name of a character whose known addresses include this one gets the password prompt under the strict known-address limit (below); every other name, existing or not, gets the plain refusal after the usual pause, and a new account cannot be started from it. So the words and the timing are the same for every name that is refused, and nothing is learned about which names exist.

**What a success does.** A correct password wipes the failure record of that name (and lifts its lock) **only if the address had not failed on that name**: the record keeps the set of addresses that failed. So an attacker who guesses four wrong passwords and then logs into his own other character, or even into the victim's account, resets nothing; the real owner coming from a clean address does. A success never changes an address's own count: logging into one's own account is not a way to buy more guesses from that address.

**The owner behind a locked name or a locked address.** The character's save keeps the last three addresses it was logged into successfully, each as a keyed hash (below). While a name is locked, or the address is locked, a connection from one of those addresses is not refused for that character: it gets the password prompt, with **its own limit of three wrong passwords** (per name and address, in the same 15 minute window); after the third that address is refused as well until the lock ends. Those failures count toward the address's own count but not toward the name's, so the owner's typos can neither lengthen the lock nor trigger the next doubling. Only the character's own known addresses get this: someone at a locked address who is not the owner of the name he types is refused as before.

*The trade-off, stated plainly.* The exception exists so that a locked name cannot be used to keep its owner out: without it, anyone could type five wrong passwords at "Vera" every five minutes and Vera could never play. With it, a person who shares an address with the owner (a household, a school, a mobile carrier's gateway, or another customer in the same IPv6 /64) gets three more guesses at that password per window, and an attacker who has taken over a machine at an address the owner used gets the same. It does not help anyone else. The three tries are the same three tries a connection always had; the exception only stops the lock from removing them from the owner. The list holds three addresses, the newest replacing the oldest, so an owner who moves between more than three places is locked out of the exception, and waits out the lock (at most an hour).

**New characters.** At most **3 new characters per address per hour and 10 per day**, counted when the password is set (the moment a name becomes reserved). The check runs when a new name is typed and again before the password is stored. The refusal is a plain sentence ("Too many new characters have been made from your address. Try again later.") and closes the connection. Existing characters are unaffected.

## What a refused player sees, and the pause

- The fifth wrong password on a name (or the failure that locks an address) ends that connection with "Wrong password. Goodbye." and, on the next line, "Too many wrong passwords on this name for now. If it is your character, wait a few minutes and try again." That is said only to the connection that made the failures, so it tells a typing owner what to do and tells a guesser nothing he does not know. The third wrong password of a connection says only "Wrong password. Goodbye." as before.
- Every refusal (locked name, locked address, a known address that used its tries) prints exactly **`Too many attempts. Try again later.`** The words do not say whether the cause was the name or the address, how long is left, or whether the name exists.
- Every "no" is answered **after the same pause**, 1 second (`LESMUDOS_THROTTLE_FAIL_DELAY_MS`): a wrong password, a refusal at the name prompt, a refusal at connect. It is a call-out on the login object, not a sleep: the game goes on. What the visitor types during the pause is swallowed (the prompt is echo-off, so nothing shows). The pause also slows an attacker who reconnects by about a second per guess.
- What the pause does and does not hide. A *refusal* and a *wrong password* take the same time, so a visitor cannot tell a locked name from a wrong password by timing. A name that **does not exist** is not a "no": the game today goes straight to "Creating a new account for X", with no pause, so that the account-existence question is answered by the prompt that follows the name, as before. That is existing behaviour of the login flow, not something the throttle adds, and it cannot be closed without changing how accounts are created (section "Not built").
- The refused connection is ended (with the same message) after the pause; a locked address never sees the name prompt.

## Hashed addresses

The addresses in a character's save are **never stored raw**. The driver keeps a random 32-byte secret in `<save>/address-secret` (created on first use, mode 0600) and computes `HMAC-SHA256(secret, key)`, keeping 16 hex characters (64 bits). The mudlib never sees the secret; `AddressTag` is the only way to use it and it is refused to everything but the throttle. Losing or replacing the secret file only makes the saved "known addresses" stop matching (the locked-name exception then does not apply until the next logins have refilled the list); nothing else depends on it. The tag is 64 bits of a keyed hash: someone who has the save files and also the secret file could check a guessed address against it, which is why the secret lives apart from the saves' normal readers (a wizard's code cannot read `<save>/address-secret`: the file is not under the mudlib or `/players`).

## Logging

One line per lock, rate-limited (20 lines a minute for the throttle, with a "left out" note after a burst), in the driver log:

```
login throttle: name locked: Vera (from 203.0.113.7) for 300 s
login throttle: address locked: 203.0.113.7 for 900 s
login throttle: Vera cleared by Alice          (the wizard command)
```

Passwords, guesses and anything typed are never logged, kept in memory, or passed to the throttle (the throttle receives names and addresses only). Successful logins were already logged ("connection from", "connection ended"). A settings line is logged at boot (`login throttle: default settings` or the changed ones).

## The wizard command

Level 10 only (below it the verb does not exist and is not in `help`). `throttle` lists current locks (names and addresses, time left, how many times locked) and counts; `throttle clear NAME` lifts a name's lock and forgets its count and its known-address tries; `throttle clear ADDRESS` does the same for an address (write the address as the list shows it: IPv4 text, or the `…::/64` form) and forgets its new-character count; `throttle clear all` empties the tables. Each clear is logged with the wizard's name.

## Memory

Everything but the salt is **in memory and lost at a restart**. That is acceptable because: a lock is minutes to an hour long; a restart is the owner's action, not an attacker's; and an attacker who could force restarts has bigger levers. What it costs: a restart gives every locked name and address a clean slate (an attacker who knows when the game restarts gets a fresh set of guesses). The tables are bounded (20,000 records each, quiet ones swept at least once a minute); at the bound the oldest quiet records go, and if every record is live new failures are not recorded (the connection limits of web-client.md 4.2 still apply). Nothing in memory identifies a player beyond a name and an address key.

## Settings

Environment variables, read once at boot (`ThrottleSettings`); the mudlib can read these fourteen values and no other environment variable. A bad value is ignored with a log note. `0` switches that limit off. Times are seconds.

| Variable | Default | Meaning |
|---|---|---|
| `LESMUDOS_THROTTLE_NAME_FAILURES` | 5 | wrong passwords on a name that lock it |
| `LESMUDOS_THROTTLE_WINDOW_SECONDS` | 900 | the window those are counted in (also for the other counts) |
| `LESMUDOS_THROTTLE_NAME_LOCK_SECONDS` | 300 | first lock of a name; doubles each time |
| `LESMUDOS_THROTTLE_NAME_LOCK_MAX_SECONDS` | 3600 | the longest name lock |
| `LESMUDOS_THROTTLE_RESET_SECONDS` | 86400 | quiet time after which a record starts again |
| `LESMUDOS_THROTTLE_ADDRESS_FAILURES` | 40 | wrong passwords from an address that lock it |
| `LESMUDOS_THROTTLE_ADDRESS_LOCK_SECONDS` | 900 | first lock of an address; doubles |
| `LESMUDOS_THROTTLE_ADDRESS_LOCK_MAX_SECONDS` | 14400 | the longest address lock |
| `LESMUDOS_THROTTLE_KNOWN_TRIES` | 3 | wrong passwords a known address may make on a locked name |
| `LESMUDOS_THROTTLE_FAIL_DELAY_MS` | 1000 | the pause before every "no" |
| `LESMUDOS_THROTTLE_NEW_PER_HOUR` | 3 | new characters per address per hour |
| `LESMUDOS_THROTTLE_NEW_PER_DAY` | 10 | and per day |
| `LESMUDOS_THROTTLE_NEW_HOUR_SECONDS` | 3600 | the "hour" (tests shorten it) |
| `LESMUDOS_THROTTLE_NEW_DAY_SECONDS` | 86400 | the "day" |

The shared test client start-up (`tests/smoke_test.py`) switches the name and address limits, the creation limits and the pause off for every other suite, because they make dozens of characters from one address and type wrong passwords on purpose; `tests/throttle_test.py` sets its own short windows (a 10 second name lock, a 6 second address lock, a 40 second "hour").

## Behind the website

A web player's address is the one Caddy and the bridge pass in the PROXY line, so the throttle sees the visitor, not Cloudflare. If the website's side is misconfigured so that every visitor looks like one address, the throttle will lock that one address after 20 wrong passwords and keep every web player out for 15 minutes. That is the safe direction (it never lets anyone claim another's address) and it is visible in the log (`address locked: <Cloudflare address>`), but it is the first thing to check if "Too many attempts" starts appearing for everyone.

## Built: a name that is online is asked for its password, and the right one takes the body over (taking over)

Owner (27 November 2026): "When I attempt to login it says my character is already here. Another name? Instead of the password loop." A player whose old connection is still open (a second client window, or far more often a half-open connection: the laptop slept or the mobile network dropped and the server has not noticed, which lasts until the idle timeout) was locked out of their own character, and the words also told anyone who typed a name whether that player was online. Now, as on most MUDs:

- **The name prompt does not look at who is online.** `GotName` (`mudlib/secure/login.cs`) no longer tests for an online body. A name that is online goes through exactly the steps of an offline existing name: `BeginLogin`, the throttle's `BeginPassword`, "Password: ". Same words, same work, so the same timing (tested: the two prompts are identical, and arrive within a second of each other). The address-locked rule, the locked-name rule and the known-address rule apply to an online name as to any other, and a refusal is the same plain refusal.
- **A wrong password** is counted like any other (`Failed`: the name's count, the address's count, the lock) and is answered like any other. **Nothing is said to the connected player.** An attacker could otherwise spam a character's owner with "Someone failed to log in as you" by typing wrong passwords, and the message would tell the attacker the name is online. (The owner can see failed guesses in the throttle's lock lines in the log and, as a wizard, with `throttle`.)
- **A right password** is a success for the throttle (`Succeeded`, `NoteAddress` on the character), and in `EnterGame`, **at the moment the connection is handed over** (not when the name was typed: the body may have appeared or gone while the visitor typed, or while they chose a calling), the login object looks for a real player body of that name that has a connection (`FindOnline`). If there is one, it **takes it over**:
  1. the body is asked (`BeginTakeover`, only from the login object) and tells its old connection one line, "Your character has been taken over from another connection.";
  2. the new login's cloned body is thrown away (one body per name: no second body, no second arrival, so no aggressive creature rolls again and the room hears no "enters the game");
  3. the driver moves the connection (`TakeOver`, `src/LesMudOS.Driver/Kernel.cs`, allowed by the master's `takeover` operation to the real `/secure/login` clone for a real player body only): the old connection is detached from the body first, so nothing it sends reaches the body and its closing does not run `NetDead`, then closed with the log reason `closed by the game (taken over)`; the body's `InputTo` handler, whoever set it, is dropped;
  4. the body is told (`TakenOver`): "You take over your character where it stands.", the room is shown, and the prompt follows. The room is told **nothing** (a quiet line would only tell bystanders that someone logged in on top of the character; the link-dead return does say "eyes regain their focus", because the room had seen the eyes go blank).
- **State is untouched**: the body stays where it is, with its fight, Vitality, Endurance, a preparation in progress (a search begun on the old connection finishes for the new one), its inventory and its timers. Prompt format and charset are the character's, so they apply to the new connection at once.
- **The link-dead body** (play-feel.md section 14) is unchanged: it has no connection, `EnterGame` finds it through `/secure/linkdead` and `Reconnected` runs. An online body is looked for first; a name never has both.
- **A pending input handler is cancelled.** Whatever the body was waiting for on the old connection is gone, and the new connection's first line is a command: a hidden-input prompt (`password`), a (y/n) prompt (`delete`, quest `abandon`) and the in-game editor. For the editor the editor is closed with the old connection and what was not written is lost, and the player is told so ("The editor was closed with the old connection; what was not written is lost."); for any other handler "A question that was waiting for your answer was cancelled." Nothing is answered "no" or "yes" on anyone's behalf.
- **Wizards** are taken over the same way. A wizard **in the workshop** (docs/SANDBOX.md) stays in it, with the checkpoint, the mark in `score` and the sandbox objects around her; the sandbox cannot see the hand-over: the call is made by the real login object (`takeover` is refused to anything restricted or not the real login clone, like `exec`), and the body's input handler is dropped by the driver, so a handler that sandbox code set with `InputTo` does not receive the first line typed on the new connection (tested with a wand that grabs the next line). What sandbox code attached to the body in the workshop with its own `AddAction` stays, as it would after any reconnect of the wizard to her own workshop. Walking out of the workshop afterwards works on the new connection (the hand-over to the restored body uses the connection the body has now).
- **Idle**: the new connection's idle clock starts with the password, so a takeover at the idle warning is activity, and the old connection's deadline no longer matters; the body's warning flag is reset.
- **GMCP**: the new connection negotiates its own GMCP at connect; the body forgets what it had sent (`/secure/gmcp.Forget`, the quest journal's burst flag) so that the whole login burst (Char.Status, Room.Info, Char.Vitals, Char.Quests.List and the logs) goes to the new connection.
- **Races** (the game thread does one login step at a time, so each is decided by order, never by interleaving): two connections giving the right password for the same online name one after the other: the first takes the body from the old connection, the second takes it from the first; one body, and only the last connection is left (tested). A connection that is still at the password prompt when another connection of the same name gets in (and made the body) takes over that body when its password is right (tested): `FindOnline` runs when the connection is handed over, not when the name was typed. A name being created by another connection is unchanged ("Someone else is setting up Name right now. Another name?", asked only for a name that has no save file; once the character is saved the name asks for the password and takes the body over). A name that is online skips the choice of a calling (it has one in the body, whatever its file says).
- **A connection that is half open** (the peer is gone, the server has not noticed): the old connection is detached from the body at once, written to and closed. If it does not close by itself (a write that never completes, so the reader never returns), the driver cuts the socket three seconds later (`Connection.Abort`), and the slot in the connection limits (web-client.md 4.2) is given back when the connection ends. Tested up to the point of the limit: at the per-address maximum a new connection is refused, and after a takeover the old one's slot is free at once. The cut itself (a peer that does not read, with a full send buffer) is not tested.
- **The log**: the old connection's line says `closed by the game (taken over)` and never `body lingers`; one line `takeover: NAME's connection from ADDRESS replaced by the one from ADDRESS` is written. No password, right or wrong, is logged.
- **The website**: the bridge makes each web player an ordinary game connection, so a web client takes over from a telnet client and the reverse (checked in `tests/web_client_test.py`); the old web page gets the line and a close.
- **Where**: `mudlib/secure/login.cs` (`FindOnline`, `EnterGame`, `AfterPassword`), `mudlib/std/player.cs` (`BeginTakeover`, `TakenOver`), `mudlib/secure/master.cs` (`takeover`), `src/LesMudOS.Driver/Kernel.cs` (`TakeOver`), `Net/TelnetServer.cs` (`Connection.Abort`). **Tests**: `tests/takeover_test.py` (every case above), the online-name checks in `tests/throttle_test.py`, `tests/web_client_test.py` and `tests/smoke_test.py`; the link-dead return stays in `tests/return_test.py`.

What still tells a stranger something: a name that does not exist goes to "Creating a new account" (existing behaviour, below), and "Someone else is setting up" is said only for a name with no save file. Whether a name is **online** is no longer visible without the password.

## Not built, and known limits

- **Account existence is visible** from the name prompt (password prompt versus "Creating a new account"). Closing it means a different creation flow (a separate "create" command); out of scope.
- **Slow guessing under the threshold** is not stopped: 4 wrong passwords a window per name never locks. It is limited by the number of windows in a day (96 windows, so about 380 guesses a day per name from many addresses; an attacker with many addresses can do more, each address being limited to 39 per window). The 8 character minimum and the hash cost (PBKDF2, 210,000 rounds) are what protect passwords at that rate; a stronger password rule is the next lever.
- **A restart wipes the locks** (above).
- **No persistence of the new-character counts**; same reasoning.
- **Known addresses are an exception worth reviewing** if the game ever has accounts of much higher value than a character (see the trade-off).

## The throttle and the wizard sandbox (net-int)

Decided and proved when the two branches met (`tests/redteam/n2_net_efuns.py`; the sandbox itself is docs/SANDBOX.md).

- **The four efuns** (`ConnectionAddress`, `AddressTag`, `ThrottleSetting`, `ThrottleLog`) are refused by the master (`ValidOperation`) to any restricted execution (a sandbox object anywhere on the call stack: that check comes first, for every operation but two reads) and then to every caller that is not exactly the real clone of `/secure/login` (`ConnectionAddress`) or the real blueprint of `/secure/throttle` (the other three): the check is on the program path **and** the domain (`caller.Domain == ""`), as `/secure/linkdead`'s `IsBody` does. A `/wiz` object built on the real `/secure/login` or `/secure/throttle` (a sandbox may inherit real programs) has the right base class and a domain, and is refused. The throttle's own `FromLogin()` and `Command()` carry the same domain test, a second wall. A real object that is neither (a level 10's own item, say) is refused too: the master answers by program, not by trust.
- **ThrottleLog** writes only what the real throttle gives it: a name (letters only, from the account prompt), an address key the driver made, a wizard's name. The driver also strips control characters, cuts the line to 200 characters and rate-limits it. No wizard or sandbox code can put a line of its own into the log through it.
- **The daemon's functions** are called from sandbox code only as read-only calls on a real object (`Query...`, `Can...`), and the only ones of those (`QueryRefusal`, `QueryDelayMs`, `CanCreate`) either reveal nothing or answer "no" to a caller that is not the login object. Nothing that records a failure, lifts a lock or reads a tag can be called (`BeginPassword`, `Failed`, `Succeeded`, `Created`, `Tag`, `AddressBlocked`, `Command` are not read-only names, and each also checks its caller). There is no login path from sandbox code, so no failure can be recorded against a name or an address by it.
- **The wizard command** exists at level 10 only; a level 1 to 9 wizard has no `throttle` verb and does not see it in `help`.
- **Known addresses** (`knownAddresses`, three keyed hashes in the character's save) are written by `NoteAddress` and read by `KnowsAddress`, both only for the login object (`PreviousObject`, and the connection check), so sandbox code, even running beside its owner's body in the workshop, can neither read nor add one. They are saved with the character, so a workshop checkpoint holds them and a restore returns them: no loss, no duplicate, at most three (tested with logins from other addresses between visits, one dropped in the workshop).
- **The salt file** `<save>/address-secret` is outside the mudlib and is not under the save paths the game's files efuns know (`/players/...`, the registers); `SaveStore.Root` is used only by the driver's own `AddressSecret()`. `cat`, `cp` and `ls` at level 10 on `/address-secret`, `../save/address-secret`, `/../save`, `/players`, and the sandbox's `ReadFile`/`GetDir` of the same, reach nothing (tested).
- **Input limit and wizards:** see DEPLOY.md and web-client.md 4.3: lines over the limit are delayed, never dropped, so an editor paste arrives whole.
- **Connection slots and the link-dead linger:** a connection's slot is given back when its socket ends (`RunTrackedAsync`), not when its body goes. A lingering body holds none, so a reconnect inside the minute is not refused for the dead socket's slot (tested in `tests/net_test.py`).
