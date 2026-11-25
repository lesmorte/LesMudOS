using System.Buffers;
using System.Text;
using System.Text.Json;

namespace LesMudOS.Web.Play;

/// <summary>What the browser sent, once checked.</summary>
public abstract record ClientFrame;
public sealed record InFrame(string Line) : ClientFrame;
public sealed record PingFrame(long N) : ClientFrame;
public sealed record SizeFrame(int Cols, int Rows) : ClientFrame;
/// <summary>Not valid JSON of the expected shape, or a line the game could not take (Code is the sys code to answer with).</summary>
public sealed record BadFrame(string Code) : ClientFrame;

/// <summary>The JSON frames of docs/specs/web-client.md section 2.5. Every frame is one small JSON object in a text frame. Encoding uses
/// System.Text.Json's DEFAULT encoder, which writes &lt; &gt; &amp; ' and + as \uXXXX (a second layer of defence), never the relaxed one.</summary>
public static class Frames
{
    public const int MaxOutText = 16 * 1024;
    public const int OutChunkChars = 5000; // 5000 UTF-16 units are at most 15,000 UTF-8 bytes

    private static byte[] Build(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var w = new Utf8JsonWriter(buffer)) write(w);
        return buffer.WrittenSpan.ToArray();
    }

    public static byte[] Hello(int maxIn, int pingSeconds) => Build(w =>
    {
        w.WriteStartObject(); w.WriteString("t", "hello"); w.WriteNumber("v", 1); w.WriteNumber("max_in", maxIn); w.WriteNumber("ping", pingSeconds); w.WriteEndObject();
    });

    public static byte[] Out(string text) => Build(w =>
    {
        w.WriteStartObject(); w.WriteString("t", "out"); w.WriteString("d", text); w.WriteEndObject();
    });

    /// <summary>Text cut into frames of at most OutChunkChars characters, never inside a surrogate pair.</summary>
    public static IEnumerable<byte[]> OutChunks(string text)
    {
        for (var i = 0; i < text.Length;)
        {
            var len = Math.Min(OutChunkChars, text.Length - i);
            if (i + len < text.Length && char.IsHighSurrogate(text[i + len - 1])) len--;
            yield return Out(text.Substring(i, len));
            i += len;
        }
    }

    public static byte[] Gmcp(string package, JsonElement? data) => Build(w =>
    {
        w.WriteStartObject(); w.WriteString("t", "gmcp"); w.WriteString("p", package);
        if (data is { } d) { w.WritePropertyName("d"); d.WriteTo(w); }
        w.WriteEndObject();
    });

    public static byte[] Mask(bool on) => Build(w =>
    {
        w.WriteStartObject(); w.WriteString("t", "mask"); w.WriteBoolean("on", on); w.WriteEndObject();
    });

    public static byte[] Pong(long n) => Build(w =>
    {
        w.WriteStartObject(); w.WriteString("t", "pong"); w.WriteNumber("n", n); w.WriteEndObject();
    });

    public static byte[] Sys(string code, string msg) => Build(w =>
    {
        w.WriteStartObject(); w.WriteString("t", "sys"); w.WriteString("code", code); w.WriteString("msg", msg); w.WriteEndObject();
    });

    /// <summary>The plain-English text that goes with each sys code.</summary>
    public static string Message(string code) => code switch
    {
        "full" => "The game is full right now. Please try again in a few minutes.",
        "unavailable" => "The game is not running right now. Trying again soon.",
        "disabled" => "The browser client is switched off.",
        "game_closed" => "The game closed your connection.",
        "restart" => "The website is restarting. Reconnecting.",
        "rate" => "You are typing too fast. That line was dropped.",
        "too_long" => "That line is too long. It was not sent.",
        "bad_frame" => "The page sent something the bridge could not understand.",
        "address" => "Too many connections from your address.",
        "slow" => "Your browser was not keeping up, so the connection was closed.",
        _ => code,
    };

    /// <summary>Checks one text frame from the browser: valid JSON, an object of exactly the expected shape, nothing else.</summary>
    public static ClientFrame ParseClient(ReadOnlySpan<byte> utf8, int maxInputBytes)
    {
        JsonDocument doc;
        if (!System.Text.Unicode.Utf8.IsValid(utf8)) return new BadFrame("bad_frame"); // the JSON reader would accept bad bytes inside a string and fail later
        try { doc = JsonDocument.Parse(utf8.ToArray(), new JsonDocumentOptions { MaxDepth = 4 }); }
        catch (JsonException) { return new BadFrame("bad_frame"); }
        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new BadFrame("bad_frame");
            string? t = null;
            JsonElement d = default, n = default, cols = default, rows = default;
            foreach (var p in root.EnumerateObject())
            {
                switch (p.Name)
                {
                    case "t": if (p.Value.ValueKind != JsonValueKind.String) return new BadFrame("bad_frame"); t = p.Value.GetString(); break;
                    case "d": d = p.Value; break;
                    case "n": n = p.Value; break;
                    case "cols": cols = p.Value; break;
                    case "rows": rows = p.Value; break;
                    default: return new BadFrame("bad_frame");
                }
            }
            switch (t)
            {
                case "in":
                    {
                        if (d.ValueKind != JsonValueKind.String || n.ValueKind != JsonValueKind.Undefined || cols.ValueKind != JsonValueKind.Undefined || rows.ValueKind != JsonValueKind.Undefined) return new BadFrame("bad_frame");
                        var line = d.GetString() ?? "";
                        foreach (var c in line) if (c is '\r' or '\n' or '\0') return new BadFrame("bad_frame");
                        if (Encoding.UTF8.GetByteCount(line) > maxInputBytes) return new BadFrame("too_long");
                        return new InFrame(line);
                    }
                case "ping":
                    if (n.ValueKind != JsonValueKind.Number || d.ValueKind != JsonValueKind.Undefined || !n.TryGetInt64(out var num) || num < 0 || num > 9007199254740991L) return new BadFrame("bad_frame");
                    return new PingFrame(num);
                case "size":
                    if (cols.ValueKind != JsonValueKind.Number || rows.ValueKind != JsonValueKind.Number || d.ValueKind != JsonValueKind.Undefined || n.ValueKind != JsonValueKind.Undefined) return new BadFrame("bad_frame");
                    if (!cols.TryGetInt32(out var c2) || !rows.TryGetInt32(out var r2) || c2 is < 1 or > 1000 || r2 is < 1 or > 1000) return new BadFrame("bad_frame");
                    return new SizeFrame(c2, r2);
                default:
                    return new BadFrame("bad_frame");
            }
        }
    }
}
