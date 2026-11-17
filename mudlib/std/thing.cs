/// <summary>Base for anything with a name and a description: items, creatures, players.</summary>
public abstract class Thing : MudObject
{
    protected string Name = "thing";
    protected string? ShortDesc;
    protected string? LongDesc;
    protected List<string> Ids = new();

    public string QueryName() => Name;

    public override bool Id(string id) => id == Name.ToLower() || Ids.Contains(id);

    public override string? Short() => ShortDesc ?? Name;

    public override string Long() => (LongDesc ?? $"You see nothing special about {Short()}.") + "\n";

    /// <summary>May a player pick this up?</summary>
    public virtual bool CanGet() => false;

    /// <summary>The numbers a builder wants to see: shown by the wizard command "stat".</summary>
    public virtual string Stat() => $"{Self.Name}: {Name}\n";

    /// <summary>The text with its first letter in upper case.</summary>
    protected static string Cap(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text.Substring(1);
}
