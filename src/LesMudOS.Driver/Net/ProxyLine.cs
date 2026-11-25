using System.Net;
using System.Net.Sockets;

namespace LesMudOS.Driver.Net;

/// <summary>
/// The PROXY protocol version 1 line that the website's bridge writes first on the loopback listener (docs/specs/web-client.md 2.6, 4.1):
/// "PROXY TCP4 <source> <destination> <source port> <destination port>\r\n" or the same with TCP6. Parsed strictly: exactly this shape and
/// nothing else, 107 bytes at most, ASCII only, single spaces, a real address of the named family on both sides, ports 0 to 65535 as plain
/// digits. "UNKNOWN", a lower-case word, a bare LF, an address with a zone, an abbreviated IPv4 ("1.2"), leading zeros and an unspecified,
/// multicast or broadcast source are all refused. Pure: no I/O, so it is tested through the listener and cannot swallow game bytes.
/// </summary>
internal static class ProxyLine
{
    public const int MaxBytes = 107;
    private static readonly byte[] Prefix = "PROXY "u8.ToArray();

    /// <summary>True if a first byte at this position can still start a valid line (lets the reader refuse early instead of waiting).</summary>
    public static bool PrefixOk(int index, byte b)
    {
        if (index < Prefix.Length) return b == Prefix[index];
        return b is >= 0x20 and <= 0x7e or (byte)'\r' or (byte)'\n';
    }

    /// <summary>Parse a whole line including its CR LF. On success the source address (IPv4-mapped IPv6 unmapped).</summary>
    public static bool TryParse(ReadOnlySpan<byte> line, out IPAddress address, out string reason)
    {
        address = IPAddress.None;
        if (line.Length > MaxBytes) { reason = "too long"; return false; }
        if (line.Length < 2 || line[^2] != '\r' || line[^1] != '\n') { reason = "no CRLF"; return false; }
        var body = line[..^2];
        foreach (var b in body)
            if (b < 0x20 || b > 0x7e) { reason = "bad character"; return false; }
        var text = System.Text.Encoding.ASCII.GetString(body);
        var parts = text.Split(' ');
        if (parts.Length != 6 || parts[0] != "PROXY") { reason = "not a PROXY line"; return false; }
        bool v4;
        if (parts[1] == "TCP4") v4 = true;
        else if (parts[1] == "TCP6") v4 = false;
        else { reason = "unsupported protocol"; return false; }
        if (!TryAddress(parts[2], v4, out var source) || !TryAddress(parts[3], v4, out _)) { reason = "bad address"; return false; }
        if (!TryPort(parts[4]) || !TryPort(parts[5])) { reason = "bad port"; return false; }
        if (source.IsIPv4MappedToIPv6) source = source.MapToIPv4();
        if (IsUnusable(source)) { reason = "unusable source address"; return false; }
        address = source;
        reason = "";
        return true;
    }

    private static bool TryPort(string s)
    {
        if (s.Length is < 1 or > 5) return false;
        foreach (var c in s) if (c is < '0' or > '9') return false;
        if (s.Length > 1 && s[0] == '0') return false; // no leading zeros
        return int.Parse(s) <= 65535;
    }

    private static bool TryAddress(string s, bool v4, out IPAddress address)
    {
        address = IPAddress.None;
        if (s.Length == 0 || s.Length > 45) return false;
        if (v4)
        {
            var octets = s.Split('.');
            if (octets.Length != 4) return false;
            var bytes = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                var o = octets[i];
                if (o.Length is < 1 or > 3 || (o.Length > 1 && o[0] == '0')) return false;
                foreach (var c in o) if (c is < '0' or > '9') return false;
                var n = int.Parse(o);
                if (n > 255) return false;
                bytes[i] = (byte)n;
            }
            address = new IPAddress(bytes);
            return true;
        }
        var colons = 0;
        foreach (var c in s)
        {
            if (c == ':') colons++;
            else if (!(c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F' or '.')) return false; // no zone ("%"), no brackets
        }
        if (colons < 2 || !IPAddress.TryParse(s, out var parsed) || parsed.AddressFamily != AddressFamily.InterNetworkV6 || parsed.ScopeId != 0) return false;
        address = parsed;
        return true;
    }

    private static bool IsUnusable(IPAddress a)
    {
        if (a.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = a.GetAddressBytes();
            return b[0] == 0 || b[0] >= 224; // 0.x.x.x, multicast, reserved and the broadcast address
        }
        return a.Equals(IPAddress.IPv6Any) || a.IsIPv6Multicast;
    }
}
