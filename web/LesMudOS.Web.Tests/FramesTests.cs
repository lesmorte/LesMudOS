using System.Text;
using System.Text.Json;
using LesMudOS.Web.Play;
using Xunit;

namespace LesMudOS.Web.Tests;

public class FramesTests
{
    private static string Json(byte[] b) => Encoding.UTF8.GetString(b);

    [Fact]
    public void Server_frames_have_the_documented_shapes()
    {
        Assert.Equal("{\"t\":\"hello\",\"v\":1,\"max_in\":4096,\"ping\":25}", Json(Frames.Hello(4096, 25)));
        Assert.Equal("{\"t\":\"mask\",\"on\":true}", Json(Frames.Mask(true)));
        Assert.Equal("{\"t\":\"pong\",\"n\":17}", Json(Frames.Pong(17)));
        Assert.Equal("{\"t\":\"sys\",\"code\":\"full\",\"msg\":\"x\"}", Json(Frames.Sys("full", "x")));
        Assert.Equal("{\"t\":\"out\",\"d\":\"a\\nb\"}", Json(Frames.Out("a\nb")));
    }

    [Fact]
    public void The_default_encoder_escapes_angle_brackets_and_the_escape_character()
    {
        var j = Json(Frames.Out("<script>\u001b[1m&'+"));
        Assert.DoesNotContain("<", j);
        Assert.DoesNotContain(">", j);
        Assert.False(j.Contains('\u001b')); // ordinal: a culture-aware search treats ESC as ignorable
        Assert.Contains("\\u003Cscript\\u003E\\u001B[1m\\u0026\\u0027\\u002B", j);
        Assert.Equal("<script>\u001b[1m&'+", JsonDocument.Parse(j).RootElement.GetProperty("d").GetString());
    }

    [Fact]
    public void Gmcp_frames_rewrite_the_json_with_the_safe_encoder()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"<b>x</b>\",\"n\":[1,2.5,null,true]}");
        var j = Json(Frames.Gmcp("Room.Info", doc.RootElement));
        Assert.DoesNotContain("<", j);
        Assert.Equal("<b>x</b>", JsonDocument.Parse(j).RootElement.GetProperty("d").GetProperty("name").GetString());
        Assert.Equal("{\"t\":\"gmcp\",\"p\":\"Core.Ping\"}", Json(Frames.Gmcp("Core.Ping", null)));
    }

    [Fact]
    public void Long_text_is_cut_into_frames_without_splitting_a_surrogate_pair()
    {
        var text = new string('a', Frames.OutChunkChars - 1) + "\U0001F600" + new string('b', 12000);
        var chunks = Frames.OutChunks(text).Select(b => JsonDocument.Parse(b).RootElement.GetProperty("d").GetString()!).ToList();
        Assert.True(chunks.Count >= 3);
        Assert.Equal(text, string.Concat(chunks));
        foreach (var c in chunks) { Assert.False(char.IsHighSurrogate(c[^1])); Assert.True(Encoding.UTF8.GetByteCount(c) <= Frames.MaxOutText); }
    }

    private static ClientFrame P(string json, int max = 4096) => Frames.ParseClient(Encoding.UTF8.GetBytes(json), max);

    [Fact]
    public void Client_frames_are_checked()
    {
        Assert.Equal(new InFrame("look"), P("{\"t\":\"in\",\"d\":\"look\"}"));
        Assert.Equal(new InFrame(""), P("{\"t\":\"in\",\"d\":\"\"}"));
        Assert.Equal(new PingFrame(17), P("{\"t\":\"ping\",\"n\":17}"));
        Assert.Equal(new SizeFrame(52, 30), P("{\"t\":\"size\",\"cols\":52,\"rows\":30}"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"t\":\"in\"}")]
    [InlineData("{\"t\":\"in\",\"d\":5}")]
    [InlineData("{\"t\":\"in\",\"d\":\"a\\nb\"}")]
    [InlineData("{\"t\":\"in\",\"d\":\"a\\rb\"}")]
    [InlineData("{\"t\":\"in\",\"d\":\"a\\u0000b\"}")]
    [InlineData("{\"t\":\"in\",\"d\":\"x\",\"extra\":1}")]
    [InlineData("{\"t\":\"gmcp\",\"p\":\"Core.Hello\"}")]
    [InlineData("{\"t\":\"ping\"}")]
    [InlineData("{\"t\":\"ping\",\"n\":-1}")]
    [InlineData("{\"t\":\"ping\",\"n\":1.5}")]
    [InlineData("{\"t\":\"ping\",\"n\":\"1\"}")]
    [InlineData("{\"t\":\"size\",\"cols\":0,\"rows\":1}")]
    [InlineData("{\"t\":\"size\",\"cols\":5}")]
    [InlineData("{\"t\":[1],\"d\":\"x\"}")]
    [InlineData("{\"t\":\"in\",\"d\":{\"a\":{\"b\":{\"c\":{\"d\":{}}}}}}")]
    public void Anything_else_is_a_bad_frame(string json)
        => Assert.Equal(new BadFrame("bad_frame"), P(json));

    [Fact]
    public void A_line_over_the_byte_limit_is_too_long_counted_in_bytes_not_characters()
    {
        Assert.Equal(new InFrame(new string('a', 4096)), P("{\"t\":\"in\",\"d\":\"" + new string('a', 4096) + "\"}"));
        Assert.Equal(new BadFrame("too_long"), P("{\"t\":\"in\",\"d\":\"" + new string('a', 4097) + "\"}"));
        Assert.Equal(new BadFrame("too_long"), P("{\"t\":\"in\",\"d\":\"" + new string('é', 2049) + "\"}")); // 4098 bytes
        Assert.IsType<InFrame>(P("{\"t\":\"in\",\"d\":\"" + new string('é', 2048) + "\"}"));
    }

    [Fact]
    public void Invalid_utf8_in_a_frame_is_a_bad_frame_not_an_exception()
        => Assert.Equal(new BadFrame("bad_frame"), Frames.ParseClient(new byte[] { 0x7b, 0x22, 0x74, 0x22, 0x3a, 0x22, 0xC3, 0x22, 0x7d }, 4096));
}
