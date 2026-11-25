using System.Net;
using System.Net.Sockets;

namespace LesMudOS.Driver.Net;

/// <summary>
/// The second listener (docs/specs/web-client.md decision 4, section 4.1), for the website's bridge. It listens on a loopback address only:
/// a non-loopback bind is refused and no setting can widen it. Every connection must open with one strict PROXY version 1 line, which must
/// arrive within a few seconds and be at most 107 bytes; it is read one byte at a time so that no game byte after it is ever swallowed. The
/// source address in the line becomes the connection's address for logs and limits, marked "(web)". The public port never reads such a line.
/// </summary>
internal sealed class BridgeListener
{
    private const int MaxPendingHandshakes = 32;

    private readonly Kernel _kernel;
    private readonly TcpListener _listener;
    private readonly TimeSpan _handshake;
    private int _pending;

    private BridgeListener(Kernel kernel, TcpListener listener, double handshakeSeconds)
    {
        _kernel = kernel; _listener = listener; _handshake = TimeSpan.FromSeconds(handshakeSeconds);
    }

    public static BridgeListener? TryStart(Kernel kernel, NetSettings settings)
    {
        if (settings.BridgePort <= 0) return null;
        if (settings.BridgeBindRejected != null || !IPAddress.IsLoopback(settings.BridgeBind))
        {
            Log.Info("bridge listener NOT started: it may listen on a loopback address only");
            return null;
        }
        try
        {
            var listener = new TcpListener(settings.BridgeBind, settings.BridgePort);
            listener.Start(16);
            var bridge = new BridgeListener(kernel, listener, settings.BridgeHandshakeSeconds);
            Log.Info($"bridge listening on {settings.BridgeBind}:{((IPEndPoint)listener.LocalEndpoint).Port} (loopback only, PROXY line required)");
            _ = bridge.AcceptLoopAsync();
            return bridge;
        }
        catch (SocketException e)
        {
            Log.Info($"bridge listener NOT started: {e.SocketErrorCode}");
            return null;
        }
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(); }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }
            _ = HandleAsync(client);
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        if (Interlocked.Increment(ref _pending) > MaxPendingHandshakes) { Interlocked.Decrement(ref _pending); client.Close(); return; }
        try
        {
            if (client.Client.RemoteEndPoint is not IPEndPoint peer || !IPAddress.IsLoopback(peer.Address.IsIPv4MappedToIPv6 ? peer.Address.MapToIPv4() : peer.Address))
            {
                client.Close();
                NetPolicy.BridgeLog.Write("bridge connection refused: peer is not loopback");
                return;
            }
            client.NoDelay = true;
            var (claimed, reason) = await ReadProxyLineAsync(client);
            if (claimed is null)
            {
                client.Close();
                NetPolicy.BridgeLog.Write($"bridge connection refused: bad PROXY line ({reason})");
                return;
            }
            var connection = new Connection(client, _kernel);
            connection.AsBridge(claimed);
            if (!NetPolicy.Admit(connection, client)) return;
            Log.Info($"connection from {connection.RemoteAddress}");
            _kernel.Post(() => _kernel.OnConnect(connection));
            _ = connection.RunTrackedAsync();
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            try { client.Close(); } catch (Exception) { }
        }
        finally { Interlocked.Decrement(ref _pending); }
    }

    private async Task<(IPAddress? Address, string Reason)> ReadProxyLineAsync(TcpClient client)
    {
        var stream = client.GetStream();
        using var timeout = new CancellationTokenSource(_handshake);
        var line = new byte[ProxyLine.MaxBytes];
        var one = new byte[1];
        var length = 0;
        try
        {
            while (true)
            {
                if (await stream.ReadAsync(one.AsMemory(0, 1), timeout.Token) == 0) return (null, "closed");
                if (!ProxyLine.PrefixOk(length, one[0])) return (null, "not a PROXY line");
                line[length++] = one[0];
                if (one[0] == (byte)'\n') break;
                if (length >= ProxyLine.MaxBytes) return (null, "too long");
            }
        }
        catch (OperationCanceledException) { return (null, "timeout"); }
        return ProxyLine.TryParse(line.AsSpan(0, length), out var address, out var reason) ? (address, "") : (null, reason);
    }
}
