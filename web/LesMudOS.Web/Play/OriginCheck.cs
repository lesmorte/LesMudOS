namespace LesMudOS.Web.Play;

/// <summary>The Origin header of a WebSocket request, compared exactly (scheme, host, port; host case-insensitive) with the allowed list.</summary>
public static class OriginCheck
{
    /// <summary>scheme://host[:port] in canonical lower case with the default port left out, or null when the text is not a plain origin.</summary>
    public static string? Canonical(string? origin)
    {
        if (string.IsNullOrEmpty(origin) || origin.Length > 200) return null;
        if (origin.EndsWith('/') || origin.Contains(' ')) return null;
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var u)) return null;
        if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps) return null;
        if (u.AbsolutePath != "/" || u.Query.Length > 0 || u.Fragment.Length > 0 || u.UserInfo.Length > 0) return null;
        var port = u.IsDefaultPort ? "" : ":" + u.Port;
        return $"{u.Scheme}://{u.IdnHost.ToLowerInvariant()}{port}";
    }

    public static bool IsAllowed(string? origin, IEnumerable<string> allowed)
    {
        var c = Canonical(origin);
        if (c == null) return false;
        foreach (var a in allowed)
            if (Canonical(a) is { } ca && ca == c) return true;
        return false;
    }

    /// <summary>The origins the bridge accepts: the configured list, else the origin of the site's own base URL; in Development also the local test address.</summary>
    public static List<string> Effective(PlayOptions play, string baseUrl, bool development)
    {
        var list = new List<string>();
        if (play.AllowedOrigins.Count > 0) list.AddRange(play.AllowedOrigins.Where(o => !string.IsNullOrWhiteSpace(o)));
        else if (Canonical(baseUrl.TrimEnd('/')) is { } b) list.Add(b);
        if (development) { list.Add("http://localhost:5090"); list.Add("http://127.0.0.1:5090"); }
        return list;
    }

    /// <summary>For a log line: control characters removed, at most 100 characters.</summary>
    public static string Safe(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "-";
        var chars = value.Take(100).Select(c => char.IsControl(c) || c == ' ' ? '?' : c).ToArray();
        return new string(chars);
    }
}
