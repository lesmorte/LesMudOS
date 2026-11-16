/// <summary>
/// Base room. A room offers one command per exit to every living object that
/// walks in; that is all "movement" is.
/// </summary>
public abstract class Room : MudObject
{
    protected string ShortDesc = "A room";
    protected string LongDesc = "A featureless room.";
    protected Dictionary<string, string> Exits = new();

    public override string? Short() => ShortDesc;

    public override void Init()
    {
        foreach (var direction in Exits.Keys)
        {
            AddAction(direction, GoExit);
            AddAction(direction.Substring(0, 1), GoExit); // "n" for "north"
        }
    }

    private bool GoExit(string args)
    {
        var player = ThisPlayer();
        var direction = Exits.Keys.FirstOrDefault(d => d.StartsWith(QueryVerb()));
        if (player == null || direction == null) return false;

        var name = player.Call<string>("QueryName") ?? "Someone";
        Say($"{name} leaves {direction}.\n");
        MoveObject(player, LoadObject(Exits[direction]));
        Say($"{name} arrives.\n");
        player.Call("Look");
        return true;
    }

    public override string Long()
    {
        var text = new StringBuilder();
        text.Append(ShortDesc).Append('\n').Append(LongDesc).Append('\n');
        text.Append(Exits.Count == 0 ? "There are no obvious exits.\n" : $"Obvious exits: {string.Join(", ", Exits.Keys)}\n");
        foreach (var ob in AllInventory())
        {
            if (ob == ThisPlayer()) continue;
            var desc = ob.Call<string>("Short");
            if (desc != null) text.Append(char.ToUpper(desc[0])).Append(desc.Substring(1)).Append(" is here.\n");
        }
        return text.ToString();
    }
}
