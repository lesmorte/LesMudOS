namespace LesMudOS.Driver;

/// <summary>
/// Server settings that are not the game's business but the operator's. Defaults first, then the command line, then the
/// environment (which wins, so a service file or a test can override a script's arguments).
///
///   idle     Time without typed input until a player is marked idle (shown in who, earns no experience). Default 5 minutes.
///            --idle-minutes N, or LESMUDOS_IDLE_MINUTES.
///   timeout  Time without typed input until a player is saved and disconnected. Default 10 minutes. Wizards are exempt (the
///            mudlib decides that). --timeout-minutes N, or LESMUDOS_TIMEOUT_MINUTES.
///
/// A value is minutes, possibly fractional ("0.5"), or a number with a unit: "30s" seconds, "2m" minutes. The seconds form is
/// for tests. Anything else is ignored with a note in the log.
/// </summary>
internal sealed class DriverSettings
{
    public const double DefaultIdleMinutes = 5;
    public const double DefaultTimeoutMinutes = 10;

    public double IdleSeconds { get; private set; } = DefaultIdleMinutes * 60;
    public double TimeoutSeconds { get; private set; } = DefaultTimeoutMinutes * 60;

    public void SetIdle(string? text, string source) { if (TryParse(text, source, out var seconds)) IdleSeconds = seconds; }

    public void SetTimeout(string? text, string source) { if (TryParse(text, source, out var seconds)) TimeoutSeconds = seconds; }

    /// <summary>Make the two settings agree: a timeout earlier than the idle mark would never show anyone idle first.</summary>
    public void Settle()
    {
        if (TimeoutSeconds < IdleSeconds)
        {
            Log.Info($"timeout ({TimeoutSeconds:0.##}s) is shorter than the idle time ({IdleSeconds:0.##}s): using the idle time for both");
            TimeoutSeconds = IdleSeconds;
        }
    }

    public static bool TryParse(string? text, string source, out double seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim().ToLowerInvariant();
        var factor = 60.0;
        if (t.EndsWith('s')) { factor = 1; t = t[..^1]; }
        else if (t.EndsWith('m')) t = t[..^1];
        if (double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) && value > 0 && value < 1e6)
        {
            seconds = value * factor;
            return true;
        }
        Log.Info($"ignoring {source}: '{text}' is not a positive time (minutes, or a number ending in s or m)");
        return false;
    }
}
