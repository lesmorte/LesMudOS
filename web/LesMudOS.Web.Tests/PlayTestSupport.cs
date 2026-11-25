using Xunit;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;

namespace LesMudOS.Web.Tests;

/// <summary>A clock the test moves by hand.</summary>
public sealed class ManualTime : TimeProvider
{
    private long _ticks;
    public override long TimestampFrequency => 1000;
    public override long GetTimestamp() => Interlocked.Read(ref _ticks);
    public void Advance(TimeSpan by) => Interlocked.Add(ref _ticks, (long)by.TotalMilliseconds);
}

/// <summary>A game the bridge can connect to: a loopback listener whose connections the test reads and writes.</summary>
public sealed class FakeGame : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
    public List<FakeGameConnection> Connections { get; } = new();
    private readonly CancellationTokenSource _cts = new();

    public FakeGame()
    {
        _listener.Start();
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var c = await _listener.AcceptTcpClientAsync(_cts.Token);
                    var fc = new FakeGameConnection(c);
                    lock (Connections) Connections.Add(fc);
                }
            }
            catch (Exception) { }
        });
    }

    public async Task<FakeGameConnection> NextAsync(int index = 0, int timeoutMs = 5000)
    {
        var end = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < end)
        {
            lock (Connections) if (Connections.Count > index) return Connections[index];
            await Task.Delay(10);
        }
        throw new TimeoutException("the bridge never connected to the fake game");
    }

    public void Dispose() { _cts.Cancel(); _listener.Stop(); lock (Connections) foreach (var c in Connections) c.Dispose(); }
}

public sealed class FakeGameConnection : IDisposable
{
    private readonly TcpClient _c;
    private readonly List<byte> _received = new();
    private readonly object _lock = new();
    public bool PeerClosed { get; private set; }

    public FakeGameConnection(TcpClient c)
    {
        _c = c;
        _ = Task.Run(async () =>
        {
            var buf = new byte[4096];
            try
            {
                while (true)
                {
                    var n = await _c.GetStream().ReadAsync(buf);
                    if (n == 0) break;
                    lock (_lock) _received.AddRange(buf.AsSpan(0, n).ToArray());
                }
            }
            catch (Exception) { }
            PeerClosed = true;
        });
    }

    public byte[] Received { get { lock (_lock) return _received.ToArray(); } }
    public string ReceivedText => Encoding.Latin1.GetString(Received);
    public async Task SendAsync(params byte[][] parts) { foreach (var p in parts) await _c.GetStream().WriteAsync(p); await _c.GetStream().FlushAsync(); }
    public Task SendTextAsync(string text) => SendAsync(Encoding.UTF8.GetBytes(text));
    public void CloseFromGame() { try { _c.Client.Shutdown(SocketShutdown.Both); } catch (Exception) { } _c.Close(); }
    public void Dispose() { try { _c.Close(); } catch (Exception) { } }

    public async Task<bool> WaitForAsync(Func<byte[], bool> pred, int timeoutMs = 5000)
    {
        var end = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < end) { if (pred(Received)) return true; await Task.Delay(10); }
        return pred(Received);
    }
    public async Task<bool> WaitForTextAsync(string needle, int timeoutMs = 5000) => await WaitForAsync(b => Encoding.Latin1.GetString(b).Contains(needle), timeoutMs);
}

/// <summary>A web player: a WebSocket to the in-memory site, with helpers to read the frames.</summary>
public sealed class Player : IDisposable
{
    public WebSocket Socket { get; }
    public Player(WebSocket s) { Socket = s; }

    public static async Task<(Player? Player, int Status)> TryConnect(Site site, string? origin = "https://example.test", string? clientIp = "203.0.113.7",
        string[]? protocols = null, Action<HttpRequestHeaders>? more = null)
    {
        var client = site.Server.CreateWebSocketClient();
        foreach (var p in protocols ?? ["hollowmere.v1"]) client.SubProtocols.Add(p);
        client.ConfigureRequest = r =>
        {
            if (origin != null) r.Headers["Origin"] = origin;
            if (clientIp != null) r.Headers["X-Hollowmere-Client-Ip"] = clientIp;
            more?.Invoke(new HttpRequestHeaders(r.Headers));
        };
        try
        {
            var ws = await client.ConnectAsync(new Uri("ws://localhost/play/ws"), CancellationToken.None);
            return (new Player(ws), 101);
        }
        catch (InvalidOperationException e) when (e.Message.Contains("status code: "))
        {
            var i = e.Message.IndexOf("status code: ", StringComparison.Ordinal) + 13;
            return (null, int.Parse(e.Message.AsSpan(i, 3)));
        }
    }

    public static async Task<Player> Connect(Site site, string clientIp = "203.0.113.7")
    {
        var (p, status) = await TryConnect(site, clientIp: clientIp);
        Assert.True(p != null, "handshake refused with " + status);
        return p!;
    }

    public async Task SendAsync(object frame)
        => await Socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(frame), WebSocketMessageType.Text, true, CancellationToken.None);
    public async Task SendRawAsync(byte[] bytes, WebSocketMessageType type = WebSocketMessageType.Text)
        => await Socket.SendAsync(bytes, type, true, CancellationToken.None);
    public Task In(string line) => SendAsync(new { t = "in", d = line });

    /// <summary>The next text frame as JSON, or null when the socket closed (CloseStatus is then set).</summary>
    public async Task<JsonElement?> NextAsync(int timeoutMs = 5000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        var buf = new byte[1 << 16];
        var len = 0;
        while (true)
        {
            var r = await Socket.ReceiveAsync(buf.AsMemory(len), cts.Token);
            if (r.MessageType == WebSocketMessageType.Close) return null;
            len += r.Count;
            if (r.EndOfMessage) break;
        }
        return JsonDocument.Parse(buf.AsMemory(0, len)).RootElement.Clone();
    }

    /// <summary>All frames until one satisfies the predicate (inclusive).</summary>
    public async Task<List<JsonElement>> UntilAsync(Func<JsonElement, bool> pred, int timeoutMs = 5000)
    {
        var list = new List<JsonElement>();
        while (true)
        {
            var f = await NextAsync(timeoutMs);
            if (f == null) throw new InvalidOperationException("socket closed; frames so far: " + string.Join(" ", list));
            list.Add(f.Value);
            if (pred(f.Value)) return list;
        }
    }

    public Task<List<JsonElement>> UntilType(string t) => UntilAsync(f => f.GetProperty("t").GetString() == t);

    /// <summary>Read until the server closes; returns the frames seen and the close status.</summary>
    public async Task<(List<JsonElement> Frames, int Code)> UntilClosedAsync(int timeoutMs = 5000)
    {
        var list = new List<JsonElement>();
        while (true)
        {
            var f = await NextAsync(timeoutMs);
            if (f == null) return (list, (int?)Socket.CloseStatus ?? 0);
            list.Add(f.Value);
        }
    }

    public void Dispose() { Socket.Dispose(); }
}

public sealed class HttpRequestHeaders(Microsoft.AspNetCore.Http.IHeaderDictionary h)
{
    public void Set(string name, string value) => h[name] = value;
    public void Remove(string name) => h.Remove(name);
    public void Add(string name, string value) => h[name] = Microsoft.Extensions.Primitives.StringValues.Concat(h[name], value);
}

public static class T
{
    public static string S(this JsonElement e, string name) => e.GetProperty(name).GetString()!;
    public static bool IsT(this JsonElement e, string t) => e.GetProperty("t").GetString() == t;
    public const byte IAC = 255, WILL = 251, WONT = 252, DO = 253, DONT = 254, SB = 250, SE = 240;
    public static byte[] Bytes(params object[] parts)
    {
        var l = new List<byte>();
        foreach (var p in parts)
            switch (p)
            {
                case byte b: l.Add(b); break;
                case int i: l.Add((byte)i); break;
                case string s: l.AddRange(Encoding.UTF8.GetBytes(s)); break;
                case byte[] a: l.AddRange(a); break;
            }
        return l.ToArray();
    }
    public static byte[] Gmcp(string package, string json) => Bytes(IAC, SB, 201, package + (json.Length > 0 ? " " + json : ""), IAC, SE);
}
