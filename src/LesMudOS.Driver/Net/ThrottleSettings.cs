using System.Globalization;

namespace LesMudOS.Driver.Net;

/// <summary>
/// The settings of the login throttle (docs/specs/login-throttle.md), read once at boot from LESMUDOS_THROTTLE_* in the environment and given
/// to /secure/throttle, and to nothing else, through the ThrottleSetting efun. A name that is not in the table is refused; so the mudlib can
/// not read any other environment variable through it. Times are seconds (the tests shorten them); 0 switches a limit off.
/// </summary>
internal static class ThrottleSettings
{
    private static readonly Dictionary<string, (string Env, long Default, long Max)> Table = new()
    {
        ["NameFailures"] = ("LESMUDOS_THROTTLE_NAME_FAILURES", 5, 1000),
        ["NameWindow"] = ("LESMUDOS_THROTTLE_WINDOW_SECONDS", 900, 86400 * 7),
        ["NameLock"] = ("LESMUDOS_THROTTLE_NAME_LOCK_SECONDS", 300, 86400 * 7),
        ["NameLockMax"] = ("LESMUDOS_THROTTLE_NAME_LOCK_MAX_SECONDS", 3600, 86400 * 7),
        ["Quiet"] = ("LESMUDOS_THROTTLE_RESET_SECONDS", 86400, 86400 * 30),
        ["AddressFailures"] = ("LESMUDOS_THROTTLE_ADDRESS_FAILURES", 40, 100000),
        ["AddressLock"] = ("LESMUDOS_THROTTLE_ADDRESS_LOCK_SECONDS", 900, 86400 * 7),
        ["AddressLockMax"] = ("LESMUDOS_THROTTLE_ADDRESS_LOCK_MAX_SECONDS", 14400, 86400 * 7),
        ["KnownTries"] = ("LESMUDOS_THROTTLE_KNOWN_TRIES", 3, 100),
        ["FailDelayMs"] = ("LESMUDOS_THROTTLE_FAIL_DELAY_MS", 1000, 10000),
        ["NewPerHour"] = ("LESMUDOS_THROTTLE_NEW_PER_HOUR", 3, 10000),
        ["NewPerDay"] = ("LESMUDOS_THROTTLE_NEW_PER_DAY", 10, 10000),
        ["NewHourSeconds"] = ("LESMUDOS_THROTTLE_NEW_HOUR_SECONDS", 3600, 86400),
        ["NewDaySeconds"] = ("LESMUDOS_THROTTLE_NEW_DAY_SECONDS", 86400, 86400 * 7),
    };

    private static readonly Dictionary<string, long> Values = new();
    private static bool _loaded;

    /// <summary>Read the environment (once; a bad value is ignored with a note) and log what is in force when it differs from the defaults.</summary>
    public static void Load()
    {
        Values.Clear();
        var changed = new List<string>();
        foreach (var (name, (env, def, max)) in Table)
        {
            var value = def;
            var text = Environment.GetEnvironmentVariable(env);
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (long.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed <= max) value = parsed;
                else Log.Info($"ignoring {env}: '{(text.Length > 40 ? text[..40] : text)}' is not a whole number from 0 to {max}");
            }
            Values[name] = value;
            if (value != def) changed.Add($"{env}={value}");
        }
        _loaded = true;
        Log.Info(changed.Count == 0 ? "login throttle: default settings" : "login throttle: " + string.Join(", ", changed));
    }

    /// <summary>The value of a setting, or -1 for a name that is not one.</summary>
    public static long Get(string name)
    {
        if (!_loaded) Load();
        return Values.TryGetValue(name, out var v) ? v : -1;
    }
}
