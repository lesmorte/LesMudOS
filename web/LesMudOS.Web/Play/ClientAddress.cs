using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace LesMudOS.Web.Play;

/// <summary>The true address of a web player. Caddy writes it into X-Hollowmere-Client-Ip (replacing whatever the client sent under that
/// name); the bridge takes it only when the TCP peer is loopback and only if it is exactly one plain IP address.</summary>
public static partial class ClientAddress
{
    public const string HeaderName = "X-Hollowmere-Client-Ip";

    [GeneratedRegex(@"^(0|[1-9][0-9]{0,2})(\.(0|[1-9][0-9]{0,2})){3}$")]
    private static partial Regex Dotted();

    /// <summary>Exactly one valid IPv4 (dotted quad, no leading zeros) or IPv6 address (no port, brackets, zone, list or spaces). An
    /// IPv4-mapped IPv6 address is returned as IPv4. Null when anything is wrong.</summary>
    public static IPAddress? Parse(string? text)
    {
        if (string.IsNullOrEmpty(text) || text.Length > 45) return null;
        foreach (var c in text) if (!(char.IsAsciiHexDigit(c) || c == '.' || c == ':')) return null;
        IPAddress? ip;
        if (text.Contains(':'))
        {
            if (!IPAddress.TryParse(text, out ip) || ip.AddressFamily != AddressFamily.InterNetworkV6) return null;
        }
        else
        {
            if (!Dotted().IsMatch(text) || !IPAddress.TryParse(text, out ip) || ip.AddressFamily != AddressFamily.InterNetwork) return null;
        }
        return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
    }

    /// <summary>The address from the request: null when the header is missing, repeated, malformed or the TCP peer is not loopback.</summary>
    public static IPAddress? FromRequest(HttpContext ctx)
    {
        var peer = ctx.Connection.RemoteIpAddress;
        if (peer == null || !IPAddress.IsLoopback(peer)) return null;
        var values = ctx.Request.Headers[HeaderName];
        if (values.Count != 1) return null;
        return Parse(values[0]);
    }

    /// <summary>The key limits are counted on: IPv4 whole, IPv6 by its /64.</summary>
    public static string Key(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetworkV6) return ip.ToString();
        var b = ip.GetAddressBytes();
        return new IPAddress(b.Take(8).Concat(new byte[8]).ToArray()).ToString() + "/64";
    }
}
