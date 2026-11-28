// @inherit /std/room

/// <summary>
/// The Chancery: Court of First Instance (docs/specs/bureaucrat.md section 2.4 and 11.3): where the case "Pruett v. Marrowby, the matter of the goose"
/// is heard. The state of each juror's case is in the register (/secure/hidden). This room holds the court's three lent orders (restrain, desist,
/// triplicate) as QUEST-SCRIPTED effects: each acts on the case's own people through their own code, costs nothing, needs no skill and is not saved,
/// and works only for a juror whose stage is 3 (sworn, case open). Nothing here touches the combat engine or /secure/control.
///
/// HOW STAGE 5's REAL SPELLS REPLACE THEM LATER: the three verbs below are the only place the court's orders are bound, and each calls one public
/// method of one person (Pruett.Restrain, Marrowby.Desist, Goose.Triplicate). A real spell file (/skills/bureaucrat/restraining_order, cease_and_desist,
/// triplicate) can serve the same people by calling those same three methods from its Resolve (or the methods can be changed to read the control
/// daemon's save and tags), and these verbs removed; a Bureaucrat (stage 9) already falls through to their own skill command because the handlers
/// return false for anyone whose stage is not 3.
/// </summary>
public class ChanceryCourt : Room
{
    private const string Cls = "bureaucrat";
    private const string Obj = "/domains/greenvale/oakenfell/founders/obj/";
    private const string Npcs = "/domains/greenvale/oakenfell/founders/npc/";

    private ObjectRef Register => LoadObject("/secure/hidden");

    public override void Create()
    {
        ShortDesc = "The Chancery: Court of First Instance";
        LongDesc = "A small, high court with a raised bench of black oak, a clerk's desk below it, a witness stand with a rail worn pale by hands, and two long tables facing each other across a strip of green carpet. A box of twelve chairs stands to one side; one of them has a cushion. A tall window of painted glass shows a sky that has been the same since the room was built. A brass plate on the bench reads THE COURT IS ALWAYS SITTING. There is straw on the floor, and a smell of goose.";
        Safe = true;
        Light = LightLit;
        Exits["west"] = "/domains/greenvale/oakenfell/founders/chancery";

        AddDetail("bench,raised bench,black bench,plate,brass plate", "The Clerk of the Court's bench. A brass plate reads THE COURT IS ALWAYS SITTING. Beneath it, in pencil: whether at you is another matter.");
        AddDetail("desk,clerk's desk,clerks desk,bell,stamp pad", "Piled with exhibit tags, a stamp pad and a bell that says 'ring for the clerk'. The clerk is two feet away.");
        AddDetail("witness stand,stand,rail", "A rail worn pale by hands. Everyone who has stood here has held on.");
        AddDetail("tables,table,left-hand table,right-hand table,left table,right table,drawer", "Two long tables facing each other across a strip of green carpet. The left-hand table is the plaintiff's, the right-hand the defendant's. Each has a drawer, and the left-hand one does not quite close.");
        AddDetail("chairs,box,jury box,cushion,chair", "Twelve chairs. Eleven are for the other jurors, who have not come. The twelfth has a cushion, and is yours. It is a very good cushion. It has been sat on by an honest person, once.");
        AddDetail("window,glass,painted glass", "Painted glass: a sky, a hill, a small scale held by a hand. The scale is very slightly out.");
        AddDetail("straw", "It is straw. It is not decorative.");

        // The mason's chit, in the drawer of the left-hand table: found per character, only by a sworn juror who has heard Tilda Marrowby (SearchModifier, ThingFound).
        HideClue("chit", 12, "table,tables,left-hand table,left table,drawer", "In the drawer of the left-hand table, under a stub of wax and a peppermint, is a slip of paper, curled with age. You take it.", "", "");

        AddSpawn(Npcs + "clerk_crake");
        AddSpawn(Npcs + "pruett");
        AddSpawn(Npcs + "marrowby");
        AddSpawn(Npcs + "goose");
    }

    private int Stage(ObjectRef who) => Register.Call<int>("StageFor", who, Cls);

    private int Note(ObjectRef who, string key) => Register.Call<int>("NoteOf", who, Cls, key, 0);

    protected override int SearchModifier(ObjectRef searcher, bool byFeel)
        => Stage(searcher) == 3 && Note(searcher, "t_heard") == 1 && Note(searcher, "chit") == 0 ? base.SearchModifier(searcher, byFeel) : -40;

    protected override void ThingFound(ObjectRef who, string id)
    {
        if (id != "chit" || Stage(who) != 3 || Note(who, "chit") != 0) return;
        MoveObject(CloneObject(Obj + "mason_chit"), who);
        Register.Call<bool>("Note", who, Cls, "chit", 1);
    }

    // ---- The three lent orders ----

    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        AddAction("restrain", args => Order("restrain", args));
        AddAction("desist", args => Order("desist", args));
        AddAction("triplicate", args => Order("triplicate", args));
    }

    private bool Order(string order, string args)
    {
        var who = ThisPlayer();
        if (who == null || Stage(who) != 3) return false; // not a juror with the case open: the word is not the court's (a Bureaucrat's own skill, or "What?")
        var word = args.Trim().ToLowerInvariant();
        if (word == "") { TellObject(who, "Serve whom?\n"); return true; }
        var target = Present(word, Self);
        if (target == null) { TellObject(who, "Serve whom?\n"); return true; }
        var path = target.ProgramPath;
        var parties = new Dictionary<string, string> { ["restrain"] = Npcs + "pruett", ["desist"] = Npcs + "marrowby", ["triplicate"] = Npcs + "goose" };
        if (order == "desist" && path == Npcs + "pruett") { TellObject(who, "Gervase Pruett is not saying anything that has not already been filed.\n"); return true; }
        if (path == Npcs + "goose" && order != "triplicate") { TellObject(who, "The court's authority does not stretch that far. The goose is Counsel, not a party.\n"); return true; }
        if (path != parties[order]) { TellObject(who, "The court's authority does not stretch that far.\n"); return true; }
        var method = order == "restrain" ? "Restrain" : order == "desist" ? "Desist" : "Triplicate";
        target.Call(method, who);
        return true;
    }
}
