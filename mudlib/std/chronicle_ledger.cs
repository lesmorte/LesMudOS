// @inherit /std/thing

/// <summary>
/// The chronicle ledger: a big book on a stand that reads out the Chronicle of
/// Hollowmere. It is fixed in place. Put one in a room (the Adventurers Guild
/// now, the Hall of Legends in Oakenfell later) with MoveObject(CloneObject(...), Self)
/// from the room's Create().
///
/// `read chronicle` shows the recent entries; `chronicle`, `chronicle <number>`,
/// `chronicle search <word>`, `chronicle about <name>`, `chronicle firsts`,
/// `chronicle heroes`, `chronicle legends` and `chronicle records` do the rest.
/// All the work is in /secure/chronicle.
/// </summary>
public class ChronicleLedger : Thing
{
    private const string Daemon = "/secure/chronicle";

    public override void Create()
    {
        Name = "chronicle ledger";
        ShortDesc = "a chronicle ledger on a stand";
        LongDesc = "A great book bound in dark leather lies open on a carved oak stand. Its pages are written in many hands, and a brass plate on the stand reads: THE CHRONICLE OF HOLLOWMERE. You could {cmd}read{/} it, or use {cmd}chronicle{/} to search it.";
        Ids.AddRange(new[] { "ledger", "chronicle", "book", "stand", "chronicle ledger", "the chronicle" });
    }

    public override void Init() => AddAction("chronicle", CmdChronicle);

    /// <summary>`read chronicle` (the player's read command asks for this).</summary>
    public string? QueryReadText() => LoadObject(Daemon).Call<string>("RecentText", 10);

    private bool CmdChronicle(string args)
    {
        var text = LoadObject(Daemon).Call<string>("ChronicleCommand", args ?? "");
        if (text == null) return false;
        Write(text);
        return true;
    }
}
