using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;

namespace LesMudOS.Web.Play;

/// <summary>One web player: a WebSocket from the page and a telnet connection to the game, with frames and telnet translated between them.
/// docs/specs/web-client.md sections 2.4 to 2.10 and 3. Nothing a player types, no frame body and no game text is ever logged.</summary>
internal sealed class BridgeSession
{
    private readonly PlayRuntime _rt;
    private readonly PlayOptions _o;
    private readonly HttpContext _ctx;
    private readonly string _key, _who, _origin;
    private readonly ILogger _log;
    private readonly CancellationToken _stopping;
    private readonly TimeProvider _time;

    private WebSocket _ws = null!;
    private TcpClient? _tcp;
    private NetworkStream? _net;
    private readonly SemaphoreSlim _gameWrite = new(1, 1);
    private readonly Channel<byte[]> _out = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _loops = new();
    private readonly CancellationTokenSource _writerStop = new();
    // Cancelling a pending receive aborts a WebSocket (Kestrel), which would lose the closing frames: the receive is only ever ended by the
    // close handshake or by Abort().
    private readonly CancellationTokenSource _recvStop = new();
    private readonly TelnetParser _parser = new();
    private readonly LineBucket _lines, _pings, _frames;

    private long _queued, _bytesOut, _linesIn;
    private int _ended;          // 0 running, 1 ending: the first caller of End wins
    private string _reason = "client-closed";
    private int _code = 1000;
    private string? _sysCode;
    private bool _drain;
    private bool _gmcpOn;
    private int _replies;
    private int _badInARow, _droppedInARow, _floodInARow;
    private long _lastRateNoticeMs = long.MinValue;
    private long _lastClientMs;
    private readonly HashSet<string> _loggedDrops = new();

    public BridgeSession(PlayRuntime rt, HttpContext ctx, string key, string who, string origin, ILogger log, CancellationToken stopping)
    {
        _rt = rt; _o = rt.Options; _ctx = ctx; _key = key; _who = who; _origin = origin; _log = log; _stopping = stopping; _time = rt.Time;
        _lines = new LineBucket(_o.InputLinesPerSecond, _o.InputBurst, _time);
        _pings = new LineBucket(5, 10, _time);
        _frames = new LineBucket(Math.Max(50, _o.InputLinesPerSecond * 5), Math.Max(100, _o.InputBurst * 5), _time); // every kind of frame, so no frame type can be flooded
    }

    private long NowMs() => (long)(_time.GetTimestamp() / (double)_time.TimestampFrequency * 1000);

    /// <summary>A refusal that the page can read (a browser script cannot see an HTTP status): upgrade, say why, close.</summary>
    public static async Task RefuseAsync(WebSocket ws, string code, int closeCode, PlayOptions o)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await ws.SendAsync(Frames.Sys(code, Frames.Message(code)), WebSocketMessageType.Text, true, cts.Token);
            await ws.CloseAsync((WebSocketCloseStatus)closeCode, Frames.Message(code), cts.Token);
        }
        catch (Exception) { ws.Abort(); }
    }

    public async Task RunAsync(Microsoft.AspNetCore.Http.WebSocketAcceptContext accept)
    {
        try { _ws = await _ctx.WebSockets.AcceptWebSocketAsync(accept); }
        catch (Exception)
        {
            _rt.Gate.Release(_key);   // the page went away during the handshake: give the slot back
            _log.LogInformation("play: refused {Who} reason=handshake-failed", _who);
            return;
        }
        _lastClientMs = NowMs();
        _log.LogInformation("play: open {Who} origin={Origin} web={Active}/{Max}", _who, OriginCheck.Safe(_origin), _rt.Gate.Active, _o.MaxConnections);
        var started = _time.GetTimestamp();
        using var stopReg = _stopping.Register(() => End("shutdown", 1012, "restart", drain: true));
        try
        {
            if (!await ConnectGameAsync())
            {
                End("unavailable", 1013, "unavailable", drain: true);
            }
            else
            {
                Enqueue(Frames.Hello(_o.MaxInputBytes, _o.PingSeconds));
            }
            var writer = WriterLoopAsync();
            Task game = Task.CompletedTask, browser = Task.CompletedTask, watchdog = Task.CompletedTask;
            if (_net != null)
            {
                game = GameLoopAsync(); browser = BrowserLoopAsync(); watchdog = WatchdogLoopAsync();
                await Task.WhenAny(game, browser, watchdog);
            }
            End("client-closed", 1000, null, drain: false); // only matters if nothing ended the session yet
            _loops.Cancel();
            CloseGame();
            await Task.WhenAll(Quiet(game), Quiet(watchdog));
            await FinishAsync(writer, browser);
        }
        catch (Exception e)
        {
            _log.LogWarning("play: error {Who} {Type}", _who, e.GetType().Name);
            _ws.Abort();
        }
        finally
        {
            CloseGame();
            _rt.Gate.Release(_key);
            var lasted = (long)((_time.GetTimestamp() - started) / (double)_time.TimestampFrequency);
            _log.LogInformation("play: close {Who} lasted={Lasted}s lines_in={Lines} bytes_out={Bytes} reason={Reason} code={Code}",
                _who, lasted, Interlocked.Read(ref _linesIn), Interlocked.Read(ref _bytesOut), _reason, _code);
            _ws.Dispose();
            _loops.Dispose(); _writerStop.Dispose(); _recvStop.Dispose(); _gameWrite.Dispose();
        }
    }

    private static async Task Quiet(Task t) { try { await t; } catch (Exception) { } }

    /// <summary>The first caller decides why and how the session ends.</summary>
    private void End(string reason, int code, string? sysCode, bool drain)
    {
        if (Interlocked.CompareExchange(ref _ended, 1, 0) != 0) return;
        _reason = reason; _code = code; _sysCode = sysCode; _drain = drain;
        try { _loops.Cancel(); } catch (ObjectDisposedException) { }
    }

    private void Dropped(string reason)
    {
        if (_loggedDrops.Add(reason)) _log.LogInformation("play: dropped {Who} reason={Reason}", _who, reason);
    }

    // ---- connecting ----

    private async Task<bool> ConnectGameAsync()
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_loops.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            _tcp = new TcpClient { NoDelay = true };
            await _tcp.ConnectAsync(_o.GameBridgeHost, _o.GameBridgePort, cts.Token);
            _net = _tcp.GetStream();
            if (!_o.PlainMode)
            {
                var line = ProxyProtocol.Line(IPAddress.Parse(_who), _o.GameBridgePort);
                await _net.WriteAsync(Encoding.ASCII.GetBytes(line), _loops.Token);
            }
            return true;
        }
        catch (Exception e) when (e is SocketException or OperationCanceledException or IOException or ObjectDisposedException)
        {
            CloseGame();
            return false;
        }
    }

    private void CloseGame()
    {
        try { _tcp?.Close(); } catch (Exception) { }
    }

    // ---- frames to the browser ----

    /// <summary>Queue a frame for the browser. Never blocks: a page that does not read ends the session at MaxQueuedBytes.</summary>
    private bool Enqueue(byte[] frame)
    {
        if (Volatile.Read(ref _ended) != 0 && !_drain) return false;
        if (Interlocked.Add(ref _queued, frame.Length) > _o.MaxQueuedBytes)
        {
            Dropped("slow");
            End("slow", 1008, "slow", drain: false);
            return false;
        }
        return _out.Writer.TryWrite(frame);
    }

    private async Task WriterLoopAsync()
    {
        try
        {
            await foreach (var f in _out.Reader.ReadAllAsync(_writerStop.Token))
            {
                Interlocked.Add(ref _queued, -f.Length);
                await _ws.SendAsync(f, WebSocketMessageType.Text, true, _writerStop.Token);
                Interlocked.Add(ref _bytesOut, f.Length);
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException or ObjectDisposedException or InvalidOperationException)
        {
            End("write-error", 1006, null, drain: false);
        }
    }

    /// <summary>After the loops stopped: say why (if there is something to say), let the queue empty, close the socket with the right code.</summary>
    private async Task FinishAsync(Task writer, Task browser)
    {
        var grace = TimeSpan.FromSeconds(Math.Max(1, _reason == "shutdown" ? _o.ShutdownGraceSeconds : 3));
        if (_sysCode != null && _drain) _out.Writer.TryWrite(Frames.Sys(_sysCode, Frames.Message(_sysCode)));
        _out.Writer.TryComplete();
        if (!_drain) _writerStop.CancelAfter(_reason == "slow" ? TimeSpan.Zero : TimeSpan.FromMilliseconds(200));
        else _writerStop.CancelAfter(grace);
        await Quiet(writer);
        if (_ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                var text = _sysCode != null ? Frames.Message(_sysCode) : "";
                var code = _code is 1005 or 1006 ? 1001 : _code;
                await _ws.CloseOutputAsync(_ws.State == WebSocketState.CloseReceived ? WebSocketCloseStatus.NormalClosure : (WebSocketCloseStatus)code, text, cts.Token);
            }
            catch (Exception) { _ws.Abort(); }
        }
        // The page's answer to our close frame ends its receive loop; if it never comes, stop waiting.
        var quiet = Quiet(browser);
        if (await Task.WhenAny(quiet, Task.Delay(TimeSpan.FromSeconds(2))) != quiet) { _ws.Abort(); _recvStop.Cancel(); await quiet; }
    }

    // ---- game to browser ----

    private async Task GameLoopAsync()
    {
        var buf = new byte[4096];
        var events = new List<TelnetEvent>();
        try
        {
            while (!_loops.IsCancellationRequested)
            {
                var n = await _net!.ReadAsync(buf, _loops.Token);
                if (n == 0)
                {
                    End("game-closed", 1000, "game_closed", drain: true);
                    return;
                }
                events.Clear();
                _parser.Feed(buf.AsSpan(0, n), events);
                foreach (var ev in events) await HandleEventAsync(ev);
            }
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            End("game-closed", 1000, "game_closed", drain: true);
        }
    }

    private async Task HandleEventAsync(TelnetEvent ev)
    {
        switch (ev)
        {
            case TextEvent t:
                foreach (var f in Frames.OutChunks(t.Text)) Enqueue(f);
                break;
            case GmcpEvent g:
                byte[] frame;
                try { frame = Frames.Gmcp(g.Package, g.Data); }
                catch (Exception e) when (e is ArgumentException or InvalidOperationException) { break; } // text the JSON writer will not re-encode: the message is dropped
                Enqueue(frame);
                break;
            case MaskEvent m:
                Enqueue(Frames.Mask(m.On));
                break;
            case NegotiationEvent n:
                await AnswerAsync(n);
                break;
        }
    }

    /// <summary>The bridge accepts GMCP (and tells the game what it wants) and ECHO; it refuses everything else the game offers or asks.</summary>
    private async Task AnswerAsync(NegotiationEvent n)
    {
        if (n.Command is TelnetParser.Wont or TelnetParser.Dont) return;
        if (_replies >= 500) return; // a game stuck in a negotiation loop gets no more answers
        _replies++;
        if (n.Command == TelnetParser.Will && n.Option == TelnetParser.Gmcp)
        {
            if (_gmcpOn) return;
            _gmcpOn = true;
            await WriteGameAsync([TelnetParser.Iac, TelnetParser.Do, TelnetParser.Gmcp]);
            await WriteGameAsync(GmcpMessage("Core.Hello", "{\"client\":\"Hollowmere Web\",\"version\":\"1\"}"));
            await WriteGameAsync(GmcpMessage("Core.Supports.Set", "[\"Room 1\",\"Char 1\",\"Comm.Channel 1\"]"));
        }
        else if (n.Command == TelnetParser.Will && n.Option == TelnetParser.Echo)
            await WriteGameAsync([TelnetParser.Iac, TelnetParser.Do, TelnetParser.Echo]);
        else if (n.Command == TelnetParser.Will)
            await WriteGameAsync([TelnetParser.Iac, TelnetParser.Dont, n.Option]);
        else if (n.Command == TelnetParser.Do)
            await WriteGameAsync([TelnetParser.Iac, TelnetParser.Wont, n.Option]);
    }

    public static byte[] GmcpMessage(string package, string json)
    {
        var body = new List<byte> { TelnetParser.Iac, TelnetParser.Sb, TelnetParser.Gmcp };
        body.AddRange(Encoding.ASCII.GetBytes(package + " " + json));
        body.Add(TelnetParser.Iac); body.Add(TelnetParser.Se);
        return body.ToArray();
    }

    private async Task WriteGameAsync(byte[] bytes)
    {
        await _gameWrite.WaitAsync(_loops.Token);
        try { await _net!.WriteAsync(bytes, _loops.Token); }
        finally { _gameWrite.Release(); }
    }

    // ---- browser to game ----

    private async Task BrowserLoopAsync()
    {
        var buffer = new byte[_o.MaxFrameBytes + 1];
        try
        {
            while (_ws.State is WebSocketState.Open or WebSocketState.CloseSent)
            {
                var len = 0;
                ValueWebSocketReceiveResult r;
                do
                {
                    r = await _ws.ReceiveAsync(buffer.AsMemory(len), _recvStop.Token);
                    if (r.MessageType == WebSocketMessageType.Close)
                    {
                        _code = _ws.CloseStatus is { } cs ? (int)cs : 1005;
                        End("client-closed", _code, null, drain: false);
                        return;
                    }
                    if (r.MessageType == WebSocketMessageType.Binary)
                    {
                        Dropped("binary");
                        End("binary-frame", 1003, null, drain: false);
                        return;
                    }
                    len += r.Count;
                    if (len > _o.MaxFrameBytes)
                    {
                        Dropped("frame-too-big");
                        End("frame-too-big", 1009, null, drain: false);
                        return;
                    }
                } while (!r.EndOfMessage);
                _lastClientMs = NowMs();
                if (!_frames.Take())
                {
                    Dropped("frame-flood");
                    if (++_floodInARow >= 200) { End("rate-abuse", 1008, null, drain: false); return; }
                    continue;   // dropped unread: not even parsed
                }
                _floodInARow = 0;
                await HandleClientAsync(Frames.ParseClient(buffer.AsSpan(0, len), _o.MaxInputBytes));
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException or IOException or SocketException or ObjectDisposedException)
        {
            End("client-dropped", 1006, null, drain: false);
        }
    }

    private async Task HandleClientAsync(ClientFrame f)
    {
        switch (f)
        {
            case InFrame i:
                _badInARow = 0;
                if (!_lines.Take())
                {
                    Dropped("rate");
                    var now = NowMs();
                    if (_lastRateNoticeMs == long.MinValue || now - _lastRateNoticeMs >= 5000)
                    {
                        _lastRateNoticeMs = now;
                        Enqueue(Frames.Sys("rate", Frames.Message("rate")));
                    }
                    if (++_droppedInARow >= 100) End("rate-abuse", 1008, null, drain: false);
                    return;
                }
                _droppedInARow = 0;
                Interlocked.Increment(ref _linesIn);
                await WriteGameAsync(Encoding.UTF8.GetBytes(i.Line + "\r\n"));
                break;
            case PingFrame p:
                _badInARow = 0;
                if (_pings.Take()) Enqueue(Frames.Pong(p.N)); // answered here, never written to the game: not player input
                break;
            case SizeFrame:
                _badInARow = 0; // accepted and dropped: the game asks for no window size today
                break;
            case BadFrame b:
                Dropped(b.Code == "too_long" ? "too-long" : "bad-frame");
                Enqueue(Frames.Sys(b.Code, Frames.Message(b.Code)));
                if (b.Code == "bad_frame" && ++_badInARow >= 3) End("bad-frames", 1008, null, drain: false);
                break;
        }
    }

    // ---- silence ----

    private async Task WatchdogLoopAsync()
    {
        try
        {
            while (!_loops.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), _time, _loops.Token);
                if (NowMs() - Volatile.Read(ref _lastClientMs) > _o.ClientSilenceSeconds * 1000L)
                {
                    End("silence", 1001, null, drain: false);
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
