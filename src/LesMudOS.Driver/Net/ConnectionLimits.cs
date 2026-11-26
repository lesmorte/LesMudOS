using System.Net;
using System.Net.Sockets;

namespace LesMudOS.Driver.Net;

internal enum Admission { Ok, Full, TooManyFromAddress, TooFast }

/// <summary>
/// The caps on connections (docs/specs/web-client.md 4.2): the total, the number per address and the rate of new connections per address.
/// An address is its IPv4 text, or for IPv6 its /64 (one subscriber usually holds a whole /64, so counting single IPv6 addresses would
/// let one machine hold an unlimited number). Thread-safe: the accept loops run on thread-pool tasks. A zero limit means none. Bounded:
/// the table of connect rates is pruned and has a hard size, so an attacker cannot grow it without end by rotating addresses.
/// </summary>
internal sealed class ConnectionLimits
{
    private const int MaxTrackedAddresses = 50000;

    private readonly object _lock = new();
    private readonly int _maxTotal, _maxPerAddress, _perMinute;
    private readonly Func<long> _clockMs;
    private readonly Dictionary<string, int> _active = new();
    private readonly Dictionary<string, (double Tokens, long Last)> _rates = new();
    private long _lastPrune;
    private int _total;

    public ConnectionLimits(int maxTotal, int maxPerAddress, int connectsPerMinute, Func<long>? clockMs = null)
    {
        _maxTotal = maxTotal; _maxPerAddress = maxPerAddress; _perMinute = connectsPerMinute;
        _clockMs = clockMs ?? (() => Environment.TickCount64);
    }

    public int Total { get { lock (_lock) return _total; } }

    public int CountFor(string key) { lock (_lock) return _active.TryGetValue(key, out var n) ? n : 0; }

    /// <summary>The key a connection is counted under: IPv4 text, or the /64 prefix of an IPv6 address (an IPv4-mapped one counts as IPv4).</summary>
    public static string AddressKey(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
        var b = ip.GetAddressBytes();
        return $"{b[0]:x2}{b[1]:x2}:{b[2]:x2}{b[3]:x2}:{b[4]:x2}{b[5]:x2}:{b[6]:x2}{b[7]:x2}::/64";
    }

    /// <summary>Ask for a slot. Ok means the caller now holds one and must call Release with the same key. A refusal holds nothing.</summary>
    public Admission TryAdmit(string key)
    {
        lock (_lock)
        {
            var now = _clockMs();
            if (_perMinute > 0 && !TakeRate(key, now)) return Admission.TooFast;
            _active.TryGetValue(key, out var held);
            if (_maxPerAddress > 0 && held >= _maxPerAddress) return Admission.TooManyFromAddress;
            if (_maxTotal > 0 && _total >= _maxTotal) return Admission.Full;
            _active[key] = held + 1;
            _total++;
            return Admission.Ok;
        }
    }

    public void Release(string key)
    {
        lock (_lock)
        {
            if (!_active.TryGetValue(key, out var held)) return;
            if (held <= 1) _active.Remove(key); else _active[key] = held - 1;
            if (_total > 0) _total--;
        }
    }

    // A token bucket per address: capacity = the per-minute figure, refilled evenly over the minute. Every attempt takes a token, so a
    // flood of refused attempts keeps the bucket empty rather than being forgiven.
    private bool TakeRate(string key, long now)
    {
        if (_rates.Count >= MaxTrackedAddresses / 2 && now - _lastPrune > 5000) Prune(now);
        if (!_rates.TryGetValue(key, out var bucket))
        {
            if (_rates.Count >= MaxTrackedAddresses) key = "(overflow)"; // many addresses at once: the surplus share one bucket
            bucket = _rates.TryGetValue(key, out var shared) ? shared : (_perMinute, now);
        }
        var tokens = Math.Min(_perMinute, bucket.Tokens + (now - bucket.Last) * _perMinute / 60000.0);
        var ok = tokens >= 1;
        _rates[key] = (ok ? tokens - 1 : tokens, now);
        return ok;
    }

    private void Prune(long now)
    {
        _lastPrune = now;
        foreach (var (k, v) in _rates.ToArray())
            if (now - v.Last >= 60000 && !_active.ContainsKey(k)) _rates.Remove(k); // a full bucket carries no information
    }
}

/// <summary>
/// Log lines about attacks must not be a way to fill the disk: at most a few per minute, then a note of how many were left out.
/// </summary>
internal sealed class LimitedLog
{
    private readonly object _lock = new();
    private readonly int _perMinute;
    private readonly Func<long> _clockMs;
    private readonly Action<string> _sink;
    private long _windowStart = long.MinValue;
    private int _written, _suppressed;

    public LimitedLog(int perMinute, Func<long>? clockMs = null, Action<string>? sink = null)
    {
        _perMinute = perMinute;
        _clockMs = clockMs ?? (() => Environment.TickCount64);
        _sink = sink ?? Log.Info;
    }

    public void Write(string text)
    {
        string? note = null;
        lock (_lock)
        {
            var now = _clockMs();
            if (_windowStart == long.MinValue || now - _windowStart >= 60000)
            {
                if (_suppressed > 0) note = $"{_suppressed} similar log line(s) left out in the last minute";
                _windowStart = now; _written = 0; _suppressed = 0;
            }
            if (_written >= _perMinute) { _suppressed++; return; }
            _written++;
        }
        if (note != null) _sink(note);
        _sink(text);
    }
}
