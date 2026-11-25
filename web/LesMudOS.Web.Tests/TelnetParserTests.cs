using System.Text;
using System.Text.Json;
using LesMudOS.Web.Play;
using Xunit;
using static LesMudOS.Web.Tests.T;

namespace LesMudOS.Web.Tests;

public class TelnetParserTests
{
    /// <summary>Events for a stream fed in the given pieces, with adjacent text joined (where a read ends must not matter).</summary>
    private static List<string> Run(params byte[][] pieces)
    {
        var p = new TelnetParser();
        var events = new List<TelnetEvent>();
        foreach (var piece in pieces) p.Feed(piece, events);
        return Describe(events);
    }

    private static List<string> Describe(List<TelnetEvent> events)
    {
        var res = new List<string>();
        foreach (var e in events)
        {
            switch (e)
            {
                case TextEvent t when res.Count > 0 && res[^1].StartsWith("text:"): res[^1] += t.Text; break;
                case TextEvent t: res.Add("text:" + t.Text); break;
                case GmcpEvent g: res.Add($"gmcp:{g.Package}:{(g.Data is { } d ? d.GetRawText() : "")}"); break;
                case MaskEvent m: res.Add("mask:" + m.On); break;
                case NegotiationEvent n: res.Add($"neg:{n.Command}:{n.Option}"); break;
            }
        }
        return res;
    }

    [Fact]
    public void Plain_text_has_its_line_ends_turned_into_newlines()
        => Assert.Equal(["text:hello\nworld\n> "], Run(Bytes("hello\r\nworld\r\n> ")));

    [Fact]
    public void Control_characters_go_but_newline_tab_and_escape_stay()
        => Assert.Equal(["text:a\tb\u001b[1mc\nd"], Run(Bytes("a\tb\u001b[1m", 0, 7, 8, 127, "c\r\nd")));

    [Fact]
    public void C1_controls_are_removed()
        => Assert.Equal(["text:ab"], Run(Bytes("a\u0085\u009bb")));

    [Fact]
    public void Utf8_split_across_reads_is_joined()
    {
        var bytes = Encoding.UTF8.GetBytes("café ─ \U0001F600");
        for (var cut = 1; cut < bytes.Length; cut++)
            Assert.Equal(["text:café ─ \U0001F600"], Run(bytes[..cut], bytes[cut..]));
    }

    [Fact]
    public void Invalid_utf8_becomes_the_replacement_character()
        => Assert.Equal(["text:a�b"], Run(Bytes("a", 0xC3, "b")));

    [Fact]
    public void A_doubled_iac_is_one_invalid_byte_and_not_the_start_of_a_command()
        => Assert.Equal(["text:a�b"], Run(Bytes("a", IAC, IAC, "b")));

    [Fact]
    public void Negotiation_is_reported_and_never_shown()
        => Assert.Equal(["neg:251:201", "text:Hi"], Run(Bytes(IAC, WILL, 201, "Hi")));

    [Fact]
    public void Echo_offers_become_mask_events_in_order_with_the_text_before_them()
        => Assert.Equal(["text:Password: ", "mask:True", "neg:251:1", "mask:False", "neg:252:1", "text:\n"],
            Run(Bytes("Password: ", IAC, WILL, 1, IAC, WONT, 1, "\r\n")));

    [Fact]
    public void A_gmcp_message_becomes_an_event_with_its_json()
        => Assert.Equal(["gmcp:Room.Info:{\"num\":1,\"name\":\"Square\"}"], Run(Gmcp("Room.Info", "{\"num\":1,\"name\":\"Square\"}")));

    [Fact]
    public void A_gmcp_message_without_a_payload_has_none()
        => Assert.Equal(["gmcp:Core.Ping:"], Run(Gmcp("Core.Ping", "")));

    [Fact]
    public void A_doubled_255_inside_gmcp_is_one_byte()
    {
        var json = Encoding.UTF8.GetBytes("{\"a\":\"x\"}");
        var raw = Bytes(IAC, SB, 201, "T.x ", json, IAC, SE);
        Assert.Equal(["gmcp:T.x:{\"a\":\"x\"}"], Run(raw));
        // 0xFF in the payload (invalid UTF-8 and so not JSON): doubled on the wire, one byte in the message, dropped as not well formed
        Assert.Empty(Run(Bytes(IAC, SB, 201, "T.x ", "\"", IAC, IAC, "\"", IAC, SE)));
    }

    [Fact]
    public void Oversized_gmcp_is_dropped_whole_and_the_text_after_it_survives()
    {
        var big = new string('x', 20 * 1024);
        Assert.Equal(["text:after"], Run(Gmcp("Big.Msg", "\"" + big + "\""), Bytes("after")));
        var huge = new string('y', 200 * 1024);
        Assert.Equal(["text:after"], Run(Gmcp("Big.Msg", "\"" + huge + "\""), Bytes("after")));
    }

    [Fact]
    public void Gmcp_that_is_not_json_or_too_deep_or_badly_named_is_dropped()
    {
        Assert.Empty(Run(Gmcp("A.b", "{not json")));
        Assert.Empty(Run(Gmcp("A.b", new string('[', 20) + new string(']', 20))));
        Assert.Empty(Run(Gmcp("1bad", "{}")));
        Assert.Empty(Run(Gmcp("a..b", "{}")));
        Assert.Equal(["gmcp:A.b:" + new string('[', 8) + new string(']', 8)], Run(Gmcp("A.b", new string('[', 8) + new string(']', 8))));
    }

    [Fact]
    public void Other_subnegotiations_and_unknown_commands_are_ignored()
        => Assert.Equal(["text:ab"], Run(Bytes("a", IAC, SB, 24, 1, 2, 3, IAC, SE, IAC, 241, IAC, 249, "b")));

    [Fact]
    public void A_stream_cut_at_every_position_gives_the_same_events()
    {
        var stream = Bytes("\r\nWelcome é─ to the game.\r\nBy what name are you known? ",
            IAC, WILL, 201, IAC, WILL, 1, "Password: ", IAC, WONT, 1, "\r\n",
            "\u001b[1;96mnorth\u001b[0m, \U0001F600\r\n",
            Gmcp("Room.Info", "{\"num\":1,\"name\":\"Village Square\",\"exits\":{\"north\":2}}"),
            Gmcp("Char.Vitals", "{\"vitality\":45,\"fighting\":false}"),
            "Alice (45/58) > ", Gmcp("Big.One", "\"" + new string('z', 20000) + "\""), IAC, IAC, "tail");
        var whole = Run(stream);
        Assert.Contains("mask:True", whole);
        for (var cut = 1; cut < stream.Length; cut++)
            Assert.Equal(whole, Run(stream[..cut], stream[cut..]));
        var oneByOne = Run(stream.Select(b => new[] { b }).ToArray());
        Assert.Equal(whole, oneByOne);
    }

    [Fact]
    public void Random_chunks_of_random_bytes_never_throw_and_never_yield_stray_control_characters()
    {
        var rng = new Random(12345);
        for (var round = 0; round < 300; round++)
        {
            var p = new TelnetParser();
            var events = new List<TelnetEvent>();
            for (var chunk = 0; chunk < 20; chunk++)
            {
                var data = new byte[rng.Next(0, 300)];
                rng.NextBytes(data);
                if (rng.Next(3) == 0) for (var i = 0; i < data.Length; i += 7) data[i] = IAC; // plenty of commands
                p.Feed(data, events);
            }
            foreach (var t in events.OfType<TextEvent>())
                foreach (var c in t.Text)
                    Assert.False((c < 0x20 && c != '\n' && c != '\t' && c != 0x1b) || (c >= 0x7f && c <= 0x9f), $"control character U+{(int)c:X4}");
        }
    }
}
