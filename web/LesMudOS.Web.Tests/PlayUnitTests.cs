using System.Net;
using LesMudOS.Web.Play;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LesMudOS.Web.Tests;

public class OriginTests
{
    private static readonly string[] Allowed = ["https://legendsofhollowmere.com"];

    [Theory]
    [InlineData("https://legendsofhollowmere.com", true)]
    [InlineData("https://LegendsOfHollowmere.COM", true)]
    [InlineData("https://legendsofhollowmere.com:443", true)]
    [InlineData("http://legendsofhollowmere.com", false)]
    [InlineData("https://www.legendsofhollowmere.com", false)]
    [InlineData("https://legendsofhollowmere.com:8443", false)]
    [InlineData("https://legendsofhollowmere.com.evil.example", false)]
    [InlineData("https://evil.example", false)]
    [InlineData("https://legendsofhollowmere.com/", false)]
    [InlineData("https://legendsofhollowmere.com/x", false)]
    [InlineData("https://user@legendsofhollowmere.com", false)]
    [InlineData("null", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("file://", false)]
    public void Only_the_exact_origin_passes(string? origin, bool ok) => Assert.Equal(ok, OriginCheck.IsAllowed(origin, Allowed));

    [Fact]
    public void The_default_list_is_the_base_url_and_development_adds_the_local_address()
    {
        var o = new PlayOptions();
        Assert.Equal(["https://x.test"], OriginCheck.Effective(o, "https://x.test/", false));
        Assert.Equal(["https://x.test", "http://localhost:5090", "http://127.0.0.1:5090"], OriginCheck.Effective(o, "https://x.test", true));
        o.AllowedOrigins.Add("https://a.test");
        Assert.Equal(["https://a.test"], OriginCheck.Effective(o, "https://x.test", false));
    }

    [Fact]
    public void Logged_origins_are_cut_and_cleaned()
    {
        var s = OriginCheck.Safe("https://a\r\nb.test " + new string('x', 300));
        Assert.True(s.Length <= 100);
        Assert.DoesNotContain("\n", s);
        Assert.DoesNotContain(" ", s);
    }
}

public class ClientAddressTests
{
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("2001:db8::1", "2001:db8::1")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    [InlineData("::1", "::1")]
    public void One_plain_address_is_accepted(string text, string expect) => Assert.Equal(expect, ClientAddress.Parse(text)?.ToString());

    [Theory]
    [InlineData("")]
    [InlineData(" 203.0.113.7")]
    [InlineData("203.0.113.7 ")]
    [InlineData("203.0.113.7, 198.51.100.1")]
    [InlineData("203.0.113.7:443")]
    [InlineData("[2001:db8::1]")]
    [InlineData("[2001:db8::1]:443")]
    [InlineData("fe80::1%eth0")]
    [InlineData("203.0.113")]
    [InlineData("1")]
    [InlineData("203.0.113.256")]
    [InlineData("203.0.113.07")]
    [InlineData("0x7f.0.0.1")]
    [InlineData("localhost")]
    [InlineData("2001:db8::1::2")]
    [InlineData("203.0.113.7\r\nX: y")]
    public void Anything_else_is_refused(string text) => Assert.Null(ClientAddress.Parse(text));

    [Fact]
    public void Limits_count_ipv6_by_its_64()
    {
        Assert.Equal(ClientAddress.Key(IPAddress.Parse("2001:db8:1:2::1")), ClientAddress.Key(IPAddress.Parse("2001:db8:1:2:ffff::9")));
        Assert.NotEqual(ClientAddress.Key(IPAddress.Parse("2001:db8:1:2::1")), ClientAddress.Key(IPAddress.Parse("2001:db8:1:3::1")));
        Assert.Equal("203.0.113.7", ClientAddress.Key(IPAddress.Parse("203.0.113.7")));
    }

    [Fact]
    public void The_proxy_line_has_the_documented_form()
    {
        Assert.Equal("PROXY TCP4 203.0.113.7 127.0.0.1 0 4001\r\n", ProxyProtocol.Line(IPAddress.Parse("203.0.113.7"), 4001));
        Assert.Equal("PROXY TCP6 2001:db8::1 ::1 0 4001\r\n", ProxyProtocol.Line(IPAddress.Parse("2001:db8::1"), 4001));
        Assert.True(ProxyProtocol.Line(IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"), 65535).Length <= 107);
    }
}

public class GateTests
{
    private static ConnectionGate Gate(ManualTime t, Action<PlayOptions>? tweak = null)
    {
        var o = new PlayOptions { MaxConnections = 60, MaxPerAddress = 5, ConnectsPerMinute = 120, ConnectsPerMinutePerAddress = 10 };
        tweak?.Invoke(o);
        return new ConnectionGate(o, t);
    }

    [Fact]
    public void The_sixth_connection_from_one_address_is_refused_and_releasing_frees_a_slot()
    {
        var g = Gate(new ManualTime());
        for (var i = 0; i < 5; i++) Assert.Equal(GateResult.Ok, g.TryOpen("a"));
        Assert.Equal(GateResult.Address, g.TryOpen("a"));
        Assert.Equal(GateResult.Ok, g.TryOpen("b"));
        g.Release("a");
        Assert.Equal(GateResult.Ok, g.TryOpen("a"));
        Assert.Equal(6, g.Active);
    }

    [Fact]
    public void The_61st_player_gets_full_and_takes_no_slot()
    {
        var t = new ManualTime();
        var g = Gate(t, o => o.MaxPerAddress = 100);
        for (var i = 0; i < 60; i++) { Assert.Equal(GateResult.Ok, g.TryOpen("k" + i)); }
        Assert.True(g.IsFull);
        Assert.Equal(GateResult.Full, g.TryOpen("k99"));
        Assert.Equal(60, g.Active);
        g.Release("k0");
        Assert.False(g.IsFull);
    }

    [Fact]
    public void Connect_rates_count_refused_attempts_and_recover_after_a_minute()
    {
        var t = new ManualTime();
        var g = Gate(t, o => o.MaxPerAddress = 100);
        for (var i = 0; i < 10; i++) { Assert.Equal(GateResult.Ok, g.TryOpen("a")); g.Release("a"); }
        Assert.Equal(GateResult.Rate, g.TryOpen("a"));
        Assert.Equal(GateResult.Rate, g.TryOpen("a"));
        Assert.Equal(GateResult.Ok, g.TryOpen("b"));
        t.Advance(TimeSpan.FromSeconds(61));
        Assert.Equal(GateResult.Ok, g.TryOpen("a"));
    }

    [Fact]
    public void The_all_addresses_rate_is_a_separate_limit()
    {
        var g = Gate(new ManualTime(), o => { o.ConnectsPerMinute = 5; o.MaxPerAddress = 100; });
        for (var i = 0; i < 5; i++) { Assert.Equal(GateResult.Ok, g.TryOpen("k" + i)); g.Release("k" + i); }
        Assert.Equal(GateResult.Rate, g.TryOpen("z"));
    }

    [Fact]
    public void Past_ten_thousand_addresses_a_new_one_is_refused_rather_than_remembered()
    {
        var t = new ManualTime();
        var g = Gate(t, o => { o.ConnectsPerMinute = 100000; o.MaxConnections = 100000; });
        for (var i = 0; i < 10000; i++) Assert.Equal(GateResult.Ok, g.TryOpen("k" + i));
        Assert.Equal(GateResult.Rate, g.TryOpen("new"));
        for (var i = 0; i < 10000; i++) g.Release("k" + i);
        t.Advance(TimeSpan.FromSeconds(120));
        Assert.Equal(GateResult.Ok, g.TryOpen("new")); // pruned: empty entries go
    }

    [Fact]
    public void The_line_bucket_allows_a_burst_then_the_sustained_rate()
    {
        var t = new ManualTime();
        var b = new LineBucket(10, 20, t);
        var ok = 0;
        for (var i = 0; i < 50; i++) if (b.Take()) ok++;
        Assert.Equal(20, ok);
        t.Advance(TimeSpan.FromSeconds(1));
        ok = 0;
        for (var i = 0; i < 50; i++) if (b.Take()) ok++;
        Assert.Equal(10, ok);
    }
}

public class OptionsTests
{
    [Fact]
    public void The_defaults_are_the_documented_ones_and_the_client_is_off()
    {
        var o = new PlayOptions();
        Assert.False(o.Enabled);
        Assert.False(o.PlainMode);
        Assert.True(o.RequireClientIpHeader);
        Assert.Equal(("127.0.0.1", 4001), (o.GameBridgeHost, o.GameBridgePort));
        Assert.Equal((60, 5, 120, 10), (o.MaxConnections, o.MaxPerAddress, o.ConnectsPerMinute, o.ConnectsPerMinutePerAddress));
        Assert.Equal((10, 20, 4096, 8192, 262144), (o.InputLinesPerSecond, o.InputBurst, o.MaxInputBytes, o.MaxFrameBytes, o.MaxQueuedBytes));
        Assert.Equal((25, 75, 5), (o.PingSeconds, o.ClientSilenceSeconds, o.ShutdownGraceSeconds));
        Assert.Equal("/var/lib/lesmudos-web/play.off", o.KillSwitchFile);
        o.Validate();
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("localhost", true)]
    [InlineData("10.0.0.5", false)]
    [InlineData("game.example.com", false)]
    public void Only_a_loopback_bridge_host_is_accepted_unless_allowed(string host, bool ok)
    {
        var o = new PlayOptions { GameBridgeHost = host };
        if (ok) o.Validate(); else Assert.Throws<InvalidOperationException>(o.Validate);
        o.AllowRemoteBridge = true;
        o.Validate();
    }

    [Fact]
    public void Nonsense_limits_stop_the_site_from_starting()
    {
        Assert.Throws<InvalidOperationException>(new PlayOptions { MaxConnections = 0 }.Validate);
        Assert.Throws<InvalidOperationException>(new PlayOptions { MaxInputBytes = 5000 }.Validate);
        Assert.Throws<InvalidOperationException>(new PlayOptions { GameBridgePort = 0 }.Validate);
    }

    [Fact]
    public void Settings_bind_from_the_environment_style_keys()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Site:Play:Enabled"] = "true", ["Site:Play:MaxConnections"] = "7", ["Site:Play:AllowedOrigins:0"] = "https://a.test", ["Site:Play:PlainMode"] = "true",
        }).Build();
        var site = cfg.GetSection("Site").Get<SiteOptions>()!;
        Assert.True(site.Play.Enabled);
        Assert.True(site.Play.PlainMode);
        Assert.Equal(7, site.Play.MaxConnections);
        Assert.Equal(["https://a.test"], site.Play.AllowedOrigins);
    }
}
