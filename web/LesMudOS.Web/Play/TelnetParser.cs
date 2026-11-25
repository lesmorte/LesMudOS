using System.Text;
using System.Text.Json;

namespace LesMudOS.Web.Play;

/// <summary>What the telnet parser found, in the order it was found.</summary>
public abstract record TelnetEvent;
/// <summary>Game text: UTF-8 decoded, "\n" line ends, only \n, tab and ESC left of the control characters.</summary>
public sealed record TextEvent(string Text) : TelnetEvent;
/// <summary>An incoming GMCP message whose payload (if any) is well formed JSON of at most 16 KiB and depth 8.</summary>
public sealed record GmcpEvent(string Package, JsonElement? Data) : TelnetEvent;
/// <summary>IAC WILL ECHO (true) or IAC WONT ECHO (false): the next typed line is secret, or no longer is.</summary>
public sealed record MaskEvent(bool On) : TelnetEvent;
/// <summary>Every WILL, WONT, DO and DONT, for the session to answer. The parser never writes anything itself.</summary>
public sealed record NegotiationEvent(byte Command, byte Option) : TelnetEvent;

/// <summary>A telnet byte stream turned into events. Pure: no I/O, so it is tested byte by byte. A split anywhere (even one byte at a
/// time) gives the same events once adjacent TextEvents are joined. docs/specs/web-client.md section 2.8.</summary>
public sealed class TelnetParser
{
    public const byte Iac = 255, Se = 240, Sb = 250, Will = 251, Wont = 252, Do = 253, Dont = 254;
    public const byte Echo = 1, Gmcp = 201;
    /// <summary>Longest subnegotiation body kept (a little over the driver's 16 KiB payload limit); a longer one is dropped whole.</summary>
    public const int MaxSubnegotiation = 17 * 1024;
    public const int MaxGmcpPayload = 16 * 1024;
    public const int MaxGmcpDepth = 8;

    private enum State { Data, Iac, Option, Sub, SubIac }

    private State _state = State.Data;
    private byte _command;
    private readonly List<byte> _sub = new();
    private bool _subOverflow;
    private readonly List<byte> _run = new();
    private readonly Decoder _decoder = new UTF8Encoding(false, false).GetDecoder();

    public void Feed(ReadOnlySpan<byte> data, List<TelnetEvent> events)
    {
        foreach (var b in data)
        {
            switch (_state)
            {
                case State.Data:
                    if (b == Iac) { FlushText(events); _state = State.Iac; } else _run.Add(b);
                    break;
                case State.Iac:
                    if (b == Iac) { _run.Add(255); _state = State.Data; } // a literal 0xFF byte: invalid UTF-8, becomes U+FFFD
                    else if (b is >= Will and <= Dont) { _command = b; _state = State.Option; }
                    else if (b == Sb) { _sub.Clear(); _subOverflow = false; _state = State.Sub; }
                    else _state = State.Data; // NOP, GA, SE outside a subnegotiation, anything else: ignored
                    break;
                case State.Option:
                    _state = State.Data;
                    if (_command == Will && b == Echo) events.Add(new MaskEvent(true));
                    else if (_command == Wont && b == Echo) events.Add(new MaskEvent(false));
                    events.Add(new NegotiationEvent(_command, b));
                    break;
                case State.Sub:
                    if (b == Iac) _state = State.SubIac;
                    else AddSub(b);
                    break;
                case State.SubIac:
                    if (b == Iac) { AddSub(255); _state = State.Sub; }
                    else if (b == Se) { EndSub(events); _state = State.Data; }
                    else _state = State.Sub; // like the driver: any other command inside a subnegotiation is skipped
                    break;
            }
        }
        FlushText(events);
    }

    private void AddSub(byte b)
    {
        if (_sub.Count < MaxSubnegotiation) _sub.Add(b); else _subOverflow = true;
    }

    private void EndSub(List<TelnetEvent> events)
    {
        var overflow = _subOverflow;
        var body = _sub.ToArray();
        _sub.Clear();
        _subOverflow = false;
        if (overflow || body.Length < 2 || body[0] != Gmcp) return;
        var rest = body.AsSpan(1);
        var space = rest.IndexOf((byte)' ');
        var nameBytes = space < 0 ? rest : rest[..space];
        var payload = space < 0 ? ReadOnlySpan<byte>.Empty : rest[(space + 1)..];
        if (nameBytes.Length == 0 || nameBytes.Length > 64) return;
        foreach (var c in nameBytes) if (c >= 128) return;
        var name = Encoding.ASCII.GetString(nameBytes);
        if (!ValidPackageName(name)) return;
        if (payload.IsEmpty) { events.Add(new GmcpEvent(name, null)); return; }
        if (payload.Length > MaxGmcpPayload || !System.Text.Unicode.Utf8.IsValid(payload)) return;
        try
        {
            using var doc = JsonDocument.Parse(payload.ToArray(), new JsonDocumentOptions { MaxDepth = MaxGmcpDepth });
            events.Add(new GmcpEvent(name, doc.RootElement.Clone()));
        }
        catch (JsonException) { /* not well formed: dropped */ }
    }

    public static bool ValidPackageName(string name)
    {
        if (name.Length == 0 || name.Length > 64 || !char.IsAsciiLetter(name[0]) || name[^1] == '.') return false;
        var previousDot = false;
        foreach (var c in name)
        {
            if (c == '.') { if (previousDot) return false; previousDot = true; continue; }
            previousDot = false;
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_')) return false;
        }
        return true;
    }

    private void FlushText(List<TelnetEvent> events)
    {
        if (_run.Count == 0) return;
        var bytes = _run.ToArray();
        _run.Clear();
        var chars = new char[_decoder.GetCharCount(bytes, 0, bytes.Length, false)];
        var n = _decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);
        var text = Clean(chars.AsSpan(0, n));
        if (text.Length > 0) events.Add(new TextEvent(text));
    }

    /// <summary>"\r" and every control character go except "\n", tab and ESC (so colour survives); the C1 controls (U+0080 to U+009F) go too.</summary>
    public static string Clean(ReadOnlySpan<char> text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c == '\n' || c == '\t' || c == '\u001b') sb.Append(c);
            else if (c < 0x20 || (c >= 0x7f && c <= 0x9f)) continue;
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
