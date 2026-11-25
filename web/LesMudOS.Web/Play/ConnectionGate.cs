namespace LesMudOS.Web.Play;

public enum GateResult { Ok, Rate, Address, Full }

/// <summary>Counts web players: total, per address, and the connect rates. Thread safe. The dictionary is pruned and capped
/// (10,000 addresses); past the cap a new address is refused as "rate", the safe failure.</summary>
public sealed class ConnectionGate
{
    private const int MaxTracked = 10_000;
    private sealed class Entry { public int Active; public readonly Queue<long> Recent = new(); }

    private readonly PlayOptions _o;
    private readonly TimeProvider _time;
    private readonly object _lock = new();
    private readonly Dictionary<string, Entry> _byAddress = new();
    private readonly Queue<long> _recentAll = new();
    private int _active;

    public ConnectionGate(PlayOptions options, TimeProvider time) { _o = options; _time = time; }

    public int Active { get { lock (_lock) return _active; } }
    public bool IsFull { get { lock (_lock) return _active >= _o.MaxConnections; } }

    private static void Trim(Queue<long> q, long now) { while (q.Count > 0 && now - q.Peek() >= 60_000) q.Dequeue(); }

    /// <summary>Called once per attempt that reached the gate. Ok: a slot was taken and must be Released with the same key.
    /// Every attempt, refused ones too, counts against the per-address connect rate.</summary>
    public GateResult TryOpen(string key)
    {
        var now = (long)(_time.GetTimestamp() / (double)_time.TimestampFrequency * 1000);
        lock (_lock)
        {
            if (!_byAddress.TryGetValue(key, out var e))
            {
                if (_byAddress.Count >= MaxTracked) Prune(now);
                if (_byAddress.Count >= MaxTracked) return GateResult.Rate;
                e = new Entry();
                _byAddress[key] = e;
            }
            Trim(e.Recent, now);
            Trim(_recentAll, now);
            e.Recent.Enqueue(now);
            if (e.Recent.Count > _o.ConnectsPerMinutePerAddress) return GateResult.Rate;
            _recentAll.Enqueue(now);
            if (_recentAll.Count > _o.ConnectsPerMinute) return GateResult.Rate;
            if (e.Active >= _o.MaxPerAddress) return GateResult.Address;
            if (_active >= _o.MaxConnections) return GateResult.Full;
            e.Active++;
            _active++;
            return GateResult.Ok;
        }
    }

    public void Release(string key)
    {
        lock (_lock)
        {
            if (_byAddress.TryGetValue(key, out var e) && e.Active > 0) { e.Active--; _active--; }
        }
    }

    private void Prune(long now)
    {
        foreach (var k in _byAddress.Where(kv => { Trim(kv.Value.Recent, now); return kv.Value.Active == 0 && kv.Value.Recent.Count == 0; }).Select(kv => kv.Key).ToList())
            _byAddress.Remove(k);
    }
}

/// <summary>A token bucket for typed lines, driven by the time provider.</summary>
public sealed class LineBucket
{
    private readonly double _rate, _burst;
    private readonly TimeProvider _time;
    private double _tokens;
    private long _last;

    public LineBucket(int perSecond, int burst, TimeProvider time)
    {
        _rate = perSecond; _burst = burst; _time = time; _tokens = burst; _last = time.GetTimestamp();
    }

    public bool Take()
    {
        var now = _time.GetTimestamp();
        _tokens = Math.Min(_burst, _tokens + (now - _last) / (double)_time.TimestampFrequency * _rate);
        _last = now;
        if (_tokens < 1) return false;
        _tokens -= 1;
        return true;
    }
}
