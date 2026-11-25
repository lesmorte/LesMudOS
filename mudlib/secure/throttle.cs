/// <summary>
/// The login throttle (docs/specs/login-throttle.md). Counts wrong passwords per character name and per address, and new characters per
/// address, and says when a login must be refused. Everything is in memory and is lost at a restart: a lock does not survive one, which an
/// attacker could use only by making the owner restart the game. Blueprint only; only the login object (clones of /secure/login) may
/// report or ask, and only a level 10 wizard's own command may list or clear.
///
/// Addresses arrive as the driver's key (IPv4 text, or the /64 of an IPv6 address; for a web player the true address behind the website).
/// Nothing typed is ever kept here: not a password, not a guess. The tables are bounded.
/// </summary>
public class Throttle : MudObject
{
    private const string LoginProgram = "/secure/login";
    private const int MaxRecords = 20000;
    private const int LevelClear = 10;
    private const string Refused = "Too many attempts. Try again later.";

    private sealed class NameRec
    {
        public List<long> Fails = new();            // times of wrong passwords inside the window
        public HashSet<string> FailedFrom = new();  // the addresses that have failed on this name since the record began
        public long LockUntil;
        public int Trips;
        public long Last;
    }

    private sealed class AddrRec
    {
        public List<long> Fails = new();
        public long LockUntil;
        public int Trips;
        public long Last;
    }

    private readonly Dictionary<string, NameRec> names = new();
    private readonly Dictionary<string, AddrRec> addrs = new();
    private readonly Dictionary<string, List<long>> knownFails = new(); // "Name|address": wrong passwords from an address that is known, while the name is locked
    private readonly Dictionary<string, List<long>> created = new();    // address: times new characters were made
    private long lastSweep;

    private readonly Dictionary<string, long> settings = new();

    // The driver reads the environment once at boot; the daemon asks once per setting.
    private long S(string name)
    {
        if (!settings.TryGetValue(name, out var value)) settings[name] = value = ThrottleSetting(name);
        return value;
    }

    private bool FromLogin()
    {
        var caller = PreviousObject();
        return !Self.IsClone && Self.Domain == "" && caller != null && caller.IsAlive && caller.IsClone && caller.Domain == "" && caller.ProgramPath == LoginProgram;
    }

    private static string Key(string? address) => string.IsNullOrEmpty(address) ? "" : address;

    // ---- What the login object asks ----

    /// <summary>The text for a refusal. The same for every cause and every name.</summary>
    public string QueryRefusal() => Refused;

    /// <summary>How long the login object waits before it answers a wrong password or a refusal, in milliseconds.</summary>
    public int QueryDelayMs() => (int)Math.Max(0, S("FailDelayMs"));

    /// <summary>The keyed hash of an address, for the list of known addresses in a character's save. "" if there is no address.</summary>
    public string Tag(string? address)
    {
        if (!FromLogin() || string.IsNullOrEmpty(address)) return "";
        return AddressTag(address);
    }

    /// <summary>True if this address may not try any login now (it failed too often).</summary>
    public bool AddressBlocked(string? address)
    {
        if (!FromLogin()) return true;
        var key = Key(address);
        if (key == "" || S("AddressFailures") <= 0) return false;
        var now = Time();
        return addrs.TryGetValue(key, out var rec) && Fresh(rec, now) && rec.LockUntil > now;
    }

    /// <summary>
    /// Before the password prompt for an existing account: "ok", "known" (the name is locked, or the address is, but this address has logged into this
    /// character before, so it gets a few tries of its own under the strict limit) or "refused". The address and the name are both checked; the answer
    /// never says which was the cause.
    /// </summary>
    public string BeginPassword(string name, string? address, bool knownAddress)
    {
        if (!FromLogin()) return "refused";
        var now = Time();
        var key = Key(address);
        var addressLocked = key != "" && S("AddressFailures") > 0 && addrs.TryGetValue(key, out var arec) && Fresh(arec, now) && arec.LockUntil > now;
        var nameLocked = S("NameFailures") > 0 && names.TryGetValue(name, out var rec) && FreshName(rec, now) && rec.LockUntil > now;
        if (!addressLocked && !nameLocked) return "ok";
        if (knownAddress && KnownCount(name, key, now) < S("KnownTries")) return "known";
        return "refused";
    }

    /// <summary>
    /// A wrong password. Returns true if this connection may try again, false if it must end now (a lock has just started or the known address used its tries).
    /// </summary>
    public bool Failed(string name, string? address, bool knownMode)
    {
        if (!FromLogin()) return false;
        var now = Time();
        Sweep(now);
        var key = Key(address);
        var again = true;
        if (key != "" && S("AddressFailures") > 0)
        {
            var a = Addr(key, now);
            Prune(a.Fails, now);
            a.Fails.Add(now);
            a.Last = now;
            if (a.Fails.Count >= S("AddressFailures") && a.LockUntil <= now)
            {
                var seconds = Lock(S("AddressLock"), S("AddressLockMax"), a.Trips++);
                a.LockUntil = now + seconds;
                a.Fails.Clear();
                ThrottleLog($"address locked: {key} for {seconds} s");
                again = false;
            }
        }
        if (knownMode)
        {
            var list = KnownList(name, key);
            Prune(list, now);
            list.Add(now);
            if (list.Count >= S("KnownTries")) again = false;
            return again;
        }
        if (S("NameFailures") > 0)
        {
            var r = Name(name, now);
            Prune(r.Fails, now);
            r.Fails.Add(now);
            r.Last = now;
            if (key != "") r.FailedFrom.Add(key);
            if (r.Fails.Count >= S("NameFailures") && r.LockUntil <= now)
            {
                var seconds = Lock(S("NameLock"), S("NameLockMax"), r.Trips++);
                r.LockUntil = now + seconds;
                r.Fails.Clear();
                ThrottleLog($"name locked: {name} (from {(key == "" ? "unknown" : key)}) for {seconds} s");
                again = false;
            }
        }
        return again;
    }

    /// <summary>
    /// A correct password. The record of failures against that name is wiped only if the login comes from an address that has not failed on it: whoever
    /// guesses wrongly and then rightly learns nothing about how to reset the count, and logging into another character of one's own does not touch it.
    /// An address's own count is never reset by a success.
    /// </summary>
    public void Succeeded(string name, string? address)
    {
        if (!FromLogin()) return;
        var key = Key(address);
        if (names.TryGetValue(name, out var r) && (key == "" || !r.FailedFrom.Contains(key)))
        {
            names.Remove(name);
            ClearKnown(name);
        }
    }

    /// <summary>True if a new character may be made from this address now (3 an hour, 10 a day).</summary>
    public bool CanCreate(string? address)
    {
        if (!FromLogin()) return false;
        var key = Key(address);
        if (key == "" || !created.TryGetValue(key, out var list)) return true;
        var now = Time();
        list.RemoveAll(t => now - t >= S("NewDaySeconds"));
        var hourly = S("NewPerHour");
        var daily = S("NewPerDay");
        if (daily > 0 && list.Count >= daily) return false;
        if (hourly > 0 && list.Count(t => now - t < S("NewHourSeconds")) >= hourly) return false;
        return true;
    }

    /// <summary>A new character was made (its password was set) from this address.</summary>
    public void Created(string? address)
    {
        if (!FromLogin()) return;
        var key = Key(address);
        if (key == "" || (S("NewPerHour") <= 0 && S("NewPerDay") <= 0)) return;
        var now = Time();
        Sweep(now);
        if (!created.TryGetValue(key, out var list))
        {
            if (created.Count >= MaxRecords) return;
            list = created[key] = new List<long>();
        }
        list.Add(now);
        while (list.Count > 100) list.RemoveAt(0);
    }

    // ---- The wizard's command (level 10) ----

    /// <summary>`throttle` lists the locks; `throttle clear NAME|ADDRESS|all` lifts them. Only a level 10 wizard's own body may ask.</summary>
    public string Command(string args)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || !caller.IsAlive || !caller.IsClone || caller.ProgramPath != "/std/player" || caller.Domain != "" || caller != ThisPlayer()) return "";
        if (caller.Call<int>("QueryWizLevel") < LevelClear) return "";
        var now = Time();
        args = (args ?? "").Trim();
        if (args.Length == 0) return Report(now);
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words[0].ToLowerInvariant() != "clear" || words.Length != 2) return "Usage: throttle | throttle clear <name>|<address>|all\n";
        return Clear(words[1], now);
    }

    private string Report(long now)
    {
        var text = new StringBuilder("Login throttle (in memory; lost at a restart)\n");
        var any = false;
        foreach (var (name, r) in names.OrderBy(p => p.Key))
        {
            if (!FreshName(r, now) || r.LockUntil <= now) continue;
            text.Append($"  name    {name,-16} locked for {Remaining(r.LockUntil - now)} more (locked {r.Trips} time(s))\n");
            any = true;
        }
        foreach (var (key, a) in addrs.OrderBy(p => p.Key))
        {
            if (!Fresh(a, now) || a.LockUntil <= now) continue;
            text.Append($"  address {key,-24} locked for {Remaining(a.LockUntil - now)} more (locked {a.Trips} time(s))\n");
            any = true;
        }
        if (!any) text.Append("  no locks\n");
        var counting = names.Count(p => p.Value.Fails.Count > 0) + addrs.Count(p => p.Value.Fails.Count > 0);
        text.Append($"  {counting} name(s) and address(es) with recent wrong passwords, {created.Count} address(es) that made characters\n");
        text.Append("  throttle clear <name>|<address>|all  lifts a lock and forgets the count\n");
        return text.ToString();
    }

    private string Clear(string what, long now)
    {
        if (what.ToLowerInvariant() == "all")
        {
            var total = names.Count + addrs.Count + created.Count;
            names.Clear(); addrs.Clear(); knownFails.Clear(); created.Clear();
            ThrottleLog($"all throttle records cleared by {ThisPlayer()?.Name}");
            return $"Cleared everything ({total} record(s)).\n";
        }
        var cleared = 0;
        if (what.All(char.IsLetter))
        {
            what = char.ToUpper(what[0]) + what.Substring(1).ToLower();
            if (names.Remove(what)) cleared++;
            ClearKnown(what);
        }
        else
        {
            if (addrs.Remove(what)) cleared++;
            if (created.Remove(what)) cleared++;
        }
        if (cleared == 0) return $"Nothing is recorded for {what}.\n";
        ThrottleLog($"{what} cleared by {ThisPlayer()?.Name}");
        return $"Cleared {what}.\n";
    }

    private static string Remaining(long seconds) => seconds >= 3600 ? $"{seconds / 3600} h {seconds % 3600 / 60} min" : seconds >= 60 ? $"{seconds / 60} min {seconds % 60} s" : $"{seconds} s";

    // ---- Records ----

    private bool Fresh(AddrRec r, long now) => now - r.Last < S("Quiet") || r.LockUntil > now;
    private bool FreshName(NameRec r, long now) => now - r.Last < S("Quiet") || r.LockUntil > now;

    private NameRec Name(string name, long now)
    {
        if (names.TryGetValue(name, out var r) && FreshName(r, now)) return r;
        if (names.Count >= MaxRecords) Sweep(now, true);
        r = new NameRec { Last = now };
        if (names.Count < MaxRecords || names.ContainsKey(name)) names[name] = r;
        return r;
    }

    private AddrRec Addr(string key, long now)
    {
        if (addrs.TryGetValue(key, out var r) && Fresh(r, now)) return r;
        if (addrs.Count >= MaxRecords) Sweep(now, true);
        r = new AddrRec { Last = now };
        if (addrs.Count < MaxRecords || addrs.ContainsKey(key)) addrs[key] = r;
        return r;
    }

    private void Prune(List<long> times, long now) => times.RemoveAll(t => now - t >= S("NameWindow"));

    private static long Lock(long first, long max, int trips)
    {
        var seconds = first;
        for (var i = 0; i < trips && seconds < max; i++) seconds *= 2;
        return Math.Min(seconds, Math.Max(first, max));
    }

    private List<long> KnownList(string name, string key)
    {
        var k = name + "|" + key;
        if (!knownFails.TryGetValue(k, out var list))
        {
            if (knownFails.Count >= MaxRecords) knownFails.Clear();
            list = knownFails[k] = new List<long>();
        }
        return list;
    }

    private int KnownCount(string name, string key, long now)
    {
        if (!knownFails.TryGetValue(name + "|" + key, out var list)) return 0;
        Prune(list, now);
        return list.Count;
    }

    private void ClearKnown(string name)
    {
        foreach (var k in knownFails.Keys.Where(k => k.StartsWith(name + "|", StringComparison.Ordinal)).ToList()) knownFails.Remove(k);
    }

    /// <summary>Forget records that have been quiet for the reset time and are not locked. At most once a minute unless the table is full.</summary>
    private void Sweep(long now, bool force = false)
    {
        if (!force && now - lastSweep < 60) return;
        lastSweep = now;
        foreach (var k in names.Where(p => !FreshName(p.Value, now)).Select(p => p.Key).ToList()) names.Remove(k);
        foreach (var k in addrs.Where(p => !Fresh(p.Value, now)).Select(p => p.Key).ToList()) addrs.Remove(k);
        foreach (var k in created.Where(p => p.Value.All(t => now - t >= S("NewDaySeconds"))).Select(p => p.Key).ToList()) created.Remove(k);
        foreach (var k in knownFails.Where(p => p.Value.All(t => now - t >= S("NameWindow"))).Select(p => p.Key).ToList()) knownFails.Remove(k);
    }
}
