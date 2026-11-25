using System.Net;
using System.Net.Sockets;

namespace LesMudOS.Driver.Net;

/// <summary>
/// The network-limits half of Connection (docs/specs/web-client.md section 4): the true client address, the slot held in the connection
/// limits, and the input rate limit. Kept in its own file so that TelnetServer.cs, which other branches change a lot, needs only a few one-line hooks.
/// </summary>
internal sealed partial class Connection
{
    /// <summary>The address the limits and the logs count: the peer's address (IPv4-mapped unmapped) or, for a bridge connection, the one in its PROXY line.</summary>
    public IPAddress? ClientIp { get; internal set; }

    /// <summary>True if the connection came in on the bridge listener (a web player).</summary>
    public bool ViaWeb { get; private set; }

    internal string? SlotKey; // the key this connection holds in ConnectionLimits; null if it holds none
    private LineBucket? _bucket;

    /// <summary>Make this a bridge connection: the logs show the claimed address and "(web)", and the limits count that address.</summary>
    internal void AsBridge(IPAddress claimed)
    {
        ClientIp = claimed;
        ViaWeb = true;
        RemoteAddress = $"{claimed} (web)";
    }

    internal void SetPeerAddress(IPAddress peer)
    {
        ClientIp ??= peer.IsIPv4MappedToIPv6 ? peer.MapToIPv4() : peer;
    }

    private LineBucket Bucket => _bucket ??= NetPolicy.NewLineBucket();

    /// <summary>A completed line is about to go to the game: false if it must be dropped (and the warning or the disconnect has been dealt with).</summary>
    private bool AllowLine() => Judge(Bucket.OnLine(Environment.TickCount64));

    /// <summary>Bytes arrived from the socket: false if this chunk is dropped unread.</summary>
    private bool AllowBytes(int count) => Judge(Bucket.OnBytes(count, Environment.TickCount64));

    private bool Judge(InputVerdict verdict)
    {
        switch (verdict)
        {
            case InputVerdict.Allow: return true;
            case InputVerdict.DropWarn:
                Send("[Too much input: some of what you typed is being ignored. Slow down.]\n");
                NetPolicy.LogInput($"input limit: ignoring input from {RemoteAddress}");
                return false;
            case InputVerdict.Disconnect:
                Send("Too much input. Goodbye.\n");
                Close("closed by the game (too much input)");
                NetPolicy.LogInput($"input limit: disconnecting {RemoteAddress}");
                return false;
            default: return false;
        }
    }

    /// <summary>RunAsync, and whatever way it ends, give the slot back.</summary>
    internal async Task RunTrackedAsync()
    {
        try { await RunAsync(); }
        finally { ReleaseSlot(); }
    }

    /// <summary>Give back the slot in the connection limits (once) and, when measuring, log the peak input rate. Called when the connection ends.</summary>
    private void ReleaseSlot()
    {
        NetPolicy.Release(this);
        if (_bucket is { } b && NetPolicy.Settings.InputStats)
        {
            var line = $"input stats {RemoteAddress}: peak {b.PeakLinesPerSecond} lines/s, {b.Lines} lines, {b.Bytes} bytes, {b.Dropped} dropped";
            Log.Info(line);
            if (NetPolicy.Settings.InputStatsFile is { } file)
                try { File.AppendAllText(file, line + "\n"); } catch (IOException) { }
        }
    }
}
