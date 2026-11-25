namespace LesMudOS.Driver.Net;

internal enum InputVerdict { Allow, Drop, DropWarn, Disconnect }

/// <summary>
/// The input rate limit of one connection (docs/specs/web-client.md 4.3): a token bucket on typed lines and one on bytes, so neither a
/// flood of empty lines nor a flood of long ones reaches the game thread unchecked. A line with no token is dropped (it never reaches the
/// game); the first drop of an overflow gives one warning; an overflow that lasts OverflowSeconds without the buckets recovering to half
/// full ends the connection. Nothing typed is ever kept or logged here. Not thread-safe: one reader task owns it. A rate of 0 switches a bucket off.
/// </summary>
internal sealed class LineBucket
{
    private sealed class Pool
    {
        public double Tokens, Cap, Rate; public long Last;
        public Pool(double rate, double cap, long now) { Rate = rate; Cap = cap; Tokens = cap; Last = now; }
        public void Refill(long now) { Tokens = Math.Min(Cap, Tokens + (now - Last) * Rate / 1000.0); Last = now; }
    }

    private readonly Pool _lines, _bytes;
    private readonly long _overflowMs;
    private long _overSince = -1;
    private bool _disconnected;
    private readonly long[]? _ring; // only while measuring: the times of the last lines
    private int _ringAt;

    public int PeakLinesPerSecond { get; private set; }
    public long Lines { get; private set; }
    public long Bytes { get; private set; }
    public long Dropped { get; private set; }

    public LineBucket(double linesPerSecond, double burst, double bytesPerSecond, double overflowSeconds, long nowMs, bool measure = false)
    {
        _lines = new Pool(linesPerSecond, burst, nowMs);
        _bytes = new Pool(bytesPerSecond, Math.Max(bytesPerSecond * 4, 1), nowMs);
        _overflowMs = (long)(overflowSeconds * 1000);
        if (measure) _ring = new long[256];
    }

    /// <summary>A completed line (empty lines count: each costs the game thread a prompt).</summary>
    public InputVerdict OnLine(long now)
    {
        if (_disconnected) return InputVerdict.Drop;
        Lines++;
        Measure(now);
        if (_lines.Rate <= 0) return Check(now, true);
        _lines.Refill(now);
        return Check(now, _lines.Tokens >= 1, () => _lines.Tokens -= 1);
    }

    /// <summary>Bytes just read from the socket, before they are looked at.</summary>
    public InputVerdict OnBytes(int count, long now)
    {
        if (_disconnected) return InputVerdict.Drop;
        Bytes += count;
        if (_bytes.Rate <= 0) return InputVerdict.Allow;
        _bytes.Refill(now);
        if (_bytes.Tokens >= count) { _bytes.Tokens -= count; Recovered(now); return InputVerdict.Allow; }
        return Over(now);
    }

    private InputVerdict Check(long now, bool ok, Action? take = null)
    {
        if (!ok) return Over(now);
        take?.Invoke();
        Recovered(now);
        return InputVerdict.Allow;
    }

    private bool Recovered(long now)
    {
        if (_overSince < 0) return true;
        if (_lines.Rate > 0) _lines.Refill(now);
        if (_bytes.Rate > 0) _bytes.Refill(now);
        if ((_lines.Rate <= 0 || _lines.Tokens >= _lines.Cap / 2) && (_bytes.Rate <= 0 || _bytes.Tokens >= _bytes.Cap / 2)) { _overSince = -1; return true; }
        return false;
    }

    private InputVerdict Over(long now)
    {
        Dropped++;
        if (_overSince < 0) { _overSince = now; return InputVerdict.DropWarn; }
        if (now - _overSince >= _overflowMs) { _disconnected = true; return InputVerdict.Disconnect; }
        return InputVerdict.Drop;
    }

    private void Measure(long now)
    {
        if (_ring is null) return;
        _ring[_ringAt] = now;
        var count = 0;
        for (var i = 0; i < _ring.Length; i++)
        {
            var t = _ring[(_ringAt - i + _ring.Length) % _ring.Length];
            if (t == 0 || now - t > 1000) break;
            count++;
        }
        _ringAt = (_ringAt + 1) % _ring.Length;
        if (count > PeakLinesPerSecond) PeakLinesPerSecond = count;
    }
}
