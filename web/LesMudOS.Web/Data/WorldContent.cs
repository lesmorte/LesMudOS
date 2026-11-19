using System.Text.Json;

namespace LesMudOS.Web.Data;

public sealed record ClassPath(string Name, string Line);
public sealed record Calling(string Name, string Line, IReadOnlyList<ClassPath> Paths);
public sealed record Faction(string Name, string Motto, string Text);

/// <summary>The copy for the classes and factions pages, read from content/world.json so that it can be edited without code.</summary>
public sealed class WorldContent
{
    public IReadOnlyList<Calling> Callings { get; }
    public IReadOnlyList<Faction> Factions { get; }

    public WorldContent(IReadOnlyList<Calling> callings, IReadOnlyList<Faction> factions) { Callings = callings; Factions = factions; }

    public static WorldContent Load(string? file, out string? problem)
    {
        problem = null;
        try
        {
            if (file == null || !File.Exists(file)) { problem = "world.json not found"; return new WorldContent(Array.Empty<Calling>(), Array.Empty<Faction>()); }
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var callings = new List<Calling>();
            foreach (var c in doc.RootElement.GetProperty("callings").EnumerateArray())
                callings.Add(new Calling(c.GetProperty("name").GetString() ?? "", c.GetProperty("line").GetString() ?? "",
                    c.GetProperty("paths").EnumerateArray().Select(p => new ClassPath(p.GetProperty("name").GetString() ?? "", p.GetProperty("line").GetString() ?? "")).ToList()));
            var factions = doc.RootElement.GetProperty("factions").EnumerateArray()
                .Select(f => new Faction(f.GetProperty("name").GetString() ?? "", f.GetProperty("motto").GetString() ?? "", f.GetProperty("text").GetString() ?? "")).ToList();
            return new WorldContent(callings, factions);
        }
        catch (Exception ex)
        {
            problem = ex.Message;
            return new WorldContent(Array.Empty<Calling>(), Array.Empty<Faction>());
        }
    }
}
