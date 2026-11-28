// @inherit /std/hiddengiver

/// <summary>
/// Marian Ashcombe, Registrar of the Chancery (docs/specs/bureaucrat.md section 11): the giver of the Mage's hidden class. She begins the quest
/// "Jury Duty" when a summoned character comes through the inner door, swears the juror, takes the badge back and says the verdict's closing remark,
/// makes the offer, takes the two-step answer (the hidden-class base does the pledge and decline), and afterwards teaches the class's ranks and sells
/// its staff and seal to holders only. Everything she says to a character depends on the register's stage for that character; to anyone else
/// every word is the unknown-word answer.
/// </summary>
public class RegistrarAshcombe : HiddenGiver
{
    private const string Obj = "/domains/greenvale/oakenfell/founders/obj/";
    private static readonly string[] ChatLines =
    {
        "Kindly state your business, in writing if you can. The Chancery is open.",
        "A matter not entered is a matter that has not occurred. I say this to everyone. It is generally the first thing they dispute.",
        "Please form a queue. There is no queue. Please form one anyway.",
    };
    private readonly Dictionary<string, int> chatAt = new();

    private ObjectRef Register => LoadObject("/secure/hidden");
    private int StageOf(ObjectRef who) => Register.Call<int>("StageFor", who, HiddenId);
    private int NoteOf(ObjectRef who, string key) => Register.Call<int>("NoteOf", who, HiddenId, key, 0);

    private const string OfferText = "Marian Ashcombe closes the Book of Proceedings and, for the first time, looks at you. 'One further matter. The court has an opening. It is not a post, and it is not advertised, and nobody applies to it. A clerk of the Chancery is a mage who has taken the seal. You have this afternoon served three orders in this building without being struck by lightning, which is not nothing. If you wish to take the seal, say {cmd}pledge{/}. If you do not, say {cmd}decline{/}. Whichever you say, the juror's fee is yours and the court thanks you. I would think about it.'";

    public override void Create()
    {
        Name = "Marian Ashcombe";
        Ids.Add("marian");
        Ids.Add("ashcombe");
        Ids.Add("registrar");
        Ids.Add("marian ashcombe");
        Ids.Add("woman");
        ShortDesc = "Marian Ashcombe, Registrar of the Chancery";
        LongDesc = "A tall, unhurried woman in a grey coat with ink to the second knuckle and a brass seal on a cord at her throat. She is writing, and does not look up until she has finished the line.";
        Level = 20;
        MaxVitality = 90;

        HiddenId = "bureaucrat";
        QuestTitle = "Jury Duty";
        DefineQuest("jury-duty", "Jury Duty", "Oakenfell",
            "You were summoned to the Chancery of Oakenfell, sworn as a juror, heard the matter of the goose and the marrows, gave your verdict and answered the Registrar's last question. The court has entered all of it.",
            "You set the summons aside. The court notes that the matter is still open.",
            Obj + "jurors_badge," + Obj + "boundary_roll," + Obj + "mason_chit",
            "have|" + Obj + "jurors_badge|Be sworn in and take a juror's badge|1", false);
        RewardTakenText = "1 gold, 250 experience and a seal of the Chancery.";
        RewardDeclinedText = "1 gold, 250 experience and a Certificate of Jury Service.";
        RewardReleasedText = "1 gold.";
        PayGold = 1;
        PayXp = 250;
        KeepsakeTaken = Obj + "notarys_seal";
        KeepsakeDeclined = Obj + "jury_certificate";

        OfferLine = "Marian Ashcombe looks up. 'Still pending. It is perfectly in order for it to remain so.'";
        PermanenceLine = "Marian Ashcombe sets down her pen. 'I will say this once, as the Chancery requires. The seal is for life. You will keep all you are: the staff, the Bolt, the Lore. You will gain the seal, and the forms that go with it, and you will not be able to put either down. No fee is asked. If you are sure, say {cmd}pledge{/} again within the minute.'";
        DeclineWarning = "Marian Ashcombe inclines her head. 'You may decline. It is permitted, and it is final. The Chancery does not summon twice. Say {cmd}decline{/} again within the minute and it is entered.'";
        AcceptedLine = "Marian Ashcombe says: \"Your first form is Form 27-B. It is in triplicate. Please do not lose the carbon. And take this. The Chancery issues one to every clerk, and it is not to be sold.\" She hands you a seal on a cord. You have served the court; the juror's fee is yours.";
        DeclinedLine = "Marian Ashcombe says: \"Entered.\" She writes. \"Declined, with thanks. The court keeps the summons.\" She takes it from your pack and files it under Weather. She stamps a small card and hands it to you. \"A certificate of jury service. You are exempt for seven years. This is not a joke and it is not, so far as I know, a lie.\" She looks back at her ledger. \"The door will not trouble you again.\" You have served the court; the juror's fee is yours.";
        ReleaseLine = "Marian Ashcombe says: \"You have taken another seal. The Chancery does not issue a second. The fine is waived, as it was always going to be. The juror's fee is one gold, entered.\"";

        foreach (var line in ChatLines) AddChat(line, "jury-duty");

        AddStock(Obj + "clerks_staff", 45);
        AddStock(Obj + "notarys_seal", 60);
        TeachCalling("bureaucrat", 2, 5);
    }

    // ---- What she says: always to the one character, and written in their journal ----

    protected override void Speak(ObjectRef who, string line) => NarrateTo(who, QuestId, line);

    protected override void Ceremony(ObjectRef who)
    {
        var name = who.Call<string>("QueryName") ?? "Someone";
        NarrateTo(who, QuestId,
            "Marian Ashcombe rises. 'Raise your right hand. The other one. Repeat after me. I will lodge before I act. I will bind nothing that has not been served. I will not, upon any provocation, agree that a war was properly declared.' She takes the seal from the cord at her throat, presses it into a pool of hot wax beside your name in the Book of Proceedings, and blows on it. 'It will be dry in a moment.'",
            $"Marian Ashcombe presses the Chancery's seal into hot wax beside {name}'s name in the Book of Proceedings. The room is very still.");
    }

    protected override void Begin(ObjectRef who)
    {
        NarrateTo(who, QuestId, "Marian Ashcombe does not look up from her ledger until she has finished the line. 'Summons.' It is not a question. You hold it up. She reads the front, and the back, and stamps the corner. 'Reference accepted. You are Juror Twelve of Twelve. The other eleven are not coming. They never do. That is not your concern, and it is now partly your duty.' She slides a card across the counter. TAKE A NUMBER, it says, in case you had not noticed the machine. 'Take a number. Then ask me about the oath. The court sits when it is sworn.'");
    }

    protected override void Resume(ObjectRef who, int stage)
    {
        if (stage == 2) NarrateTo(who, QuestId, "Marian Ashcombe looks up. 'Juror. The oath, once you are numbered.'");
        else if (stage == 4) Return(who);
    }

    protected override string? Note(ObjectRef who, int stage) => stage switch
    {
        2 => "You are at the Chancery with a summons. Take a number, then ask the Registrar about the oath.",
        3 => "You are sworn. The court sits to the east: Pruett against Marrowby, the matter of the goose. Hear the parties; the Clerk will take your verdict.",
        4 => "The verdict is entered. Return your badge to the Registrar.",
        5 => "The Registrar has a question for you. Neither answer is urgent. Neither is reversible.",
        _ => null,
    };

    // ---- The return: the badge, the closing remark, the offer ----

    private static readonly string[,] Remarks =
    {
        { "Liable, on no exhibits. The court admires decisiveness. The court also wonders what it was based on.", "Liable, on one exhibit. A sound habit: when in doubt, find against the person who has been shouting.", "Liable, on both exhibits, with the elm in evidence. Rigorous. Mrs Marrowby has been told that the goose will in future be supervised." },
        { "Not liable, on no exhibits. Generous. The Chancery does not hold it against you.", "Not liable, on one exhibit. Cautious. It will survive an appeal, which is no part of the verdict.", "Not liable, on both exhibits. The elm has been moved, the marrows were on her side, and the goose cannot be shown to have known. Mr Pruett has appealed. He has appealed to the elm." },
    };

    /// <summary>A juror whose verdict is entered comes back: she takes the badge, reads the verdict back, and the offer follows. Returns the first paragraph.</summary>
    private string Return(ObjectRef who)
    {
        if (StageOf(who) != 4 || !AdvanceTo(who, 5)) return "";
        foreach (var item in AllInventory(who).ToList())
            if (item.IsAlive && (item.ProgramPath == Obj + "jurors_badge" || item.ProgramPath == Obj + "jury_ticket")) item.Call("Vanish");
        var verdict = NoteOf(who, "v") == 1 ? 0 : 1;
        var exhibits = (NoteOf(who, "roll") == 3 ? 1 : 0) + (NoteOf(who, "chit") == 3 ? 1 : 0);
        var first = "Marian Ashcombe holds out her hand. You give her the badge. She enters the verdict in the Book of Proceedings and reads it back to you, without comment, and with a different comment for each kind of verdict.";
        NarrateTo(who, QuestId, first);
        NarrateTo(who, QuestId, $"Marian Ashcombe says: \"{Remarks[verdict, exhibits]}\"");
        NarrateTo(who, QuestId, "Marian Ashcombe says: \"The juror's fee is one gold. It is entered.\"");
        NarrateTo(who, QuestId, OfferText);
        return first;
    }

    // ---- Ask ----

    private sealed record Rule(string Keys, int From, int To, Func<ObjectRef, string> Line);

    private string Said(string line) => $"Marian Ashcombe says: \"{line}\"";

    private List<Rule> JurorRules() => new()
    {
        new("oath,swear,sworn,badge", 2, 2, Oath),
        new("badge,lost,oath,sworn,swear", 3, 3, BadgeAgain),
        new("number,machine,ticket,clock", 2, 2, _ => Said("The machine is correct. The clock is correct. The time is wrong. If you can explain that to me, I will find you something better to do.")),
        new("summons,form,reference,juror,jury,duty", 2, 3, _ => Said("Form 27-J. Third copy. It means what it says. It said fourteen days, from the date you read it. You have read it. Nobody has ever been late.")),
        new("fine,thirty-seven,one third", 2, 3, _ => Said("The fine is thirty-seven gold and one third of a gold. It has never been collected. The third of a gold is the difficulty.")),
        new("court,east door,hearing,case,matter,goose", 2, 2, _ => Said("The court sits when you are sworn. It is Pruett against Marrowby, the matter of the goose. You will know it when you hear it.")),
        new("court,east door,hearing,case,matter,goose", 3, 3, _ => Said("The court is through the east door. Clerk Crake will take it from there.")),
        new("orders,authority,borrowed,trial,forms", 3, 3, _ => Said("In the court a juror may serve three of the court's orders, on its authority and for the length of the hearing. A restraining order, a notice to cease and desist, and a form in triplicate. They are on loan. They work on the parties and on Counsel, and nowhere else. They will not work in a corridor. Do not become attached.")),
        new("roll,boundary,pigeonhole,pigeonholes,weather", 3, 3, _ => Said("Boundary matters are filed under Weather, because they are always changing, and Weather is under Adjournments. Pigeonhole two hundred and twelve. I cannot stop you looking. I am not permitted to help.")),
        new("verdict,clerk,crake", 3, 3, _ => Said("That is for Clerk Crake. I only enter it.")),
    };

    private string Oath(ObjectRef who)
    {
        var carrying = AllInventory(who).Any(o => o.IsAlive && o.ProgramPath == Obj + "jury_ticket");
        if (!carrying && NoteOf(who, "ticket") == 0) return Said("I cannot swear an unnumbered juror. It has been tried. Take a number.");
        if (!AdvanceTo(who, 3)) return Said("I cannot swear you at present. The ink is wet.");
        foreach (var item in AllInventory(who).ToList())
            if (item.IsAlive && item.ProgramPath == Obj + "jury_ticket") item.Call("Vanish");
        MoveObject(CloneObject(Obj + "jurors_badge"), who);
        var name = who.Call<string>("QueryName") ?? "Someone";
        if (Environment() is { } room) TellRoom(room, $"Marian Ashcombe rises and swears {name} as a juror.\n", who);
        return "Marian Ashcombe rises. 'Raise your right hand. The other one. Repeat after me. I will hear the matter. I will not be moved by the length of the speeches. I will say yes or no, and mean one of them.' You say it. She stamps a small brass badge on the pad and slides it across. 'A juror's badge. It is a loan from the court and it will be asked for back. Through the east door. Clerk Crake will take it from there. Do not touch the goose.'";
    }

    private string BadgeAgain(ObjectRef who)
    {
        if (AllInventory(who).Any(o => o.IsAlive && o.ProgramPath == Obj + "jurors_badge"))
            return Said("You have it. You are wearing it. Is it not remarkable?");
        MoveObject(CloneObject(Obj + "jurors_badge"), who);
        return Said("Again? It is a loan.") + " She stamps another badge and slides it across.";
    }

    private List<Rule> HolderRules() => new()
    {
        new("chancery,this place", 9, 9, _ => Said("The Chancery of Oakenfell keeps the Assembly's papers and the city's peace, in that order, and the one has never once required the other. We have never lost a war. We have never agreed that one was properly declared.")),
        new("bureaucrat,clerk,clerks,class,path,seal", 9, 9, _ => Said("A Clerk of the Chancery is a Mage who has taken the seal. The common word is Bureaucrat, and I do not correct it, because to correct it would be to open a file. A clerk slows what must wait, bars what must not approach, stays what must not be said, and serves notice on what cannot read. It is not a calling for the impatient.")),
        new("triplicate,forms in triplicate", 9, 9, _ => Said("Form 27-B. Three copies. The creature keeps none and wishes it understood why.")),
        new("restraining order,restraining,restrain", 9, 9, _ => Said("It may not come within reach of you. It may do other things. The order is quite specific.")),
        new("cease and desist,desist,cease", 9, 9, _ => Said("Served at the moment it begins, which is the only moment it may be served.")),
        new("red tape,tape", 9, 9, _ => Said("A creature may not call for help without a countersignature. I need not tell you who countersigns.")),
        new("audit", 9, 9, _ => Said("Nothing is so useful as knowing precisely what one is dealing with. Nothing is so unpopular.")),
        new("spells,spell", 9, 9, _ => Said("Forms in Triplicate. A Restraining Order. Cease and Desist. Red Tape. An Audit. Ask me of any of them by name.")),
        new("boss,above the law,ignores your authority", 9, 9, _ => Said("There are things in Hollowmere that are above the law, and they are almost always the things one most wishes to serve. Half the order will hold; sometimes none. There are also things that ignore your authority, as there are in any office: they are only above your grade. They are served more often than you would think. A senior clerk is a good deal harder to ignore.")),
        new("fee,gold,price,ranks,staff,second seal", 9, 9, _ => Said("Ranks are forty, a hundred and fifty, four hundred and nine hundred, entered separately. The staff is forty-five. A second seal is sixty. Fees are posted.")),
        new("jury,court,goose", 9, 9, _ => Said("Mrs Marrowby's goose is, I understand, in good health. Mr Pruett's marrows are not.")),
    };

    public override string? Ask(ObjectRef who, string topic)
    {
        Spoken(who);
        var stage = StageOf(who);
        var asked = $"ask {Name} about {topic.Replace("{", "{{")}";
        var holder = who.Call<bool>("QueryHasClass", HiddenId);
        string? line = null;
        if (stage == 4) { var first = Return(who); return first == "" ? base.Ask(who, topic) : first; }
        if (stage == 5 && Register.Call<bool>("OfferIsOpen", who, HiddenId) && TopicHas(topic, "seal,offer,pledge,decline,class,question,matter,mage,opening"))
        {
            RecordQuest(who, QuestId, asked, OfferText);
            return OfferText;
        }
        if (stage >= 2 && stage <= 3)
        {
            line = Best(JurorRules(), stage, topic)?.Invoke(who);
            if (line == null)
            {
                var name = who.Call<string>("QueryName") ?? "";
                var i = chatAt.GetValueOrDefault(name);
                chatAt[name] = i + 1;
                line = Said(ChatLines[i % ChatLines.Length]);
            }
        }
        else if (holder) line = Best(HolderRules(), 9, topic)?.Invoke(who);
        if (line == null) return base.Ask(who, topic);
        RecordQuest(who, stage >= 2 && stage <= 3 ? QuestId : "", asked, line);
        return line;
    }

    private static Func<ObjectRef, string>? Best(List<Rule> rules, int stage, string topic)
    {
        topic = topic.ToLowerInvariant().Trim();
        Func<ObjectRef, string>? best = null;
        var bestLength = 0;
        foreach (var rule in rules)
        {
            if (stage < rule.From || stage > rule.To) continue;
            foreach (var raw in rule.Keys.Split(','))
            {
                var key = raw.Trim();
                if (key == "") continue;
                var length = topic == key ? 1000 + key.Length : topic.Contains(key) ? key.Length : 0;
                if (length > bestLength) { best = rule.Line; bestLength = length; }
            }
        }
        return best;
    }
}
