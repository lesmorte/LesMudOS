# The login throttle

Status: **built** (November 2026, branch `work/net-limits`). Not part of [web-client.md](web-client.md); the owner asked for it because the game is on the internet: until now a connection had 3 password tries and reconnecting was free, so a password could be guessed without limit. Related: [web-client.md](web-client.md) section 4 (the connection limits the throttle stands behind), [../DEPLOY.md](../DEPLOY.md) (the settings).

## What it does, in one paragraph

Wrong passwords are counted per character name and per address. Five wrong passwords on one name within 15 minutes lock that name for 5 minutes; twenty from one address, across any names, lock the address for 15 minutes; each lock doubles the next one up to a ceiling. A refusal always says the same thing, and every "no" comes after the same one-second pause. A name that is locked still lets in the addresses its owner has used before, with three tries of their own. A success wipes a name's count only when it comes from an address that had not failed on that name. New characters are limited to 3 per address per hour and 10 per day.

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

**Per address.** Each wrong password is also recorded against the address. 20 inside 15 minutes lock the **address for 15 minutes**, doubling per lock up to 4 hours. A locked address is refused **at connect**, before the name prompt, whatever name it would type, and the known-address exception below does not apply to it.

**What a success does.** A correct password wipes the failure record of that name (and lifts its lock) **only if the address had not failed on that name**: the record keeps the set of addresses that failed. So an attacker who guesses four wrong passwords and then logs into his own other character, or even into the victim's account, resets nothing; the real owner coming from a clean address does. A success never changes an address's own count: logging into one's own account is not a way to buy more guesses from that address.

**The owner of a locked name.** The character's save keeps the last three addresses it was logged into successfully, each as a keyed hash (below). While a name is locked, a connection from one of those addresses is not refused: it gets the password prompt, with **its own limit of three wrong passwords** (per name and address, in the same 15 minute window); after the third that address is refused as well until the lock ends. Those failures count toward the address's own count but not toward the name's, so the owner's typos can neither lengthen the lock nor trigger the next doubling.

*The trade-off, stated plainly.* The exception exists so that a locked name cannot be used to keep its owner out: without it, anyone could type five wrong passwords at "Vera" every five minutes and Vera could never play. With it, a person who shares an address with the owner (a household, a school, a mobile carrier's gateway, or another customer in the same IPv6 /64) gets three more guesses at that password per window, and an attacker who has taken over a machine at an address the owner used gets the same. It does not help anyone else. The three tries are the same three tries a connection always had; the exception only stops the lock from removing them from the owner. The list holds three addresses, the newest replacing the oldest, so an owner who moves between more than three places is locked out of the exception, and waits out the lock (at most an hour).

**New characters.** At most **3 new characters per address per hour and 10 per day**, counted when the password is set (the moment a name becomes reserved). The check runs when a new name is typed and again before the password is stored. The refusal is a plain sentence ("Too many new characters have been made from your address. Try again later.") and closes the connection. Existing characters are unaffected.

## What a refused player sees, and the pause

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
| `LESMUDOS_THROTTLE_ADDRESS_FAILURES` | 20 | wrong passwords from an address that lock it |
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

## Not built: "X is already here" before the password

Today, at the name prompt, a name that is online is answered "`Vera is already here. Another name?`" **before** any password. Anyone can therefore ask, without a password and without the throttle noticing, who is online. The throttle does not change this. Proposal: ask for the password first, then say it. What it takes now:

1. In `GotName`, skip the `IsOnline` test; go on to `BeginLogin` and the password prompt as for any existing name (an online name has a save file, so it is "password").
2. After a correct password, test `IsOnline(name)`. If the body is online and **connected**, say "`Vera is already here. Another name? `" (the visitor has proved the password, so this reveals nothing to a stranger). The attempt counts as a success for the throttle, not a failure.
3. The link-dead body (`mudlib/secure/linkdead.cs`, 60 seconds): `EnterGame` already finds it through `/secure/linkdead.Find(name)` and resumes it, so a reconnect with the password works as it does today. `IsOnline` (which looks at `Users()`, connected objects only) does not see a link-dead body, so no change is needed there.
4. Two things must be checked: the test suites that expect "already here" before the password (`smoke_test.py`, `comm_test.py` use `Client("alice")` twice and look for "Another name?" right after the name: they would need to send the password first); and the new-account path (a name that is being claimed by someone else has its own message, "Someone else is setting up…", which has the same property and would also need to come after something the visitor cannot get for free).

This is a login-flow change with test-suite fallout, so it is left for the owner to decide.

## Not built, and known limits

- **Account existence is visible** from the name prompt (password prompt versus "Creating a new account") and from "already here". Closing it means a different creation flow (a separate "create" command); out of scope.
- **Slow guessing under the threshold** is not stopped: 4 wrong passwords a window per name never locks. It is limited by the number of windows in a day (96 windows, so about 380 guesses a day per name from many addresses; an attacker with many addresses can do more, each address being limited to 19 per window). The 8 character minimum and the hash cost (PBKDF2, 210,000 rounds) are what protect passwords at that rate; a stronger password rule is the next lever.
- **A restart wipes the locks** (above).
- **No persistence of the new-character counts**; same reasoning.
- **Known addresses are an exception worth reviewing** if the game ever has accounts of much higher value than a character (see the trade-off).
