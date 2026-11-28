// @inherit /std/npc

/// <summary>
/// Osbert Crake, Clerk of the Court (docs/specs/bureaucrat.md section 11.3): opens the hearing for a sworn juror, takes the two exhibits (submit) and the
/// verdict (ask about it; the next line typed answers y or n). The case of each juror is in the register (/secure/hidden); this person keeps only
/// the "court is recovering" pause (20 seconds, in memory, not saved).
/// </summary>
public class ClerkCrake : Npc
{
    private const string Cls = "bureaucrat";
    private const string Q = "jury-duty";
    private const string Obj = "/domains/greenvale/oakenfell/founders/obj/";
    private readonly Dictionary<string, long> recovering = new();

    private ObjectRef Register => LoadObject("/secure/hidden");
    private int Stage(ObjectRef who) => Register.Call<int>("StageFor", who, Cls);
    private int Note(ObjectRef who, string key) => Register.Call<int>("NoteOf", who, Cls, key, 0);
    private void SetNote(ObjectRef who, string key, int value) => Register.Call<bool>("Note", who, Cls, key, value);

    public override void Create()
    {
        Name = "Osbert Crake";
        Ids.Add("osbert");
        Ids.Add("crake");
        Ids.Add("clerk");
        Ids.Add("clerk of the court");
        Ids.Add("osbert crake");
        ShortDesc = "Osbert Crake, Clerk of the Court";
        LongDesc = "A small, pale man in a black gown and a wig that was made for a larger head. He holds a bell in one hand, a pen in the other, and an air of having been right for a long time without anyone noticing.";
        Level = 12;
        MaxVitality = 60;
    }

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Interactive(other)) return;
        AddAction("submit", CmdSubmit);
        CallOut(() => Open(other), 1);
    }

    /// <summary>The hearing opens, once, for a sworn juror who walks in.</summary>
    private void Open(ObjectRef who)
    {
        if (!who.IsAlive || Environment(who) != Environment() || Stage(who) != 3 || Note(who, "entered") != 0) return;
        SetNote(who, "entered", 1);
        var text = "Clerk Crake rings his bell. 'All rise,' he says, and then, seeing that you are already standing, 'Noted.' He consults a paper. 'Pruett against Marrowby: the matter of the marrows. Three marrows, prize, the Harvest Show nine days hence, said to have been eaten by a goose, said to belong to the defendant, said by the defendant to belong to no one. The plaintiff claims eleven gold, and the stakes. Counsel for the defence is the goose, entered as such since Tuesday. The jury is you.' He puts the bell down. 'You will hear the plaintiff and the defendant. You may examine the exhibits, which are not yet exhibits. You will give a verdict, yes or no. You may use the court's orders on the parties if the proceedings require it. They usually do.'";
        NarrateTo(who, Q, text, text);
    }

    /// <summary>Called by the person of Tilda Marrowby when her history was cut short or ran on: the court takes no question for twenty seconds.</summary>
    public void Recover(ObjectRef who)
    {
        recovering[who.Call<string>("QueryName") ?? ""] = TimeMs() + 20000;
        if (recovering.Count > 100) foreach (var key in recovering.Where(kv => kv.Value < TimeMs()).Select(kv => kv.Key).ToList()) recovering.Remove(key);
    }

    private string Say(ObjectRef who, string asked, string line)
    {
        RecordQuest(who, Q, asked, line);
        return Speech(line);
    }

    public override string? Ask(ObjectRef who, string topic)
    {
        Spoken(who);
        var stage = Stage(who);
        var asked = $"ask {Name} about {topic.Replace("{", "{{")}";
        if (stage == 3 && recovering.TryGetValue(who.Call<string>("QueryName") ?? "", out var until) && until > TimeMs())
            return Say(who, asked, "The court is recovering.");
        if (stage == 3 && TopicHas(topic, "verdict,finished,decide,decision,judgment,judgement"))
        {
            if (Note(who, "p_heard") != 1 || Note(who, "t_heard") != 1)
                return Say(who, asked, "The court cannot take a verdict from a juror who has not heard the parties. It has tried.");
            var prompt = "Clerk Crake rises. 'The jury will say, yes or no, and without elaboration. Is the defendant liable for the marrows? (y/n)'";
            RecordQuest(who, Q, asked, prompt);
            InputTo(line => Verdict(who, line, 0));
            return prompt;
        }
        if (stage == 3 && TopicHas(topic, "exhibit,exhibits,evidence,submit,roll,chit,receipt"))
            return Say(who, asked, "Anything you find, hand to me with {cmd}submit{/}. The court does not take things from jurors. It takes them from witnesses, and you are, for this purpose, both.");
        if (stage == 3 && TopicHas(topic, "court,case,matter,goose,marrows,parties,hearing"))
            return Say(who, asked, "You will hear the plaintiff and the defendant. Ask them. Then, when you have heard enough, ask me for the verdict.");
        if (stage == 4 && TopicHas(topic, "verdict,finished,decide,court,case,matter"))
            return Say(who, asked, "The matter is heard. Return your badge to the Registrar. She will have a question for you. I would not be hurried.");
        return base.Ask(who, topic);
    }

    /// <summary>The line typed after the question: yes, no, or not an answer (twice, and the question is put aside).</summary>
    private void Verdict(ObjectRef who, string line, int wrong)
    {
        if (!who.IsAlive || Environment(who) != Environment() || Stage(who) != 3) return;
        var reply = line.Trim().ToLowerInvariant();
        int verdict = reply is "y" or "yes" ? 1 : reply is "n" or "no" ? 2 : 0;
        if (verdict == 0)
        {
            if (wrong == 0)
            {
                var again = "Clerk Crake says: \"The court requires a yes or a no. The court has waited longer for less.\"";
                NarrateTo(who, Q, again, "");
                InputTo(next => Verdict(who, next, 1));
            }
            else NarrateTo(who, Q, "Clerk Crake says: \"The court will ask again when you are ready.\"", "");
            return;
        }
        SetNote(who, "v", verdict);
        var entered = verdict == 1
            ? "'The court enters: liable.' He writes. 'Eleven gold, and the stakes. The court regrets the stakes.'"
            : "'The court enters: not liable.' He writes. 'Mr Pruett will be informed. He will be informed at length.'";
        var text = $"Clerk Crake rises. {entered}\n" +
                   "'The marrows,' says Gervase Pruett, to no one. 'The stakes.'\n" +
                   "Tilda Marrowby nods to the court as one nods to the weather, and collects the goose.\n" +
                   "'The matter is heard. Return your badge to the Registrar. She will have a question for you. I would not be hurried.'";
        NarrateTo(who, Q, text, text);
        Register.Call<bool>("Advance", who, Cls, 4);
    }

    // ---- Exhibits ----

    private bool CmdSubmit(string args)
    {
        var who = ThisPlayer();
        if (who == null || Stage(who) != 3) return false;
        var word = args.Trim().ToLowerInvariant();
        if (word == "") { TellObject(who, "Submit what?\n"); return true; }
        ObjectRef? match = null;
        foreach (var item in AllInventory(who).ToList())
            if (item.IsAlive && item.Call<bool>("Id", word)) { match = item; break; }
        if (match == null) { TellObject(who, "You have nothing the court recognises.\n"); return true; }
        string line, key;
        if (match.ProgramPath == Obj + "boundary_roll")
        {
            key = "roll";
            line = "You hand the Roll to Clerk Crake. He holds it at arm's length, as one holds a baby that has been returned. 'Exhibit One,' he says, and writes.";
        }
        else if (match.ProgramPath == Obj + "mason_chit")
        {
            key = "chit";
            line = "Clerk Crake reads the chit twice. 'Exhibit Two,' he says, quietly. At the left-hand table Gervase Pruett finds something very interesting in his boots.";
        }
        else { TellObject(who, "Clerk Crake does not accept that. The court has never accepted that.\n"); return true; }
        match.Call("Vanish");
        SetNote(who, key, 3);
        NarrateTo(who, Q, line, line);
        return true;
    }
}
