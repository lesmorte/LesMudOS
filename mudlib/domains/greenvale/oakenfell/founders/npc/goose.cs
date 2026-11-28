// @inherit /std/npc

/// <summary>
/// The goose, Counsel for the Defence (docs/specs/bureaucrat.md section 11.3): the comic interruption. When a juror who holds the Boundary Roll walks into the
/// court the goose takes it, and keeps it until it has been served a trial form in triplicate (and then lets the juror take it back). It never fights.
/// </summary>
public class CounselGoose : Npc
{
    private const string Cls = "bureaucrat";
    private const string Q = "jury-duty";
    private const string Obj = "/domains/greenvale/oakenfell/founders/obj/";

    private ObjectRef Register => LoadObject("/secure/hidden");
    private int Stage(ObjectRef who) => Register.Call<int>("StageFor", who, Cls);
    private int Note(ObjectRef who, string key) => Register.Call<int>("NoteOf", who, Cls, key, 0);
    private void SetNote(ObjectRef who, string key, int value) => Register.Call<bool>("Note", who, Cls, key, value);

    public override void Create()
    {
        Name = "goose";
        Ids.Add("goose");
        Ids.Add("counsel");
        Ids.Add("grey goose");
        Ids.Add("counsel for the defence");
        ShortDesc = "a goose, Counsel for the Defence";
        LongDesc = "A large grey goose with an expression of settled judgement. A small card on a string round its neck reads COUNSEL.";
        Level = 1;
        MaxVitality = 10;
    }

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Interactive(other)) return;
        AddAction("take", CmdTake);
        AddAction("get", CmdTake);
        AddAction("kill", CmdKill);
        AddAction("attack", CmdKill);
        CallOut(() => Interrupt(other), 2);
    }

    /// <summary>A juror carrying the Roll walks in: Counsel asks to see the exhibit.</summary>
    private void Interrupt(ObjectRef who)
    {
        if (!who.IsAlive || Environment(who) != Environment() || Stage(who) != 3 || Note(who, "roll") != 1) return;
        var held = AllInventory(who).FirstOrDefault(o => o.IsAlive && o.ProgramPath == Obj + "boundary_roll");
        if (held == null) return;
        if (Note(who, "gs") == 1)
        {
            NarrateTo(who, Q, "Counsel rises, looks at you, and then at the Roll, and then, slowly, at the space where its second copy should be. It sits.");
            return;
        }
        held.Call("Vanish");
        SetNote(who, "roll", 2);
        var text = "Counsel rises. It crosses the court with great dignity and at surprising speed, takes the Boundary Roll out of your hand, and returns to the right-hand table with it. Clerk Crake does not look up. 'Counsel has asked to see the exhibit,' he says. 'Counsel always asks to see the exhibit.'";
        NarrateTo(who, Q, text, $"Counsel rises, crosses the court with great dignity and at surprising speed, and takes something out of {who.Call<string>("QueryName")}'s hand.");
    }

    public override string? Ask(ObjectRef who, string topic)
    {
        Spoken(who);
        var holds = Stage(who) == 3 && Note(who, "roll") == 2 && TopicHas(topic, "roll,boundary,exhibit");
        var text = holds
            ? "The goose looks at the Roll, and then at you, and holds the Roll a little higher."
            : "The goose looks at you. You have the impression of having been entered.";
        if (Stage(who) == 3) RecordQuest(who, Q, $"ask {Name} about {topic.Replace("{", "{{")}", text);
        return text;
    }

    /// <summary>The court's lent form in triplicate, served by a sworn juror (quest-scripted: see ChanceryCourt).</summary>
    public void Triplicate(ObjectRef who)
    {
        if (Stage(who) != 3) return;
        var name = who.Call<string>("QueryName") ?? "Someone";
        if (Note(who, "gs") == 1)
        {
            NarrateTo(who, Q, "The goose is still looking for the second copy.");
            return;
        }
        SetNote(who, "gs", 1);
        NarrateTo(who, Q, "You serve the goose with a trial Form 27-B, in triplicate, on the court's authority. It begins, slowly, to look for the second copy. It does not put the Roll down. It is, however, looking elsewhere.",
            $"{name} serves the goose with a form. The goose studies it.");
    }

    private bool CmdTake(string args)
    {
        var who = ThisPlayer();
        var word = args.Trim().ToLowerInvariant();
        if (who == null || Stage(who) != 3 || Note(who, "roll") != 2 || word is not ("roll" or "the roll" or "boundary roll" or "the boundary roll")) return false;
        if (Note(who, "gs") != 1)
        {
            NarrateTo(who, Q, "The goose is holding it. The goose looks at you.");
            return true;
        }
        MoveObject(CloneObject(Obj + "boundary_roll"), who);
        SetNote(who, "roll", 1);
        NarrateTo(who, Q, "The goose is still looking for the second copy. You take the Roll from under its wing. It lets you.");
        return true;
    }

    private bool CmdKill(string args)
    {
        var word = args.Trim().ToLowerInvariant();
        if (word is not ("goose" or "the goose" or "counsel" or "grey goose")) return false;
        TellObject(ThisPlayer()!, "The court takes a dim view.\n");
        return true;
    }
}
