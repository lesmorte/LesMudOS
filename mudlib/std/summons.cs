// @inherit /std/item

/// <summary>
/// The paper that serves a hidden class's summons (docs/specs/hidden-classes.md section 5): bound to its holder (cannot be dropped, given or
/// sold; a death leaves it on them), and read from the class's own text, so a content file sets only its name and which class it belongs to.
/// {Name}, {serial} and {date} in the text are the holder's name, the number the register gave it and the game date. Never shown to anyone
/// who does not hold it: the item is only ever cloned into a pack by the register's own door or a corpse the holder looted.
/// </summary>
public class SummonsPaper : Item
{
    /// <summary>The hidden class this paper belongs to (its id).</summary>
    protected string ClassId = "";

    public override void Create()
    {
        QuestItem = true;
        Bound = true;
        Value = 0;
    }

    public bool IsSummonsPaper() => true;

    private ObjectRef? ClassFile()
    {
        try { return LoadObject("/secure/classes").Call<ObjectRef>("Class", ClassId); }
        catch (MudRuntimeException) { return null; }
    }

    public override string QueryBoundLine() => ClassFile()?.Call<string>("QueryLine", "bound") is { Length: > 0 } line ? line : base.QueryBoundLine();

    public override string? QueryReadText()
    {
        var holder = Environment();
        var text = ClassFile()?.Call<string>("QueryLine", "text") ?? "";
        if (text == "" || holder == null || !holder.IsAlive || !holder.Call<bool>("IsPlayerCharacter")) return null;
        var serial = LoadObject("/secure/hidden").Call<int>("SerialOf", holder, ClassId);
        return text.Replace("{Name}", holder.Call<string>("QueryName") ?? "")
                   .Replace("{serial}", serial.ToString())
                   .Replace("{date}", LoadObject("/secure/chronicle").Call<string>("DateText") ?? "");
    }
}
