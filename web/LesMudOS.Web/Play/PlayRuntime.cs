using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LesMudOS.Web.Play;

/// <summary>Everything the bridge shares between players: the settings, the limits, the kill switch. One per website.</summary>
public sealed class PlayRuntime
{
    public const string SubProtocol = "hollowmere.v1";

    private readonly TimeProvider _time;
    private readonly ILogger _log;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly List<string> _origins;
    private long _killCheckedAt = long.MinValue;
    private bool _killed;

    public PlayOptions Options { get; }
    public ConnectionGate Gate { get; }
    public TimeProvider Time => _time;

    public PlayRuntime(IOptions<SiteOptions> site, IWebHostEnvironment env, TimeProvider time, ILoggerFactory logs, IHostApplicationLifetime lifetime)
    {
        Options = site.Value.Play;
        Options.Validate();
        _time = time;
        _lifetime = lifetime;
        _log = logs.CreateLogger("Play");
        Gate = new ConnectionGate(Options, time);
        _origins = OriginCheck.Effective(Options, site.Value.BaseUrl, env.IsDevelopment());
        DevelopmentMode = env.IsDevelopment();
    }

    public bool DevelopmentMode { get; }

    /// <summary>On: the switch is on and the kill-switch file does not exist (looked for at most every 5 seconds).</summary>
    public bool IsOn()
    {
        if (!Options.Enabled) return false;
        var now = _time.GetTimestamp();
        if (_killCheckedAt == long.MinValue || (now - _killCheckedAt) / (double)_time.TimestampFrequency >= 5)
        {
            try { _killed = !string.IsNullOrWhiteSpace(Options.KillSwitchFile) && File.Exists(Options.KillSwitchFile); }
            catch (Exception) { _killed = false; }
            _killCheckedAt = now;
        }
        return !_killed;
    }

    public bool IsFull => Gate.IsFull;

    /// <summary>Where "Play Now" leads: the client when it is on, otherwise the instructions (so a site with the client off looks as before).</summary>
    public string PlayHref => IsOn() ? "/play" : "/how-to-play";

    private void Refuse(HttpContext ctx, int status, string reason, string who, string? extra = null)
    {
        _log.LogInformation("play: refused {Who} reason={Reason}{Extra}", who, reason, extra == null ? "" : " " + extra);
        ctx.Response.Headers.CacheControl = "no-store";
        ctx.Response.StatusCode = status;
        if (status == 429) ctx.Response.Headers.RetryAfter = "60";
    }

    /// <summary>The handshake checks of docs/specs/web-client.md section 2.4, then the bridge for one player.</summary>
    public async Task HandleAsync(HttpContext ctx)
    {
        var peer = ctx.Connection.RemoteIpAddress;
        var peerText = peer == null ? "-" : (peer.IsIPv4MappedToIPv6 ? peer.MapToIPv4() : peer).ToString();
        if (!IsOn()) { Refuse(ctx, 404, "disabled", peerText); return; }
        if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.Headers.CacheControl = "no-store"; ctx.Response.StatusCode = 400; return; }

        var originValues = ctx.Request.Headers.Origin;
        var origin = originValues.Count == 1 ? originValues[0] : null;
        if (!OriginCheck.IsAllowed(origin, _origins)) { Refuse(ctx, 403, "origin", peerText, "origin=" + OriginCheck.Safe(origin)); return; }
        if (!ctx.WebSockets.WebSocketRequestedProtocols.Contains(SubProtocol)) { Refuse(ctx, 403, "subprotocol", peerText); return; }

        var ip = ClientAddress.FromRequest(ctx);
        if (ip == null)
        {
            if (Options.RequireClientIpHeader) { Refuse(ctx, 403, "address-header", peerText); return; }
            ip = peer == null ? IPAddress.Loopback : (peer.IsIPv4MappedToIPv6 ? peer.MapToIPv4() : peer);
        }
        var who = ip.ToString();
        var key = ClientAddress.Key(ip);

        var gate = Gate.TryOpen(key);
        if (gate == GateResult.Rate) { Refuse(ctx, 429, "rate", who); return; }
        if (gate == GateResult.Address) { Refuse(ctx, 429, "address", who); return; }

        var accept = new Microsoft.AspNetCore.Http.WebSocketAcceptContext
        {
            SubProtocol = SubProtocol,
            KeepAliveInterval = TimeSpan.FromSeconds(Options.PingSeconds),
            KeepAliveTimeout = TimeSpan.FromSeconds(Options.ClientSilenceSeconds),
        };
        ctx.Response.Headers.CacheControl = "no-store";
        if (gate == GateResult.Full)
        {
            _log.LogInformation("play: refused {Who} reason=full", who);
            using var full = await ctx.WebSockets.AcceptWebSocketAsync(accept);
            await BridgeSession.RefuseAsync(full, "full", 1013, Options);
            return;
        }

        var session = new BridgeSession(this, ctx, key, who, origin ?? "-", _log, _lifetime.ApplicationStopping);
        await session.RunAsync(accept);
    }
}
