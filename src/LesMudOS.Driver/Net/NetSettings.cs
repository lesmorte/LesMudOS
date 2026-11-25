using System.Globalization;
using System.Net;

namespace LesMudOS.Driver.Net;

/// <summary>
/// The operator's settings for the network layer (docs/specs/web-client.md section 4, docs/DEPLOY.md): the loopback bridge listener, the
/// connection limits and the input rate limit. Defaults first, then the command line (only the three the spec names), then the
/// environment, which wins. A bad value is ignored with a note in the log. For the counts and rates, 0 means "no limit" (the test
/// suites use that; production leaves the defaults).
///
///   LESMUDOS_BRIDGE_PORT (--bridge-port)       0 = no bridge listener (default). Otherwise the loopback port the website's bridge dials.
///   LESMUDOS_BRIDGE_BIND                        127.0.0.1 or ::1 only. Any other address makes the driver refuse to start the listener.
///   LESMUDOS_BRIDGE_HANDSHAKE_SECONDS           how long the PROXY line may take (5).
///   LESMUDOS_MAX_CONNECTIONS (--max-connections)  total connections (200).
///   LESMUDOS_MAX_PER_ADDRESS (--max-per-address)  connections per address, IPv6 counted by /64 (10).
///   LESMUDOS_CONNECTS_PER_MINUTE                new connections per address per minute (20).
///   LESMUDOS_MAX_LINES_PER_SECOND               typed lines per second, sustained (20).
///   LESMUDOS_INPUT_BURST                        typed lines that may arrive at once (60).
///   LESMUDOS_INPUT_BYTES_PER_SECOND             typed bytes per second, sustained (65536); the burst is four seconds' worth.
///   LESMUDOS_INPUT_OVERFLOW_SECONDS             continuous overflow that ends the connection (5).
///   LESMUDOS_INPUT_STATS                        1 = log each connection's peak input rate when it ends (measurement; off); a file path = also append it there.
/// </summary>
internal sealed class NetSettings
{
    public int BridgePort { get; private set; }
    public IPAddress BridgeBind { get; private set; } = IPAddress.Loopback;
    public string? BridgeBindRejected { get; private set; }
    public double BridgeHandshakeSeconds { get; private set; } = 5;
    public int MaxConnections { get; private set; } = 200;
    public int MaxPerAddress { get; private set; } = 10;
    public int ConnectsPerMinute { get; private set; } = 20;
    public double LinesPerSecond { get; private set; } = 20;
    public double InputBurst { get; private set; } = 60;
    public double BytesPerSecond { get; private set; } = 65536;
    public double OverflowSeconds { get; private set; } = 100;
    public bool InputStats { get; private set; }
    public string? InputStatsFile { get; private set; }

    /// <summary>One value from the command line or the environment. Returns true if the name is one of ours.</summary>
    public bool Apply(string name, string? text, string source)
    {
        switch (name)
        {
            case "LESMUDOS_BRIDGE_PORT": if (Int(text, source, 0, 65535, out var port)) BridgePort = port; return true;
            case "LESMUDOS_BRIDGE_BIND": SetBind(text); return true;
            case "LESMUDOS_BRIDGE_HANDSHAKE_SECONDS": if (Num(text, source, 0.05, 60, out var secs)) BridgeHandshakeSeconds = secs; return true;
            case "LESMUDOS_MAX_CONNECTIONS": if (Int(text, source, 0, 100000, out var total)) MaxConnections = total; return true;
            case "LESMUDOS_MAX_PER_ADDRESS": if (Int(text, source, 0, 100000, out var per)) MaxPerAddress = per; return true;
            case "LESMUDOS_CONNECTS_PER_MINUTE": if (Int(text, source, 0, 1000000, out var cpm)) ConnectsPerMinute = cpm; return true;
            case "LESMUDOS_MAX_LINES_PER_SECOND": if (Num(text, source, 0, 100000, out var lps)) LinesPerSecond = lps; return true;
            case "LESMUDOS_INPUT_BURST": if (Num(text, source, 1, 1000000, out var burst)) InputBurst = burst; return true;
            case "LESMUDOS_INPUT_BYTES_PER_SECOND": if (Num(text, source, 0, 1e9, out var bps)) BytesPerSecond = bps; return true;
            case "LESMUDOS_INPUT_OVERFLOW_SECONDS": if (Num(text, source, 0.1, 3600, out var over)) OverflowSeconds = over; return true;
            case "LESMUDOS_INPUT_STATS": SetStats(text); return true;
            default: return false;
        }
    }

    public static readonly string[] Names =
    {
        "LESMUDOS_BRIDGE_PORT", "LESMUDOS_BRIDGE_BIND", "LESMUDOS_BRIDGE_HANDSHAKE_SECONDS", "LESMUDOS_MAX_CONNECTIONS", "LESMUDOS_MAX_PER_ADDRESS",
        "LESMUDOS_CONNECTS_PER_MINUTE", "LESMUDOS_MAX_LINES_PER_SECOND", "LESMUDOS_INPUT_BURST", "LESMUDOS_INPUT_BYTES_PER_SECOND",
        "LESMUDOS_INPUT_OVERFLOW_SECONDS", "LESMUDOS_INPUT_STATS",
    };

    public void ApplyEnvironment()
    {
        foreach (var name in Names) Apply(name, Environment.GetEnvironmentVariable(name), name);
    }

    /// <summary>The command line names the spec gives (section 4.1, 4.2).</summary>
    public bool ApplyArgument(string flag, string value) => flag switch
    {
        "--bridge-port" => Apply("LESMUDOS_BRIDGE_PORT", value, flag),
        "--max-connections" => Apply("LESMUDOS_MAX_CONNECTIONS", value, flag),
        "--max-per-address" => Apply("LESMUDOS_MAX_PER_ADDRESS", value, flag),
        _ => false,
    };

    /// <summary>The bridge listener may only ever listen on a loopback address: any other value is remembered as rejected and the listener will not start.</summary>
    private void SetBind(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var t = text.Trim();
        if (IPAddress.TryParse(t, out var ip) && IPAddress.IsLoopback(ip)) { BridgeBind = ip; BridgeBindRejected = null; }
        else BridgeBindRejected = t.Length > 64 ? t[..64] : t;
    }

    // "1" logs the peak input rate of each connection when it ends; any other non-empty value is a file path that the same line is also appended to
    // (for measuring a test suite whose driver log is deleted when it finishes).
    private void SetStats(string? text)
    {
        var t = text?.Trim();
        InputStats = !string.IsNullOrEmpty(t) && t != "0";
        InputStatsFile = InputStats && t != "1" ? t : null;
    }

    private static bool Int(string? text, string source, int min, int max, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= min && value <= max) return true;
        Log.Info($"ignoring {source}: '{Clip(text)}' is not a whole number from {min} to {max}");
        return false;
    }

    private static bool Num(string? text, string source, double min, double max, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value) && value >= min && value <= max) return true;
        Log.Info($"ignoring {source}: '{Clip(text)}' is not a number from {min:0.##} to {max:0.##}");
        return false;
    }

    private static string Clip(string text) => text.Length > 40 ? text[..40] : text;
}
