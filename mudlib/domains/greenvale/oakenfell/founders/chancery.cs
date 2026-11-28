// @inherit /std/room

/// <summary>
/// The Chancery: Waiting Room (docs/specs/bureaucrat.md section 2.4). Behind City Hall's inner door, which exists only for a character the
/// hidden-class register admits (GatedExit in city_hall.cs). Here the quest "Jury Duty" begins (the Registrar), a ticket is taken from the machine,
/// the juror is sworn and given a badge for the east door, and the Boundary Roll is found in the pigeonholes. The state of each character's case is
/// in the register (/secure/hidden), not here: this room is shared by everyone and keeps nothing but the next ticket number.
/// </summary>
public class ChanceryWaitingRoom : Room
{
    private const string Cls = "bureaucrat";
    private const string Obj = "/domains/greenvale/oakenfell/founders/obj/";
    private int nextTicket = 4;

    private ObjectRef Register => LoadObject("/secure/hidden");

    public override void Create()
    {
        ShortDesc = "The Chancery: Waiting Room";
        LongDesc = "A long, low room with a floor of worn green tile and a ceiling that has been repainted so often it has lost its corners. Wooden benches, each bolted to the floor facing a different wall, run down one side. At the far end a brass counter with a grille divides the room in two, and behind it, floor to ceiling, climb pigeonholes whose brass numbers do not run in order; a chained lectern beside the counter holds a book as large as a door. A green iron machine by the entrance wears a small sign, and above the counter a clock tells the wrong time with great confidence. A door in the east wall has two cards pinned to it. The air smells of ink, hot wax and old tea, and the only sound is a pen.";
        Safe = true;
        Light = LightLit;
        Exits["out"] = "/domains/greenvale/oakenfell/founders/city_hall";
        Exits["east"] = "/domains/greenvale/oakenfell/founders/chancery_court";
        LockExit("east", "jurors badge", "The east door is shut. The Registrar says, without looking up, 'Jurors are sworn first. It is not a complicated rule. It is merely a rule.'");

        AddDetail("ticket machine,machine,crank,slot,ticket,window,iron machine,green machine", "A green iron machine with a crank, a brass slot and a glass window. The window says NOW SERVING: 3. A plate beneath it says: THE WINDOW DOES NOT CHANGE. A sign says: TAKE A NUMBER.");
        AddDetail("benches,bench,seats,seat", "Eleven benches, bolted down facing eleven different walls. They are empty. A faded paper sign says TAKE A SEAT, and someone has added in another hand: NOT THAT ONE. You cannot tell which one.");
        AddDetail("clock,wrong clock", "A plain wooden clock with its hands at twenty past nine. It has been at twenty past nine for as long as anyone can say. A card beneath it reads: THE CLOCK IS CORRECT. THE TIME IS WRONG.");
        AddReadable("clock card,card under clock", "THE CLOCK IS CORRECT. THE TIME IS WRONG.", "A card beneath the clock. You could {cmd}read{/} it.");
        AddDetail("counter,grille,brass counter,hatch", "A brass counter with a grille. The brass hatch for passing papers is the only part of it that is ever opened.");
        AddDetail("pigeonholes,pigeonhole,rolls,holes", "Hundreds of pigeonholes, each with a rolled paper and a brass number. The numbers do not run in order, and the order they do run in has never been explained. A neat card gives the filing rule: BOUNDARIES UNDER WEATHER. WEATHER UNDER ADJOURNMENTS.");
        AddDetail("book,lectern,book of proceedings,proceedings", "A book as large as a door, chained to a lectern. Entries in a dozen hands. You could {cmd}read{/} it.");
        AddReadable("book,lectern,book of proceedings,proceedings", "Entries in a dozen hands. The latest, dated this morning: 'Goblins, Vincent's Hollow: boundary dispute, referred.'", "A book as large as a door, chained to a lectern. You could {cmd}read{/} it.");
        AddDetail("cards,east door,door", "Two cards on the east door. The first: JURORS ONLY. The second, underneath: ALL OTHERS TAKE A NUMBER.");
        AddReadable("card,east card", "Two cards on the east door. The first: JURORS ONLY. The second, underneath: ALL OTHERS TAKE A NUMBER.", "Two cards are pinned to the east door. You could {cmd}read{/} them.");
        AddReadable("plate,brass plate,motto", "NOTHING IS DONE UNTIL IT IS ENTERED.\nBelow it, smaller: Fees are posted. The clerks are not to be hurried.", "A brass plate is screwed to the counter. You could {cmd}read{/} it.");
        AddDetail("lamps,lamp,green lamps", "Green-shaded lamps on short brass arms. They burn all day. The Chancery has a view on daylight and has not yet shared it.");

        // The Boundary Roll: found per character, only by a juror who has been sworn and has not found it (SearchModifier, ThingFound). Hint: the filing rule on the pigeonholes.
        HideClue("roll", 12, "pigeonholes,pigeonhole,rolls,holes", "In pigeonhole two hundred and twelve, between a notice of adjournment and a very old umbrella, is a long roll tied with green tape. You take it.", "umbrella,notice,notice of adjournment,adjournment", "A notice of adjournment, yellow with age: 'The matter is adjourned. The matter was always going to be adjourned.' The umbrella is only an umbrella.");

        AddSpawn("/domains/greenvale/oakenfell/founders/npc/registrar_ashcombe");
    }

    // ---- The case, for one character ----

    private int Stage(ObjectRef who) => Register.Call<int>("StageFor", who, Cls);

    private int Note(ObjectRef who, string key) => Register.Call<int>("NoteOf", who, Cls, key, 0);

    /// <summary>Only a sworn juror who has not yet found the Roll can find it; everyone else's search here finds nothing, whatever the dice.</summary>
    protected override int SearchModifier(ObjectRef searcher, bool byFeel)
        => Stage(searcher) == 3 && Note(searcher, "roll") == 0 ? base.SearchModifier(searcher, byFeel) : -40;

    protected override void ThingFound(ObjectRef who, string id)
    {
        // Only the dice can find it for anyone else (a natural 20 always succeeds): a juror who is not yet sworn is given it too, so that the record of the find is never wasted.
        if (id != "roll" || Stage(who) is < 2 or > 3 || Note(who, "roll") != 0) return;
        MoveObject(CloneObject(Obj + "boundary_roll"), who);
        Register.Call<bool>("Note", who, Cls, "roll", 1);
    }

    /// <summary>The Book of Proceedings says more to a juror whose case is set down.</summary>
    public override string? QueryReadable(string name)
    {
        var text = base.QueryReadable(name);
        if (text == null || ThisPlayer() is not { } who) return text;
        if ((name ?? "").Trim().ToLowerInvariant() is "book" or "lectern" or "book of proceedings" or "proceedings" && Stage(who) is >= 2 and <= 4)
            text += "\nBelow it, in a fresh hand: 'Pruett v. Marrowby: the matter of the goose. Set down for hearing, a jury having been found.'";
        return text;
    }

    // ---- The ticket machine ----

    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        AddAction("take", Machine);
        AddAction("get", Machine);
        AddAction("turn", Machine);
        AddAction("pull", Machine);
    }

    private bool Machine(string args)
    {
        var word = args.Trim().ToLowerInvariant();
        foreach (var article in new[] { "a ", "the ", "one " }) if (word.StartsWith(article)) { word = word.Substring(article.Length).Trim(); break; }
        var verb = QueryVerb();
        var asks = verb is "take" or "get" ? word is "number" or "ticket" or "ticket from machine" or "number from machine" or "ticket from the machine"
                 : verb == "turn" ? word is "crank" or "machine" or "handle"
                 : word is "ticket" or "crank" or "handle";
        if (!asks || ThisPlayer() is not { } who) return false;
        var carrying = AllInventory(who).Any(o => o.IsAlive && o.ProgramPath == Obj + "jury_ticket");
        if (carrying || Note(who, "ticket") == 1)
        {
            TellObject(who, "The machine has already issued you a number. It does not do refunds.\n");
            return true;
        }
        var number = nextTicket++;
        var ticket = CloneObject(Obj + "jury_ticket");
        ticket.Call("SetNumber", number);
        MoveObject(ticket, who);
        if (Stage(who) is >= 1 and <= 8) Register.Call<bool>("Note", who, Cls, "ticket", 1);
        TellObject(who, $"You turn the crank. The machine clanks, thinks about it, and drops a paper ticket into your hand: 'No. {number}.'\n");
        TellRoom(Self, $"{who.Call<string>("QueryName")} turns the crank on the ticket machine.\n", who);
        return true;
    }
}
