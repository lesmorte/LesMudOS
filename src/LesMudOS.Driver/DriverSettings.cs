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
///   stall    How long one step of the game loop may run with sandbox code on the call stack before the watchdog stops the driver
///            (the service restarts it) and closes that wizard's workshop (docs/SANDBOX.md, "A stall"). Default 5 seconds.
///            --sandbox-stall N, or LESMUDOS_SANDBOX_STALL (same forms as below; a bare number here is seconds).
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
    public double StallSeconds { get; private set; } = 5;

    // A wizard's home (/wiz/<name>), checked by the driver on every write, copy, move and mkdir into it (the second review's D2a).
    public long HomeBytes { get; private set; } = 3_000_000;
    public int HomeFiles { get; private set; } = 1000;
    public int HomeDirs { get; private set; } = 64;
    public int HomeDepth { get; private set; } = 8;

    /// <summary>LESMUDOS_HOME_BYTES, LESMUDOS_HOME_FILES, LESMUDOS_HOME_DIRS, LESMUDOS_HOME_DEPTH: whole positive numbers.</summary>
    public void SetHomeQuota(Func<string, string?> environment)
    {
        static long? Number(string? text) => long.TryParse(text, out var n) && n > 0 ? n : null;
        if (Number(environment("LESMUDOS_HOME_BYTES")) is { } bytes) HomeBytes = bytes;
        if (Number(environment("LESMUDOS_HOME_FILES")) is { } files) HomeFiles = (int)Math.Min(files, int.MaxValue);
        if (Number(environment("LESMUDOS_HOME_DIRS")) is { } dirs) HomeDirs = (int)Math.Min(dirs, int.MaxValue);
        if (Number(environment("LESMUDOS_HOME_DEPTH")) is { } depth) HomeDepth = (int)Math.Min(depth, 64);
    }

    public void SetStall(string? text, string source)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var t = text.Trim().ToLowerInvariant();
        if (!t.EndsWith('s') && !t.EndsWith('m')) t += "s";
        if (TryParse(t, source, out var seconds)) StallSeconds = Math.Max(1, seconds);
    }

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
