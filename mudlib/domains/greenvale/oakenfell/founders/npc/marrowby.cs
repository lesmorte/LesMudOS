// @inherit /std/npc

/// <summary>
/// Tilda Marrowby, defendant (docs/specs/bureaucrat.md section 11.3). While Pruett is unrestrained he interrupts her; once he is served she begins
/// her family history and a juror has 8 seconds to serve a notice to cease and desist before it runs on (and costs only the Clerk's patience: twenty
/// seconds). Either way she then says where the case turns. The window is memory only (not saved); the case is in the register.
/// </summary>
public class TildaMarrowby : Npc
{
    private const string Cls = "bureaucrat";
    private const string Q = "jury-duty";
    private const string Npcs = "/domains/greenvale/oakenfell/founders/npc/";
    private const string Statement = "Tilda Marrowby says: \"The goose is mine. I have never said otherwise. I said it is not guilty, which is different. It was on my side of the line when it ate his marrows. Ask the court where the line is. Hollis the mason moved the old elm, and Pruett's father paid him for it; there will be a receipt, because there is always a receipt. Try the table.\"";

    private readonly Dictionary<string, long> window = new();

    private ObjectRef Register => LoadObject("/secure/hidden");
    private int Stage(ObjectRef who) => Register.Call<int>("StageFor", who, Cls);
    private int Note(ObjectRef who, string key) => Register.Call<int>("NoteOf", who, Cls, key, 0);
    private string KeyOf(ObjectRef who) => who.Call<string>("QueryName") ?? "";

    public override void Create()
    {
        Name = "Tilda Marrowby";
        Ids.Add("tilda");
        Ids.Add("marrowby");
        Ids.Add("defendant");
        Ids.Add("tilda marrowby");
        ShortDesc = "Tilda Marrowby, defendant";
        LongDesc = "A broad, composed woman in a good shawl, who looks at the court as one would look at weather.";
        Level = 3;
        MaxVitality = 20;
    }

    public override string? Ask(ObjectRef who, string topic)
    {
        Spoken(who);
        if (Stage(who) != 3 || !TopicHas(topic, "goose,marrows,marrow,boundary,elm,line,case,claim,hester,history,receipt,table,mason"))
            return base.Ask(who, topic);
        var asked = $"ask {Name} about {topic.Replace("{", "{{")}";
        string text;
        if (Note(who, "pr") == 0)
            text = "Gervase Pruett is on his feet before she can answer. 'Objection! She is lying! The goose is hers! The goose has always been hers!' The Clerk rings his bell. 'The court notes the objection,' he says. 'The court notes that it was not made to anything.' Tilda looks at her hands. 'Jurors,' Clerk Crake adds, to no one in particular, 'have been known to have a form for that.'";
        else if (Note(who, "t_heard") == 1) text = Statement;
        else if (window.TryGetValue(KeyOf(who), out var until) && until > TimeMs())
            text = "Tilda Marrowby is in the middle of a sentence. It is a long one.";
        else
        {
            text = "Tilda Marrowby draws a very long breath. 'It begins,' she says, 'with my great-grandmother, Hester Marrowby, who came to Greenvale in the year of the late frost, and who kept geese, and whose mother kept geese, and whose mother's mother-'";
            window[KeyOf(who)] = TimeMs() + 8000;
            if (window.Count > 100) foreach (var key in window.Where(kv => kv.Value < TimeMs() - 60000).Select(kv => kv.Key).ToList()) window.Remove(key);
            CallOut(() => RunsOn(who), 8.5);
        }
        RecordQuest(who, Q, asked, text);
        return text;
    }

    /// <summary>Eight seconds have gone and nobody served the notice: the history proceeds, the Clerk waits, and she says what she was going to say.</summary>
    private void RunsOn(ObjectRef who)
    {
        var key = KeyOf(who);
        if (!window.TryGetValue(key, out var until) || until == 0 || until > TimeMs()) return; // served in time, or begun again
        window.Remove(key);
        if (!who.IsAlive || Environment(who) != Environment() || Stage(who) != 3) return;
        Register.Call<bool>("Note", who, Cls, "ds", 2);
        Register.Call<bool>("Note", who, Cls, "t_heard", 1);
        if (Environment() is { } room && AllInventory(room).FirstOrDefault(o => o.IsAlive && o.ProgramPath == Npcs + "clerk_crake") is { } clerk) clerk.Call("Recover", who);
        var text = "The history proceeds as far as the Founders' second winter. Clerk Crake rings his bell and waits. At length: 'Briefly, then,' says Tilda, and has the grace to look tired.";
        NarrateTo(who, Q, text, text);
        NarrateTo(who, Q, Statement);
    }

    /// <summary>The court's lent notice to cease and desist, served by a sworn juror (quest-scripted: see ChanceryCourt).</summary>
    public void Desist(ObjectRef who)
    {
        if (Stage(who) != 3) return;
        var key = KeyOf(who);
        var name = who.Call<string>("QueryName") ?? "Someone";
        if (window.TryGetValue(key, out var until) && until > TimeMs())
        {
            window.Remove(key);
            Register.Call<bool>("Note", who, Cls, "ds", 1);
            Register.Call<bool>("Note", who, Cls, "t_heard", 1);
            NarrateTo(who, Q, "You serve Tilda Marrowby with a trial notice to cease and desist, on the court's authority. Her great-grandmother stops mid-sentence. 'Oh,' says Tilda. 'Well. Briefly, then.'",
                $"{name} serves Tilda Marrowby with a notice to cease and desist. Her great-grandmother stops mid-sentence.");
            NarrateTo(who, Q, Statement);
            return;
        }
        NarrateTo(who, Q, "Tilda Marrowby is not saying anything that has not already been filed.");
    }
}
