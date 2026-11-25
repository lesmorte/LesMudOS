using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using static LesMudOS.Web.Tests.T;

namespace LesMudOS.Web.Tests;

/// <summary>The bridge against a fake game: handshake checks, the PROXY line, telnet to frames, input rules, limits, shutdown, logging.</summary>
public class PlayBridgeTests
{
    private static Site Open(FakeGame game, Dictionary<string, string?>? extra = null, CaptureLogs? logs = null, TimeProvider? time = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Site:Play:Enabled"] = "true",
            ["Site:Play:GameBridgePort"] = game.Port.ToString(),
            ["Site:Play:KillSwitchFile"] = Path.Combine(Path.GetTempPath(), "hollowmere-no-such-" + Guid.NewGuid().ToString("N")),
        };
        if (extra != null) foreach (var kv in extra) settings[kv.Key] = kv.Value;
        return new Site(null, settings) { Logs = logs, Time = time };
    }

    private static async Task<(Player P, FakeGameConnection G)> Join(Site site, FakeGame game, string ip = "203.0.113.7")
    {
        int before; lock (game.Connections) before = game.Connections.Count;
        var p = await Player.Connect(site, ip);
        var g = await game.NextAsync(before);
        return (p, g);
    }

    // ---------------- handshake ----------------

    [Fact]
    public async Task An_allowed_origin_with_the_subprotocol_and_an_address_is_accepted_and_gets_hello_first()
    {
        using var game = new FakeGame(); using var site = Open(game);
        var (p, g) = await Join(site, game);
        using var _ = p;
        Assert.Equal("hollowmere.v1", p.Socket.SubProtocol);
        var hello = await p.NextAsync();
        Assert.Equal("{\"t\":\"hello\",\"v\":1,\"max_in\":4096,\"ping\":25}", hello!.Value.GetRawText());
    }

    [Theory]
    [InlineData("https://evil.example", 403)]
    [InlineData("https://example.test:8443", 403)]
    [InlineData("http://example.test", 403)]
    [InlineData("https://www.example.test", 403)]
    [InlineData("null", 403)]
    [InlineData(null, 403)]
    public async Task A_wrong_missing_or_null_origin_is_refused_before_any_socket(string? origin, int status)
    {
        using var game = new FakeGame(); using var site = Open(game);
        var (p, got) = await Player.TryConnect(site, origin: origin);
        Assert.Null(p);
        Assert.Equal(status, got);
        await Task.Delay(100);
        Assert.Empty(game.Connections); // the game was never contacted
    }

    [Fact]
    public async Task A_wrong_or_missing_subprotocol_is_refused()
    {
        using var game = new FakeGame(); using var site = Open(game);
        Assert.Equal(403, (await Player.TryConnect(site, protocols: ["other"])).Status);
        var client = site.Server.CreateWebSocketClient();
        client.ConfigureRequest = r => { r.Headers["Origin"] = "https://example.test"; r.Headers["X-Hollowmere-Client-Ip"] = "203.0.113.7"; };
        var e = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync(new Uri("ws://localhost/play/ws"), CancellationToken.None));
        Assert.Contains("403", e.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not an address")]
    [InlineData("203.0.113.7, 198.51.100.1")]
    [InlineData("203.0.113.7:4000")]
    [InlineData("[2001:db8::1]")]
    [InlineData("203.0.113.300")]
    public async Task A_missing_or_malformed_client_address_is_refused_when_required(string? header)
    {
        using var game = new FakeGame(); using var site = Open(game);
        Assert.Equal(403, (await Player.TryConnect(site, clientIp: header)).Status);
    }

    [Fact]
    public async Task A_repeated_address_header_is_refused()
    {
        using var game = new FakeGame(); using var site = Open(game);
        var (p, status) = await Player.TryConnect(site, clientIp: "203.0.113.7", more: h => h.Add("X-Hollowmere-Client-Ip", "198.51.100.1"));
        Assert.Null(p); Assert.Equal(403, status);
    }

    [Fact]
    public async Task Without_the_requirement_a_missing_header_is_accepted()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:RequireClientIpHeader"] = "false" });
        var (p, status) = await Player.TryConnect(site, clientIp: null);
        Assert.NotNull(p); Assert.Equal(101, status);
        p!.Dispose();
    }

    [Fact]
    public void A_peer_that_is_not_loopback_cannot_claim_an_address()
    {
        var ctx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        ctx.Request.Headers["X-Hollowmere-Client-Ip"] = "203.0.113.7";
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.9");
        Assert.Null(LesMudOS.Web.Play.ClientAddress.FromRequest(ctx));
        ctx.Connection.RemoteIpAddress = null;
        Assert.Null(LesMudOS.Web.Play.ClientAddress.FromRequest(ctx));
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.IPv6Loopback;
        Assert.Equal("203.0.113.7", LesMudOS.Web.Play.ClientAddress.FromRequest(ctx)!.ToString());
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("::ffff:127.0.0.1");
        Assert.NotNull(LesMudOS.Web.Play.ClientAddress.FromRequest(ctx));
    }

    [Fact]
    public async Task Disabled_gives_404_and_the_status_and_the_page_say_so()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:Enabled"] = "false" });
        Assert.Equal(404, (await Player.TryConnect(site)).Status);
        var (_, status, r) = await site.Get("/play/status");
        Assert.Equal("{\"v\":1,\"enabled\":false,\"full\":false}", status);
        Assert.Equal("no-store", r.Headers.CacheControl!.ToString());
        var (code, page, _) = await site.Get("/play");
        Assert.Equal(200, code);
        Assert.Contains("switched off", page);
        Assert.DoesNotContain("play.js", page);
        Assert.Empty(game.Connections);
    }

    [Fact]
    public async Task The_kill_switch_file_stops_new_connections_without_a_restart_and_is_looked_at_every_five_seconds()
    {
        using var game = new FakeGame();
        var file = Path.Combine(Path.GetTempPath(), "hollowmere-kill-" + Guid.NewGuid().ToString("N"));
        var time = new ManualTime();
        using var site = Open(game, new() { ["Site:Play:KillSwitchFile"] = file }, time: time);
        var (first, _) = await Player.TryConnect(site);
        Assert.NotNull(first);                             // existing sessions continue below
        await File.WriteAllTextAsync(file, "");
        try
        {
            Assert.NotNull((await Player.TryConnect(site)).Player);   // looked at only every 5 s: still on
            time.Advance(TimeSpan.FromSeconds(6));
            var (blocked, status) = await Player.TryConnect(site);
            Assert.Null(blocked); Assert.Equal(404, status);
            Assert.Contains("\"enabled\":false", (await site.Get("/play/status")).Body);
            Assert.Contains("switched off", (await site.Get("/play")).Body);
            await first!.In("still here");
            Assert.True(await (await game.NextAsync()).WaitForTextAsync("still here"));
            File.Delete(file);
            time.Advance(TimeSpan.FromSeconds(6));
            Assert.NotNull((await Player.TryConnect(site)).Player);
        }
        finally { File.Delete(file); }
    }

    // ---------------- the PROXY line ----------------

    [Fact]
    public async Task The_game_receives_exactly_one_proxy_line_first_and_nothing_until_the_player_types()
    {
        using var game = new FakeGame(); using var site = Open(game);
        var (p, g) = await Join(site, game, "203.0.113.7");
        using var _ = p;
        Assert.True(await g.WaitForTextAsync("\r\n"));
        await Task.Delay(300);
        Assert.Equal($"PROXY TCP4 203.0.113.7 127.0.0.1 0 {game.Port}\r\n", g.ReceivedText);
        await p.In("look");
        Assert.True(await g.WaitForTextAsync("look\r\n"));
        Assert.Equal($"PROXY TCP4 203.0.113.7 127.0.0.1 0 {game.Port}\r\nlook\r\n", g.ReceivedText);
    }

    [Fact]
    public async Task An_ipv6_player_gets_a_tcp6_line_and_a_mapped_address_is_written_as_ipv4()
    {
        using var game = new FakeGame(); using var site = Open(game);
        var (p, g) = await Join(site, game, "2001:db8::1");
        using var _ = p;
        Assert.True(await g.WaitForTextAsync("\r\n"));
        Assert.Equal($"PROXY TCP6 2001:db8::1 ::1 0 {game.Port}\r\n", g.ReceivedText);
        var (p2, g2) = await Join(site, game, "::ffff:198.51.100.4");
        using var _2 = p2;
        Assert.True(await g2.WaitForTextAsync("\r\n"));
        Assert.Equal($"PROXY TCP4 198.51.100.4 127.0.0.1 0 {game.Port}\r\n", g2.ReceivedText);
    }

    [Fact]
    public async Task Plain_mode_sends_no_proxy_line_at_all()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await Task.Delay(300);
        Assert.Empty(g.Received);
        await p.In("look");
        Assert.True(await g.WaitForTextAsync("look\r\n"));
        Assert.Equal("look\r\n", g.ReceivedText);
    }

    // ---------------- telnet to frames ----------------

    [Fact]
    public async Task The_gmcp_offer_is_accepted_and_the_bridge_says_hello_and_asks_for_three_packages()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await g.SendAsync(Bytes(IAC, WILL, 201));
        var expect = Bytes(IAC, DO, 201,
            IAC, SB, 201, "Core.Hello {\"client\":\"Hollowmere Web\",\"version\":\"1\"}", IAC, SE,
            IAC, SB, 201, "Core.Supports.Set [\"Room 1\",\"Char 1\",\"Comm.Channel 1\"]", IAC, SE);
        Assert.True(await g.WaitForAsync(b => b.Length >= expect.Length));
        Assert.Equal(expect, g.Received);
        await g.SendAsync(Bytes(IAC, WILL, 201)); // offered again: answered once only
        await Task.Delay(200);
        Assert.Equal(expect.Length, g.Received.Length);
    }

    [Fact]
    public async Task Other_offers_and_requests_are_refused_and_never_shown()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await g.SendAsync(Bytes(IAC, WILL, 24, IAC, DO, 31, IAC, WONT, 5, IAC, DONT, 6, "Hi"));
        var frames = await p.UntilAsync(f => f.IsT("out"));
        Assert.Equal("Hi", frames[^1].S("d"));
        var expect = Bytes(IAC, DONT, 24, IAC, WONT, 31);
        Assert.True(await g.WaitForAsync(b => b.Length >= expect.Length));
        await Task.Delay(100);
        Assert.Equal(expect, g.Received);
    }

    [Fact]
    public async Task Text_mask_and_gmcp_arrive_as_frames_in_the_order_the_game_sent_them()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await g.SendAsync(Bytes("\r\nWelcome.\r\n\u001b[1;96mnorth\u001b[0m\r\nPassword: ", IAC, WILL, 1));
        await g.SendAsync(Bytes(IAC, WONT, 1, "\r\n"), Gmcp("Room.Info", "{\"num\":1,\"name\":\"<b>Square</b>\"}"), Bytes("Alice > "));
        var frames = await p.UntilAsync(f => f.IsT("out") && f.S("d") == "Alice > ");
        var kinds = frames.Select(f => f.S("t") + (f.IsT("mask") ? ":" + f.GetProperty("on").GetBoolean() : "")).ToList();
        Assert.Equal(["hello", "out", "mask:True", "mask:False", "out", "gmcp", "out"], kinds);
        Assert.Equal("\nWelcome.\n\u001b[1;96mnorth\u001b[0m\nPassword: ", frames[1].S("d"));
        Assert.Equal("\n", frames[4].S("d"));
        Assert.Equal("Room.Info", frames[5].S("p"));
        Assert.Equal("<b>Square</b>", frames[5].GetProperty("d").GetProperty("name").GetString());
        Assert.DoesNotContain("<", frames[5].GetRawText());
    }

    [Fact]
    public async Task An_oversized_gmcp_message_is_dropped_and_the_text_around_it_is_kept()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await g.SendAsync(Bytes("one "), Gmcp("Big.Msg", "\"" + new string('x', 30000) + "\""), Bytes("two"));
        var frames = await p.UntilAsync(f => f.IsT("out") && f.S("d") == "two");
        Assert.DoesNotContain(frames, f => f.IsT("gmcp"));
        Assert.Equal("one ", frames.First(f => f.IsT("out")).S("d"));
    }

    [Fact]
    public async Task A_lone_surrogate_escape_in_gmcp_cannot_break_the_session()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await g.SendAsync(Gmcp("Comm.Channel.Text", "{\"text\":\"\\ud800 x\"}"), Bytes("after"));
        var frames = await p.UntilAsync(f => f.IsT("out") && f.S("d") == "after");
        Assert.NotEmpty(frames);
    }

    [Fact]
    public async Task Long_game_output_is_cut_into_frames_and_every_byte_arrives()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        var text = string.Concat(Enumerable.Range(0, 3000).Select(i => "line " + i + " \u00e9\u2500\r\n"));
        await g.SendTextAsync(text);
        var sb = new StringBuilder();
        var expected = text.Replace("\r\n", "\n");
        while (sb.Length < expected.Length)
        {
            var f = (await p.NextAsync())!.Value;
            if (!f.IsT("out")) continue;
            Assert.True(Encoding.UTF8.GetByteCount(f.S("d")) <= 16 * 1024);
            sb.Append(f.S("d"));
        }
        Assert.Equal(expected, sb.ToString());
    }

    // ---------------- input ----------------

    [Fact]
    public async Task A_typed_line_is_written_with_crlf_and_an_empty_line_is_allowed()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await p.In("say h\u00e9llo \u2500 <b>");
        await p.In("");
        Assert.True(await g.WaitForAsync(b => b.Length >= 2 + Encoding.UTF8.GetByteCount("say h\u00e9llo \u2500 <b>") + 2));
        Assert.Equal(Encoding.UTF8.GetBytes("say h\u00e9llo \u2500 <b>\r\n\r\n"), g.Received);
    }

    [Theory]
    [InlineData("{\"t\":\"in\",\"d\":\"a\\nb\"}", "bad_frame")]
    [InlineData("{\"t\":\"in\",\"d\":\"a\\rb\"}", "bad_frame")]
    [InlineData("{\"t\":\"in\",\"d\":\"a\\u0000b\"}", "bad_frame")]
    [InlineData("not json", "bad_frame")]
    [InlineData("{\"t\":\"gmcp\",\"p\":\"Core.Hello\"}", "bad_frame")]
    public async Task A_line_that_could_run_two_commands_or_is_not_a_frame_is_refused_and_nothing_is_sent(string raw, string code)
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await p.SendRawAsync(Encoding.UTF8.GetBytes(raw));
        var frames = await p.UntilType("sys");
        Assert.Equal(code, frames[^1].S("code"));
        await p.In("ok");
        Assert.True(await g.WaitForTextAsync("ok\r\n"));
        Assert.Equal("ok\r\n", g.ReceivedText);
    }

    [Fact]
    public async Task A_line_over_4096_bytes_is_refused_with_too_long_and_a_line_of_exactly_4096_goes_through()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await p.In(new string('a', 4097));
        Assert.Equal("too_long", (await p.UntilType("sys"))[^1].S("code"));
        await p.In(new string('b', 4096));
        Assert.True(await g.WaitForAsync(b => b.Length >= 4098));
        Assert.Equal(new string('b', 4096) + "\r\n", g.ReceivedText);
    }

    [Fact]
    public async Task Three_bad_frames_in_a_row_close_with_1008_but_a_good_frame_between_resets_the_count()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, _) = await Join(site, game);
        using var __ = p;
        await p.SendRawAsync(Encoding.UTF8.GetBytes("x")); await p.SendRawAsync(Encoding.UTF8.GetBytes("x"));
        await p.SendAsync(new { t = "ping", n = 1 });
        await p.SendRawAsync(Encoding.UTF8.GetBytes("x")); await p.SendRawAsync(Encoding.UTF8.GetBytes("x"));
        await p.SendAsync(new { t = "ping", n = 2 });
        await p.UntilAsync(f => f.IsT("pong") && f.GetProperty("n").GetInt32() == 2);
        for (var i = 0; i < 3; i++) await p.SendRawAsync(Encoding.UTF8.GetBytes("x"));
        var (_, code) = await p.UntilClosedAsync();
        Assert.Equal(1008, code);
    }

    [Fact]
    public async Task A_binary_frame_closes_with_1003_and_an_oversized_frame_with_1009()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        await p.SendRawAsync(new byte[] { 1, 2, 3 }, WebSocketMessageType.Binary);
        Assert.Equal(1003, (await p.UntilClosedAsync()).Code);
        Assert.True(await WaitUntil(() => g.PeerClosed)); // the game side is closed too
        p.Dispose();

        var (p2, g2) = await Join(site, game);
        await p2.SendRawAsync(Encoding.UTF8.GetBytes("{\"t\":\"in\",\"d\":\"" + new string('a', 8193 - 17) + "\"}"));
        Assert.Equal(1009, (await p2.UntilClosedAsync()).Code);
        Assert.True(await WaitUntil(() => g2.PeerClosed));
        p2.Dispose();

        var (p3, _) = await Join(site, game);       // exactly 8192 bytes is still a frame (a bad one: too long a line, but read whole)
        await p3.SendRawAsync(Encoding.UTF8.GetBytes("{\"t\":\"in\",\"d\":\"" + new string('a', 8192 - 17) + "\"}"));
        Assert.Equal("too_long", (await p3.UntilType("sys"))[^1].S("code"));
        p3.Dispose();
    }

    private static async Task<bool> WaitUntil(Func<bool> cond, int ms = 5000)
    {
        var end = Environment.TickCount64 + ms;
        while (Environment.TickCount64 < end) { if (cond()) return true; await Task.Delay(20); }
        return cond();
    }

    [Fact]
    public async Task A_ping_is_answered_by_the_bridge_and_never_reaches_the_game()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        for (var i = 1; i <= 5; i++)
        {
            await p.SendAsync(new { t = "ping", n = i });
            var f = (await p.UntilType("pong"))[^1];
            Assert.Equal(i, f.GetProperty("n").GetInt32());
            await p.SendAsync(new { t = "size", cols = 52, rows = 30 });
        }
        await Task.Delay(200);
        Assert.Empty(g.Received);
    }

    [Fact]
    public async Task Typed_lines_over_the_rate_are_dropped_with_a_notice_and_constant_abuse_closes_the_socket()
    {
        using var game = new FakeGame();
        using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true", ["Site:Play:InputLinesPerSecond"] = "1", ["Site:Play:InputBurst"] = "5" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        for (var i = 0; i < 20; i++) await p.In("x" + i);
        var frames = await p.UntilType("sys");
        Assert.Equal("rate", frames[^1].S("code"));
        Assert.True(await g.WaitForAsync(b => b.Length >= 5 * 4));
        await Task.Delay(200);
        var lines = g.ReceivedText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.InRange(lines.Length, 5, 7);                // the burst, plus at most a refill
        for (var i = 0; i < 150; i++) { try { await p.In("y"); } catch (Exception) { break; } }
        Assert.Equal(1008, (await p.UntilClosedAsync()).Code);
    }

    // ---------------- limits ----------------

    [Fact]
    public async Task The_sixth_connection_from_one_address_is_refused_and_another_address_is_not()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var open = new List<Player>();
        for (var i = 0; i < 5; i++) open.Add((await Player.TryConnect(site)).Player!);
        var (p6, status) = await Player.TryConnect(site);
        Assert.Null(p6); Assert.Equal(429, status);
        Assert.NotNull((await Player.TryConnect(site, clientIp: "198.51.100.2")).Player);
        open[0].Dispose();
        await open[0].Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ContinueWith(_ => { });
        Assert.True(await WaitUntil(() => site.Services.GetRequiredService<LesMudOS.Web.Play.PlayRuntime>().Gate.Active <= 5));
        Assert.NotNull((await Player.TryConnect(site)).Player);
    }

    [Fact]
    public async Task The_connect_rate_per_address_is_limited()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true", ["Site:Play:MaxPerAddress"] = "50", ["Site:Play:ConnectsPerMinutePerAddress"] = "4" });
        for (var i = 0; i < 4; i++) { var (p, _) = await Player.TryConnect(site); Assert.NotNull(p); p!.Dispose(); }
        var (blocked, status) = await Player.TryConnect(site);
        Assert.Null(blocked); Assert.Equal(429, status);
    }

    [Fact]
    public async Task When_the_site_is_full_the_page_is_told_and_the_status_says_so()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true", ["Site:Play:MaxConnections"] = "2" });
        var a = await Player.Connect(site, "203.0.113.1"); var b = await Player.Connect(site, "203.0.113.2");
        using var _a = a; using var _b = b;
        Assert.Contains("\"full\":true", (await site.Get("/play/status")).Body);
        var (c, status) = await Player.TryConnect(site, clientIp: "203.0.113.3");
        Assert.Equal(101, status);
        using var _c = c!;
        var (frames, code) = await c!.UntilClosedAsync();
        Assert.Equal("full", frames[^1].S("code"));
        Assert.Equal(1013, code);
        Assert.Equal(2, game.Connections.Count);        // the game never saw the third
    }

    [Fact]
    public async Task A_page_that_does_not_read_is_closed_at_the_queue_limit_and_the_game_side_closed_too()
    {
        using var game = new FakeGame();
        using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true", ["Site:Play:MaxQueuedBytes"] = "20000" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        var chunk = Encoding.UTF8.GetBytes(new string('z', 4000) + "\r\n");
        try { for (var i = 0; i < 3000 && !g.PeerClosed; i++) { await g.SendAsync(chunk); if (i % 50 == 0) await Task.Delay(1); } } catch (Exception) { }
        Assert.True(await WaitUntil(() => g.PeerClosed, 8000), "the bridge should have closed the game connection");
    }

    [Fact]
    public async Task A_page_that_goes_silent_is_closed_with_1001()
    {
        using var game = new FakeGame();
        using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true", ["Site:Play:ClientSilenceSeconds"] = "2" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        var (_, code) = await p.UntilClosedAsync(8000);
        Assert.Equal(1001, code);
        Assert.True(await WaitUntil(() => g.PeerClosed));
    }

    [Fact]
    public async Task Pings_keep_a_quiet_page_alive()
    {
        using var game = new FakeGame();
        using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true", ["Site:Play:ClientSilenceSeconds"] = "2" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        for (var i = 1; i <= 6; i++) { await Task.Delay(700); await p.SendAsync(new { t = "ping", n = i }); }
        await p.UntilAsync(f => f.IsT("pong") && f.GetProperty("n").GetInt32() == 6);
        Assert.False(g.PeerClosed);
        Assert.Empty(g.Received);
    }

    // ---------------- endings ----------------

    [Fact]
    public async Task When_the_game_closes_the_page_gets_game_closed_after_the_last_text_and_a_normal_close()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        await g.SendTextAsync("Goodbye.\r\n");
        g.CloseFromGame();
        var (frames, code) = await p.UntilClosedAsync();
        Assert.Equal(1000, code);
        Assert.Equal(["hello", "out", "sys"], frames.Select(f => f.S("t")));
        Assert.Equal("Goodbye.\n", frames[1].S("d"));
        Assert.Equal("game_closed", frames[2].S("code"));
    }

    [Fact]
    public async Task When_the_page_closes_the_game_connection_is_closed_at_once()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        await p.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        Assert.True(await WaitUntil(() => g.PeerClosed));
        Assert.True(await WaitUntil(() => site.Services.GetRequiredService<LesMudOS.Web.Play.PlayRuntime>().Gate.Active == 0));
        p.Dispose();
    }

    [Fact]
    public async Task When_the_game_is_not_there_the_page_is_told_unavailable_and_closed_with_1013()
    {
        var dead = new FakeGame(); var port = dead.Port; dead.Dispose();
        using var site = new Site(null, new() { ["Site:Play:Enabled"] = "true", ["Site:Play:GameBridgePort"] = port.ToString() });
        var p = await Player.Connect(site);
        using var _ = p;
        var (frames, code) = await p.UntilClosedAsync();
        Assert.Equal("unavailable", frames[^1].S("code"));
        Assert.Equal(1013, code);
        Assert.True(await WaitUntil(() => site.Services.GetRequiredService<LesMudOS.Web.Play.PlayRuntime>().Gate.Active == 0));
    }

    [Fact]
    public async Task A_stopping_website_tells_every_page_restart_and_closes_with_1012()
    {
        using var game = new FakeGame(); using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" });
        var (p, g) = await Join(site, game);
        using var _ = p;
        site.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        var (frames, code) = await p.UntilClosedAsync();
        Assert.Equal("restart", frames[^1].S("code"));
        Assert.Equal(1012, code);
        Assert.True(await WaitUntil(() => g.PeerClosed));
    }

    // ---------------- logging ----------------

    [Fact]
    public async Task The_log_has_one_open_and_one_close_line_with_the_true_address_and_nothing_a_player_typed_or_the_game_said()
    {
        const string secret = "hunter2-secret-marker";
        using var game = new FakeGame(); var logs = new CaptureLogs();
        using var site = Open(game, new() { ["Site:Play:PlainMode"] = "true" }, logs);
        var (p, g) = await Join(site, game, "203.0.113.7");
        await g.SendAsync(Bytes("Password: ", IAC, WILL, 1));
        await p.UntilAsync(f => f.IsT("mask"));
        await p.In(secret);                          // typed as a masked password
        await p.In("say " + secret);                 // and as a command
        await p.SendRawAsync(Encoding.UTF8.GetBytes("{\"t\":\"in\",\"d\":\"" + secret + "\\n\"}"));   // a refused frame
        await g.SendTextAsync("The game says " + secret + "\r\n");
        await p.UntilAsync(f => f.IsT("out") && f.S("d").Contains(secret));
        await p.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, secret, CancellationToken.None);
        Assert.True(await WaitUntil(() => logs.Lines.Any(l => l.Contains("play: close"))));
        var lines = logs.Lines.Where(l => l.StartsWith("Play:")).ToList();
        Assert.Single(lines, l => l.Contains("play: open 203.0.113.7 origin=https://example.test web=1/60"));
        Assert.Single(lines, l => l.Contains("play: close 203.0.113.7 lasted=") && l.Contains("lines_in=2") && l.Contains("reason=client-closed code=1000"));
        foreach (var l in logs.Lines) Assert.DoesNotContain(secret, l, StringComparison.Ordinal);
        p.Dispose();
    }

    [Fact]
    public async Task Refusals_are_logged_with_the_reason_and_a_cleaned_origin_but_no_secrets()
    {
        using var game = new FakeGame(); var logs = new CaptureLogs();
        using var site = Open(game, null, logs);
        await Player.TryConnect(site, origin: "https://evil.example");
        await Player.TryConnect(site, clientIp: null);
        Assert.Contains(logs.Lines, l => l.Contains("play: refused") && l.Contains("reason=origin") && l.Contains("origin=https://evil.example"));
        Assert.Contains(logs.Lines, l => l.Contains("play: refused") && l.Contains("reason=address-header"));
    }
}
