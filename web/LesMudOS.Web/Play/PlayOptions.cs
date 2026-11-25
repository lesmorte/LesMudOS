using System.Net;

namespace LesMudOS.Web.Play;

/// <summary>Settings of the browser client ("Site:Play:*", environment Site__Play__X, /etc/lesmudos-web.env). See docs/specs/web-client.md section 10.1.
/// A change needs a website restart (which drops web players), except the kill-switch file.</summary>
public sealed class PlayOptions
{
    /// <summary>Master switch. False (the default, so deploying the code changes nothing): /play shows a notice, /play/ws is 404.</summary>
    public bool Enabled { get; set; }

    /// <summary>If this file exists the client is off for new connections and new page views (checked at most every 5 seconds). Existing sessions continue.</summary>
    public string KillSwitchFile { get; set; } = "/var/lib/lesmudos-web/play.off";

    /// <summary>Where the bridge connects: the game's loopback-only bridge listener (driver setting --bridge-port).</summary>
    public string GameBridgeHost { get; set; } = "127.0.0.1";
    public int GameBridgePort { get; set; } = 4001;

    /// <summary>Allow a non-loopback GameBridgeHost. Off: such a host stops the website from starting.</summary>
    public bool AllowRemoteBridge { get; set; }

    /// <summary>Test and development only: connect to the game's ordinary telnet port and send NO PROXY line. In this mode the game
    /// sees the website's own loopback address, not the player's. Never switch it on in production.</summary>
    public bool PlainMode { get; set; }

    /// <summary>Exact origins allowed to open the socket. Empty: the origin of Site:BaseUrl.</summary>
    public List<string> AllowedOrigins { get; set; } = new();

    /// <summary>Refuse a connection with no valid X-Hollowmere-Client-Ip header (false only in Development).</summary>
    public bool RequireClientIpHeader { get; set; } = true;

    public int MaxConnections { get; set; } = 60;
    public int MaxPerAddress { get; set; } = 5;
    public int ConnectsPerMinute { get; set; } = 120;
    public int ConnectsPerMinutePerAddress { get; set; } = 10;
    public int InputLinesPerSecond { get; set; } = 10;
    public int InputBurst { get; set; } = 20;
    public int MaxInputBytes { get; set; } = 4096;
    public int MaxFrameBytes { get; set; } = 8192;
    public int MaxQueuedBytes { get; set; } = 262144;
    public int PingSeconds { get; set; } = 25;
    public int ClientSilenceSeconds { get; set; } = 75;
    public int ShutdownGraceSeconds { get; set; } = 5;

    /// <summary>The loopback check of the startup validation. "localhost" counts.</summary>
    public bool BridgeHostIsLoopback()
        => string.Equals(GameBridgeHost, "localhost", StringComparison.OrdinalIgnoreCase)
           || (IPAddress.TryParse(GameBridgeHost, out var ip) && IPAddress.IsLoopback(ip));

    /// <summary>Throws (the website then refuses to start) when the settings are unsafe or nonsense.</summary>
    public void Validate()
    {
        if (!AllowRemoteBridge && !BridgeHostIsLoopback())
            throw new InvalidOperationException($"Site:Play:GameBridgeHost is '{GameBridgeHost}', which is not a loopback address. The bridge sends the player's address in a PROXY line and must only talk to a game on this machine. Set Site:Play:AllowRemoteBridge=true only if you mean it.");
        if (GameBridgePort is < 1 or > 65535) throw new InvalidOperationException("Site:Play:GameBridgePort must be 1 to 65535.");
        if (MaxConnections < 1 || MaxPerAddress < 1 || ConnectsPerMinute < 1 || ConnectsPerMinutePerAddress < 1
            || InputLinesPerSecond < 1 || InputBurst < 1 || PingSeconds < 1 || ClientSilenceSeconds < 1 || ShutdownGraceSeconds < 0)
            throw new InvalidOperationException("Site:Play: limits must be positive numbers.");
        if (MaxInputBytes is < 16 or > 4096) throw new InvalidOperationException("Site:Play:MaxInputBytes must be 16 to 4096 (the game's own line limit).");
        if (MaxFrameBytes < 256 || MaxQueuedBytes < 4096) throw new InvalidOperationException("Site:Play: MaxFrameBytes or MaxQueuedBytes is too small.");
    }
}
