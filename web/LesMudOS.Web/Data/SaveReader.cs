using System.Text.Json;

namespace LesMudOS.Web.Data;

/// <summary>Raw Renown ledger as the game saves it (mudlib/secure/renown.cs).</summary>
public sealed record RenownFile(
    IReadOnlyDictionary<string, int> Renown,
    IReadOnlyDictionary<string, List<string>> Titles,
    IReadOnlyDictionary<string, string> ActiveTitle);

/// <summary>What the site needs of one character: from roster.json, or from the first few fields of players/NAME.json.</summary>
public sealed record Character(string Name, int Level, string Calling, string AdvancedClass);

public sealed record ChronicleFile(long EpochDay, IReadOnlyList<IReadOnlyDictionary<string, string>> Entries);

/// <summary>
/// Reads the game's save files, read only. Every method returns null when the file is missing, empty, too large, not JSON or
/// cut short (a half-written file); none of them throws. Files are opened with shared access so that the game can write at the
/// same time (the game itself writes to a temporary file and renames it, so a half-written file is rare).
/// </summary>
public static class SaveReader
{
    private const long MaxBytes = 128L * 1024 * 1024;

    /// <summary>"secure/NAME.json" under the directory (the game's own layout), else "NAME.json" (the sanitized copy).</summary>
    public static string? Locate(string dir, string name)
    {
        foreach (var candidate in new[] { Path.Combine(dir, "secure", name + ".json"), Path.Combine(dir, name + ".json") })
            if (File.Exists(candidate)) return candidate;
        return null;
    }

    private static JsonDocument? Open(string? path)
    {
        try
        {
            if (path == null) return null;
            var info = new FileInfo(path);
            if (!info.Exists || info.Length == 0 || info.Length > MaxBytes) return null;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var doc = JsonDocument.Parse(stream, new JsonDocumentOptions { MaxDepth = 32 });
            if (doc.RootElement.ValueKind != JsonValueKind.Object) { doc.Dispose(); return null; }
            return doc;
        }
        catch (Exception) { return null; }
    }

    private static string AsText(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString() ?? "",
        JsonValueKind.Number => e.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => ""
    };

    public static ChronicleFile? ReadChronicle(string dir)
    {
        using var doc = Open(Locate(dir, "chronicle"));
        if (doc == null) return null;
        try
        {
            long epoch = 0;
            if (doc.RootElement.TryGetProperty("epochDay", out var e) && e.ValueKind == JsonValueKind.Number) e.TryGetInt64(out epoch);
            var entries = new List<IReadOnlyDictionary<string, string>>();
            if (doc.RootElement.TryGetProperty("entries", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var d = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var p in item.EnumerateObject()) d[p.Name] = AsText(p.Value);
                    entries.Add(d);
                }
            }
            return new ChronicleFile(epoch, entries);
        }
        catch (Exception) { return null; }
    }

    public static RenownFile? ReadRenown(string dir)
    {
        using var doc = Open(Locate(dir, "renown"));
        if (doc == null) return null;
        try
        {
            var renown = new Dictionary<string, int>();
            if (doc.RootElement.TryGetProperty("renown", out var r) && r.ValueKind == JsonValueKind.Object)
                foreach (var p in r.EnumerateObject()) if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var n)) renown[p.Name.ToLowerInvariant()] = n;
            var titles = new Dictionary<string, List<string>>();
            if (doc.RootElement.TryGetProperty("titles", out var t) && t.ValueKind == JsonValueKind.Object)
                foreach (var p in t.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.Array)
                        titles[p.Name.ToLowerInvariant()] = p.Value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList();
            var active = new Dictionary<string, string>();
            if (doc.RootElement.TryGetProperty("activeTitle", out var a) && a.ValueKind == JsonValueKind.Object)
                foreach (var p in a.EnumerateObject()) if (p.Value.ValueKind == JsonValueKind.String) active[p.Name.ToLowerInvariant()] = p.Value.GetString()!;
            return new RenownFile(renown, titles, active);
        }
        catch (Exception) { return null; }
    }

    /// <summary>Lower-cased names of every wizard. Level 0 players are not in the file. An empty set if there is no file; null if the file exists but cannot be read.</summary>
    public static HashSet<string>? ReadWizards(string dir)
    {
        var path = Locate(dir, "wizlist");
        if (path == null) return new HashSet<string>(); // the game writes the file at the first promotion: no file, no wizards
        using var doc = Open(path);
        if (doc == null) return null;
        try
        {
            var set = new HashSet<string>();
            if (doc.RootElement.TryGetProperty("levels", out var l) && l.ValueKind == JsonValueKind.Object)
                foreach (var p in l.EnumerateObject()) if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var n) && n > 0) set.Add(p.Name.ToLowerInvariant());
            return set;
        }
        catch (Exception) { return null; }
    }

    /// <summary>
    /// The characters. Preferred: roster.json, which the deployment's sync job writes without password hashes:
    /// {"characters": {"mira": {"level": 14, "calling": "warrior", "class": "knight"}}}.
    /// Otherwise (a development machine pointed at a game's own save directory) players/NAME.json is read for exactly four
    /// fields: archetype, advancedClass, Level and the name from the file name. Nothing else in those files is looked at or kept.
    /// Null when neither source exists.
    /// </summary>
    public static Dictionary<string, Character>? ReadCharacters(string dir)
    {
        try
        {
            using var doc = Open(Path.Combine(dir, "roster.json"));
            if (doc != null)
            {
                var result = new Dictionary<string, Character>();
                if (doc.RootElement.TryGetProperty("characters", out var c) && c.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in c.EnumerateObject())
                    {
                        if (p.Value.ValueKind != JsonValueKind.Object) continue;
                        var level = p.Value.TryGetProperty("level", out var lv) && lv.ValueKind == JsonValueKind.Number && lv.TryGetInt32(out var n) ? n : 1;
                        string S(string k) => p.Value.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
                        result[p.Name.ToLowerInvariant()] = new Character(p.Name.ToLowerInvariant(), level, S("calling"), S("class"));
                    }
                }
                return result;
            }
            var players = Path.Combine(dir, "players");
            if (!Directory.Exists(players)) return null;
            var found = new Dictionary<string, Character>();
            foreach (var file in Directory.EnumerateFiles(players, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                using var pd = Open(file);
                if (pd == null) { found[name] = new Character(name, 1, "", ""); continue; }
                string S(string k) => pd.RootElement.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
                var level = pd.RootElement.TryGetProperty("Level", out var lv) && lv.ValueKind == JsonValueKind.Number && lv.TryGetInt32(out var n) ? n : 1;
                found[name] = new Character(name, level, S("archetype"), S("advancedClass"));
            }
            return found;
        }
        catch (Exception) { return null; }
    }
}
