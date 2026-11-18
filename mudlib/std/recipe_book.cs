// @inherit /std/item

/// <summary>
/// A book, scroll or page that teaches recipes to whoever reads it. Set Name, ShortDesc, Ids, ReadText and add the recipe ids
/// it teaches with Teaches.Add("berry_juice") in Create(). Reading it ("read book") shows the text and teaches each recipe the
/// reader does not know yet. ConsumedOnRead makes it crumble after it has taught someone something (a one-use scroll). Only a
/// book whose file lies in a trusted tree can teach; a Secret recipe cannot be taught by any book. See docs/CRAFTING.md.
/// </summary>
public class RecipeBook : Item
{
    /// <summary>The recipe ids this book teaches.</summary>
    protected List<string> Teaches = new();
    /// <summary>The book is used up once it has taught its reader anything.</summary>
    protected bool ConsumedOnRead;

    public RecipeBook()
    {
        Name = "recipe book";
        Ids.Add("book");
        ShortDesc = "a recipe book";
        ReadText = "The pages are covered in careful notes.";
    }

    public List<string> QueryTeaches() => new(Teaches);

    public override void Init()
    {
        base.Init();
        AddAction("read", CmdRead);
    }

    // Handles "read <this book>" only; anything else goes on to the ordinary read command.
    private bool CmdRead(string args)
    {
        var reader = ThisPlayer();
        if (reader == null || args == "" || !Self.IsAlive) return false;
        var room = Environment(reader);
        var id = args.Trim().ToLowerInvariant();
        var named = Present(id, reader) ?? (room == null ? null : Present(id, room));
        if (named != Self) return false;
        Write((ReadText ?? "").TrimEnd('\n') + "\n");
        var learned = 0;
        foreach (var recipe in Teaches)
        {
            string answer;
            try { answer = LoadObject("/secure/crafting").Call<string>("Teach", reader, recipe) ?? "refused"; }
            catch (MudRuntimeException) { answer = "refused"; }
            if (answer.StartsWith("ok")) learned++;
        }
        if (Teaches.Count > 0 && learned == 0) Write("There is nothing here you do not already know.\n");
        if (learned > 0 && ConsumedOnRead)
        {
            Write($"{Cap(Short() ?? "It")} crumbles to nothing as you finish reading.\n");
            UseOne();
        }
        return true;
    }

    public override string Stat() => base.Stat() + $"  Teaches: {string.Join(", ", Teaches)}{(ConsumedOnRead ? " (consumed on read)" : "")}\n";
}
