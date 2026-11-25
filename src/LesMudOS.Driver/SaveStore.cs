using System.Reflection;
using System.Text.Json;
using LesMudOS.Api;

namespace LesMudOS.Driver;

/// <summary>
/// save_object / restore_object. Persists the [Saved] fields of an object,
/// across its whole class hierarchy, as one JSON file under the save root.
/// Field types must be plain data (numbers, strings, lists, dictionaries);
/// object handles are not persisted.
/// </summary>
internal sealed class SaveStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, IncludeFields = true };
    private readonly string _root;

    public SaveStore(string root) => _root = Path.GetFullPath(root);

    /// <summary>The absolute save directory.</summary>
    public string Root => _root;

    private string FileFor(string mudPath) => _root + mudPath + ".json";

    private static IEnumerable<FieldInfo> SavedFields(MudObject instance)
    {
        for (var type = instance.GetType(); type is not null && type != typeof(MudObject); type = type.BaseType)
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (field.IsDefined(typeof(SavedAttribute), false))
                    yield return field;
    }

    public void Save(MudObject instance, string mudPath)
    {
        var data = new SortedDictionary<string, JsonElement>();
        foreach (var field in SavedFields(instance))
            data[field.Name] = JsonSerializer.SerializeToElement(field.GetValue(instance), field.FieldType, Options);

        var file = FileFor(mudPath);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, file, overwrite: true);
    }

    public bool Restore(MudObject instance, string mudPath)
    {
        var file = FileFor(mudPath);
        if (!File.Exists(file)) return false;
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(file), Options);
        if (data is null) return false;
        foreach (var field in SavedFields(instance))
        {
            if (!data.TryGetValue(field.Name, out var element)) continue;
            try
            {
                field.SetValue(instance, element.Deserialize(field.FieldType, Options));
            }
            catch (JsonException)
            {
                // The field changed type since the file was written; keep the default.
            }
        }
        return true;
    }
}
