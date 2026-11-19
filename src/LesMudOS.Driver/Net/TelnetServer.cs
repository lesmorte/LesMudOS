using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using LesMudOS.Api;

namespace LesMudOS.Driver.Net;

/// <summary>
/// One client connection. Reading and writing happen on thread-pool tasks;
/// the only thing they do with game state is post events to the kernel.
/// </summary>
internal sealed class Connection
{
    private const int MaxLineLength = 4096;

    private readonly TcpClient _client;
    private readonly Kernel _kernel;
    private readonly Channel<byte[]> _outbox = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });

    /// <summary>The object this connection is attached to. Read and written on the game thread only.</summary>
    public ObjectRef? Owner;

    public string RemoteAddress { get; }

    public Connection(TcpClient client, Kernel kernel)
    {
        _client = client;
        _kernel = kernel;
        RemoteAddress = client.Client.RemoteEndPoint is IPEndPoint ep
            ? (ep.Address.IsIPv4MappedToIPv6 ? $"{ep.Address.MapToIPv4()}:{ep.Port}" : ep.ToString())
            : "unknown";
    }

    /// <summary>
    /// True while the driver has told the client it will echo (IAC WILL ECHO),
    /// so the client is not echoing what the user types. Game thread only.
    /// </summary>
    public bool EchoSuppressed;

    /// <summary>Queue text for the client. Never blocks the game thread.</summary>
    public void Send(string text) => _outbox.Writer.TryWrite(Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n").Replace("\n", "\r\n")));

    /// <summary>
    /// Queue raw bytes (telnet commands) for the client, in order with text
    /// queued by Send. Never blocks the game thread.
    /// </summary>
    public void SendRaw(ReadOnlySpan<byte> bytes) => _outbox.Writer.TryWrite(bytes.ToArray());

    /// <summary>Flush what is queued, then close the socket.</summary>
    public void Close() => _outbox.Writer.TryComplete();

    public async Task RunAsync()
    {
        var stream = _client.GetStream();
        var writer = Task.Run(async () =>
        {
            try
            {
                await foreach (var bytes in _outbox.Reader.ReadAllAsync())
                    await stream.WriteAsync(bytes);
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally { _client.Close(); } // also unblocks the reader
        });

        try
        {
            await ReadLinesAsync(stream);
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }

        _outbox.Writer.TryComplete();
        await writer;
        _kernel.Post(() => _kernel.OnDisconnect(this));
    }

    private async Task ReadLinesAsync(NetworkStream stream)
    {
        var buffer = new byte[2048];
        var line = new List<byte>();
        var telnet = TelnetState.Data;

        while (true)
        {
            var read = await stream.ReadAsync(buffer);
            if (read == 0) return;
            for (var i = 0; i < read; i++)
            {
                var b = buffer[i];
                switch (telnet)
                {
                    case TelnetState.Data when b == 255: telnet = TelnetState.Iac; break;
                    case TelnetState.Data when b == (byte)'\n':
                        var text = Encoding.UTF8.GetString(line.ToArray());
                        line.Clear();
                        _kernel.Post(() => _kernel.OnLine(this, text));
                        break;
                    case TelnetState.Data when b is (byte)'\r' or 0: break;
                    case TelnetState.Data when b is 8 or 127:
                        if (line.Count > 0) line.RemoveAt(line.Count - 1);
                        break;
                    case TelnetState.Data:
                        if (line.Count < MaxLineLength && (b >= 32 || b == 9)) line.Add(b);
                        break;

                    // Telnet negotiation is recognised and discarded.
                    case TelnetState.Iac when b == 255: line.Add(255); telnet = TelnetState.Data; break;
                    case TelnetState.Iac when b is >= 251 and <= 254: telnet = TelnetState.Option; break;
                    case TelnetState.Iac when b == 250: telnet = TelnetState.Subnegotiation; break;
                    case TelnetState.Iac: telnet = TelnetState.Data; break;
                    case TelnetState.Option: telnet = TelnetState.Data; break;
                    case TelnetState.Subnegotiation when b == 255: telnet = TelnetState.SubnegotiationIac; break;
                    case TelnetState.Subnegotiation: break;
                    case TelnetState.SubnegotiationIac: telnet = b == 240 ? TelnetState.Data : TelnetState.Subnegotiation; break;
                }
            }
        }
    }

    private enum TelnetState { Data, Iac, Option, Subnegotiation, SubnegotiationIac }
}

internal sealed class TelnetServer
{
    private readonly Kernel _kernel;
    private readonly TcpListener _listener;

    public TelnetServer(Kernel kernel, int port)
    {
        _kernel = kernel;
        // Listen on IPv6 and IPv4 together: a domain with an AAAA record sends many clients to IPv6 first.
        // Fall back to IPv4 alone on a host with IPv6 switched off.
        try
        {
            _listener = new TcpListener(IPAddress.IPv6Any, port);
            _listener.Server.DualMode = true;
        }
        catch (SocketException)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }
    }

    public void Start()
    {
        _listener.Start();
        Log.Info($"listening on port {((IPEndPoint)_listener.LocalEndpoint).Port}");
        _ = AcceptLoopAsync();
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(); }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }

            client.NoDelay = true;
            var connection = new Connection(client, _kernel);
            Log.Info($"connection from {connection.RemoteAddress}");
            _kernel.Post(() => _kernel.OnConnect(connection));
            _ = connection.RunAsync();
        }
    }
}
