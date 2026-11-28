// @inherit /std/npc

/// <summary>Gervase Pruett, plaintiff (docs/specs/bureaucrat.md section 11.3). Peaceful; hears the court's lent restraining order.</summary>
public class GervasePruett : Npc
{
    private const string Cls = "bureaucrat";
    private const string Q = "jury-duty";

    private ObjectRef Register => LoadObject("/secure/hidden");
    private int Stage(ObjectRef who) => Register.Call<int>("StageFor", who, Cls);
    private int Note(ObjectRef who, string key) => Register.Call<int>("NoteOf", who, Cls, key, 0);

    public override void Create()
    {
        Name = "Gervase Pruett";
        Ids.Add("gervase");
        Ids.Add("pruett");
        Ids.Add("plaintiff");
        Ids.Add("gardener");
        Ids.Add("gervase pruett");
        ShortDesc = "Gervase Pruett, plaintiff";
        LongDesc = "A square man in a gardener's apron, red to the ears. He is gripping a marrow-shaped hole in the air.";
        Level = 3;
        MaxVitality = 20;
    }

    public override string? Ask(ObjectRef who, string topic)
    {
        Spoken(who);
        if (Stage(who) != 3 || !TopicHas(topic, "marrows,marrow,goose,boundary,elm,claim,line,case,roll,stakes,tilda,marrowby"))
            return base.Ask(who, topic);
        var asked = $"ask {Name} about {topic.Replace("{", "{{")}";
        string text;
        if (Note(who, "p_heard") == 0)
        {
            Register.Call<bool>("Note", who, Cls, "p_heard", 1);
            text = "Gervase Pruett says: \"Three marrows. Three! Prize marrows, madam, sir, whatever you are. The Harvest Show is in nine days. The goose ate them, and the goose is hers, and the line is where the Roll says it is, and the Roll says my bed is mine. Eleven gold. And the stakes.\" He sits. He stands again. \"And the stakes.\"";
        }
        else text = "Gervase Pruett says: \"Eleven gold. And the stakes.\"";
        RecordQuest(who, Q, asked, text);
        return text;
    }

    /// <summary>The court's lent restraining order, served by a sworn juror (quest-scripted: see ChanceryCourt). Once it is served, Tilda may be heard.</summary>
    public void Restrain(ObjectRef who)
    {
        if (Stage(who) != 3) return;
        var name = who.Call<string>("QueryName") ?? "Someone";
        if (Note(who, "pr") == 1)
        {
            NarrateTo(who, Q, "Pruett has been served. He is writing about it.");
            return;
        }
        Register.Call<bool>("Note", who, Cls, "pr", 1);
        NarrateTo(who, Q, "You serve Gervase Pruett with a trial restraining order, on the court's authority. He may not come within reach of Tilda Marrowby, which he takes personally. He sits, writes something, and underlines it.",
            $"{name} serves Gervase Pruett with a restraining order. Pruett sits.");
    }
}
