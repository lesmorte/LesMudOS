using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LesMudOS.Driver.Net;

/// <summary>
/// The entry points the accept loops call (docs/specs/web-client.md section 4): configure from the command line and the environment, admit or
/// refuse a new connection, start the loopback bridge listener, release a connection's slot. TelnetServer.cs and Program.cs only call these.
/// </summary>
internal static class NetPolicy
{
    private static NetSettings _settings = new();
    private static ConnectionLimits? _limits;
    private static readonly object _gate = new();
    private static readonly LimitedLog RefusalLog = new(10);
    private static readonly LimitedLog InputLog = new(10);
    public static readonly LimitedLog BridgeLog = new(10);
    public static readonly LimitedLog ThrottleLog = new(20);

    public static NetSettings Settings => _settings;

    /// <summary>Command line flag (--bridge-port N, --max-connections N, --max-per-address N). Returns false for any other flag.</summary>
    public static bool SetArgument(string flag, string value) => _settings.ApplyArgument(flag, value);

    /// <summary>Read the environment (which wins over the command line) and build the limits. Called once at start.</summary>
    public static void Configure()
    {
        lock (_gate)
        {
            _settings.ApplyEnvironment();
            ThrottleSettings.Load();
            _limits = new ConnectionLimits(_settings.MaxConnections, _settings.MaxPerAddress, _settings.ConnectsPerMinute);
            Log.Info($"network limits: {Describe(_settings.MaxConnections)} connections, {Describe(_settings.MaxPerAddress)} per address, " +
                     $"{Describe(_settings.ConnectsPerMinute)} new per address per minute, input {Describe((int)_settings.LinesPerSecond)} lines/s (burst {_settings.InputBurst:0.##})" +
                     (_settings.BridgePort > 0 ? $", bridge on port {_settings.BridgePort}" : ", bridge off"));
        }
    }

    private static string Describe(int n) => n <= 0 ? "unlimited" : n.ToString();

    private static ConnectionLimits Limits
    {
        get { if (_limits is null) Configure(); return _limits!; }
    }

    internal static LineBucket NewLineBucket()
        => new(_settings.LinesPerSecond, _settings.InputBurst, _settings.BytesPerSecond, _settings.OverflowSeconds, Environment.TickCount64, _settings.InputStats);

    internal static void LogInput(string text) => InputLog.Write(text);

    /// <summary>
    /// A new connection (from the public listener, or from the bridge after its PROXY line). True: it holds a slot and goes on. False: it has been
    /// sent one plain line and closed, one (rate-limited) log line has been written, and the caller must not use it again.
    /// </summary>
    public static bool Admit(Connection connection, TcpClient client)
    {
        if (connection.ClientIp is null && client.Client.RemoteEndPoint is IPEndPoint peer) connection.SetPeerAddress(peer.Address);
        if (connection.ClientIp is null) { Refuse(client, "Connection refused.\r\n", "no address", connection.RemoteAddress); return false; }
        var key = ConnectionLimits.AddressKey(connection.ClientIp);
        var result = Limits.TryAdmit(key);
        if (result == Admission.Ok) { connection.SlotKey = key; return true; }
        var (text, reason) = result switch
        {
            Admission.Full => ("The game is full. Please try again in a few minutes.\r\n", "full"),
            Admission.TooManyFromAddress => ("Too many connections from your address.\r\n", "too many from address"),
            _ => ("Too many connection attempts from your address. Please wait a minute.\r\n", "connecting too fast"),
        };
        Refuse(client, text, reason, connection.ClientIp + (connection.ViaWeb ? " (web)" : ""));
        return false;
    }

    private static void Refuse(TcpClient client, string text, string reason, string address)
    {
        try
        {
            var socket = client.Client;
            socket.SendTimeout = 250;
            socket.Send(Encoding.ASCII.GetBytes(text));
            socket.Shutdown(SocketShutdown.Send);
        }
        catch (Exception e) when (e is SocketException or ObjectDisposedException or InvalidOperationException) { }
        try { client.Close(); } catch (Exception e) when (e is SocketException or ObjectDisposedException) { }
        RefusalLog.Write($"connection refused: {reason}: {address}");
    }

    internal static void Release(Connection connection)
    {
        var key = Interlocked.Exchange(ref connection.SlotKey, null);
        if (key != null) Limits.Release(key);
    }

    private static BridgeListener? _bridge;

    /// <summary>Start the loopback bridge listener if it is switched on. Called where the public listener starts.</summary>
    public static void StartBridge(Kernel kernel) => _bridge = BridgeListener.TryStart(kernel, _settings);
}
