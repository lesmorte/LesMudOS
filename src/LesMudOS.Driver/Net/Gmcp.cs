using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LesMudOS.Api;

namespace LesMudOS.Driver.Net;

/// <summary>
/// GMCP (Generic MUD Communication Protocol, telnet option 201): framing, JSON encoding of plain values, and the few incoming
/// packages the driver understands. docs/specs/gmcp.md; the protocol is described in docs/DRIVER_API.md.
/// Nothing here touches world state: it is used by the network thread (incoming) and by the game thread (outgoing, through the
/// connection's one output queue, so GMCP and text keep their order).
/// </summary>
internal static class Gmcp
{
    public const byte Option = 201;
    public const byte Iac = 255, Se = 240, Sb = 250, Will = 251, Wont = 252, Do = 253, Dont = 254;

    /// <summary>The offer the server makes at connect: IAC WILL GMCP.</summary>
    public static readonly byte[] WillOffer = [Iac, Will, Option];

    /// <summary>Longest incoming message body the driver keeps (option byte excluded). A longer one is dropped whole.</summary>
    public const int MaxIncomingBytes = 4096;

    /// <summary>Longest outgoing package name and encoded payload.</summary>
    public const int MaxPackageName = 64;
    public const int MaxPayloadBytes = 16 * 1024;
    private const int MaxDepth = 8;

    /// <summary>A package name: dotted words of letters, digits and underscores, starting with a letter.</summary>
    public static bool ValidPackageName(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > MaxPackageName || !char.IsAsciiLetter(name[0])) return false;
        var previousDot = false;
        foreach (var c in name)
        {
            if (c == '.') { if (previousDot) return false; previousDot = true; continue; }
            previousDot = false;
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_')) return false;
        }
        return name[^1] != '.';
    }

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        // Escapes quotes, backslash and every control character; leaves ordinary text readable. Never indented.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
        SkipValidation = false,
    };

    /// <summary>
    /// JSON for a plain value: null, string, bool, a number, a list or array of values, or a map with string keys. Anything else
    /// is refused, so game code can only ever hand the driver data, never JSON text to be trusted.
    /// </summary>
    public static byte[] EncodeJson(object? value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
        {
            WriteValue(writer, value, 0);
        }
        if (stream.Length > MaxPayloadBytes) throw new MudRuntimeException($"GMCP payload too large ({stream.Length} bytes; the limit is {MaxPayloadBytes})");
        return stream.ToArray();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value, int depth)
    {
        if (depth > MaxDepth) throw new MudRuntimeException("GMCP value nested too deeply");
        switch (value)
        {
            case null: writer.WriteNullValue(); break;
            case string s: writer.WriteStringValue(Clean(s)); break;
            case bool b: writer.WriteBooleanValue(b); break;
            case int or long or short or sbyte or byte or ushort or uint: writer.WriteNumberValue(Convert.ToInt64(value)); break;
            case ulong u: writer.WriteNumberValue(u); break;
            case float f: WriteDouble(writer, f); break;
            case double d: WriteDouble(writer, d); break;
            case decimal m: writer.WriteNumberValue(m); break;
            case IDictionary map:
                writer.WriteStartObject();
                foreach (DictionaryEntry entry in map)
                {
                    if (entry.Key is not string key) throw new MudRuntimeException("GMCP maps need string keys");
                    writer.WritePropertyName(Clean(key));
                    WriteValue(writer, entry.Value, depth + 1);
                }
                writer.WriteEndObject();
                break;
            case IEnumerable list:
                writer.WriteStartArray();
                foreach (var item in list) WriteValue(writer, item, depth + 1);
                writer.WriteEndArray();
                break;
            default:
                throw new MudRuntimeException($"GMCP cannot encode a value of type {value.GetType().Name}");
        }
    }

    private static void WriteDouble(Utf8JsonWriter writer, double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) throw new MudRuntimeException("GMCP numbers must be finite");
        writer.WriteNumberValue(d);
    }

    /// <summary>A string made valid UTF-16: a lone surrogate becomes U+FFFD, so one bad character cannot lose the package.</summary>
    private static string Clean(string text)
    {
        StringBuilder? fixedText = null;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var bad = false;
            if (char.IsHighSurrogate(c)) { if (i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { fixedText?.Append(c).Append(text[i + 1]); i++; continue; } bad = true; }
            else if (char.IsLowSurrogate(c)) bad = true;
            if (bad) { fixedText ??= new StringBuilder(text, 0, i, text.Length); fixedText.Append('�'); }
            else fixedText?.Append(c);
        }
        return fixedText?.ToString() ?? text;
    }

    /// <summary>The bytes of one outgoing message: IAC SB GMCP name SP json IAC SE, with every 255 in the body doubled.</summary>
    public static byte[] Frame(string package, byte[] json)
    {
        var body = new List<byte>(package.Length + json.Length + 8) { Iac, Sb, Option };
        foreach (var b in Encoding.ASCII.GetBytes(package)) body.Add(b);
        if (json.Length > 0)
        {
            body.Add((byte)' ');
            foreach (var b in json) { body.Add(b); if (b == Iac) body.Add(Iac); }
        }
        body.Add(Iac);
        body.Add(Se);
        return body.ToArray();
    }
}

/// <summary>
/// The GMCP state of one connection. Written by the network thread (the client's answers and messages) and read by the game thread
/// (the efuns), so every field is a volatile flag or an immutable snapshot that is replaced whole.
/// </summary>
internal sealed class GmcpState
{
    private const int BucketSize = 30;
    private const double RefillPerSecond = 15;
    private const int MaxSupportEntries = 64;
    private const int MaxSupportLength = 64;

    private volatile bool _enabled;
    private volatile HashSet<string>? _supports; // null: the client never said, so everything is sent
    private double _tokens = BucketSize;
    private long _lastRefill = Stopwatch.GetTimestamp();
    private bool _loggedDrop;

    /// <summary>The client answered DO to the server's offer (and has not said DONT since).</summary>
    public bool Enabled => _enabled;

    public void SetEnabled(bool on)
    {
        _enabled = on;
        if (!on) _supports = null;
    }

    /// <summary>May a package of this name go to the client? Everything, until the client says what it wants.</summary>
    public bool Wants(string package)
    {
        var supports = _supports;
        if (supports == null) return true;
        var name = package.ToLowerInvariant();
        foreach (var module in supports)
            if (name == module || name.StartsWith(module + ".", StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>Rate limit for incoming messages: a token bucket. False means drop the message.</summary>
    private bool TakeToken()
    {
        var now = Stopwatch.GetTimestamp();
        _tokens = Math.Min(BucketSize, _tokens + (now - _lastRefill) / (double)Stopwatch.Frequency * RefillPerSecond);
        _lastRefill = now;
        if (_tokens < 1) return false;
        _tokens -= 1;
        return true;
    }

    /// <summary>
    /// One complete incoming subnegotiation body for option 201 (the option byte is already removed). Runs on the network thread.
    /// Understood: Core.Hello, Core.Supports.Set / Add / Remove, Core.Ping. Everything else is ignored. Nothing in a message is
    /// ever run, passed to the mudlib, or sent to another player; nothing is logged but the fact of one dropped message.
    /// Returns the reply to send, if any (the answer to Core.Ping).
    /// </summary>
    public byte[]? OnMessage(byte[] body, bool overflow, Action<string> log)
    {
        if (!_enabled) return null; // a client that never said DO is not speaking GMCP
        if (!TakeToken() || overflow || body.Length == 0)
        {
            if (!_loggedDrop && (overflow || body.Length > 0)) { _loggedDrop = true; log(overflow ? "gmcp: oversized message dropped" : "gmcp: message rate limit hit"); }
            return null;
        }
        var text = Encoding.UTF8.GetString(body);
        var space = text.IndexOf(' ');
        var package = space < 0 ? text : text[..space];
        var json = space < 0 ? "" : text[(space + 1)..];
        if (!Gmcp.ValidPackageName(package)) return null;
        switch (package.ToLowerInvariant())
        {
            case "core.ping":
                return Gmcp.Frame("Core.Ping", []);
            case "core.hello":
                // Parsed to check that it is well-formed; the driver has no use for the client's name.
                TryParse(json);
                return null;
            case "core.supports.set":
                if (ReadModules(json) is { } set) _supports = set;
                return null;
            case "core.supports.add":
                if (ReadModules(json) is { } add)
                {
                    var merged = new HashSet<string>(_supports ?? [], StringComparer.Ordinal);
                    merged.UnionWith(add);
                    if (merged.Count <= MaxSupportEntries) _supports = merged;
                }
                return null;
            case "core.supports.remove":
                if (ReadModules(json) is { } remove && _supports is { } current)
                {
                    var rest = new HashSet<string>(current, StringComparer.Ordinal);
                    rest.ExceptWith(remove);
                    _supports = rest;
                }
                return null;
            default:
                return null;
        }
    }

    private static JsonDocument? TryParse(string json)
    {
        try { return JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4, AllowTrailingCommas = false }); }
        catch (JsonException) { return null; }
    }

    /// <summary>A list of "Module 1" strings as module names in lower case, or null if the message is not one (too long, wrong shape).</summary>
    private static HashSet<string>? ReadModules(string json)
    {
        using var document = TryParse(json);
        if (document == null || document.RootElement.ValueKind != JsonValueKind.Array) return null;
        if (document.RootElement.GetArrayLength() > MaxSupportEntries) return null;
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) continue;
            var entry = item.GetString() ?? "";
            if (entry.Length > MaxSupportLength) continue;
            var module = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            if (Gmcp.ValidPackageName(module)) result.Add(module.ToLowerInvariant());
        }
        return result;
    }
}
