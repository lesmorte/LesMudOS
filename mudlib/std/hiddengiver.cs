// @inherit /std/trainer

/// <summary>
/// The base of the person who gives a hidden class (docs/specs/hidden-classes.md section 7): the Registrar of the Chancery is one. It starts
/// the quest when a summoned character arrives, keeps the offer at the end, takes the two-step answer, pays the same reward whatever is
/// answered, and afterwards teaches the class's ranks to its holders only. A content file sets the class id and the lines; the class
/// itself (the numbers, the summons, the gate) is in /classes.
///
///   pledge    the first time says the permanence line; the second within a minute takes the class (ApplyClass), pays and closes the quest.
///   decline   the first time warns that it is closed for good; the second within a minute refuses it for ever, pays and closes the quest.
///   Both are bound for everyone and answer "What?" (return false) unless the register says the offer is open for this character.
///
/// Nothing is recorded in the Chronicle but the routine event class.hidden (counted, never written, no rumour, no Renown) for a taker.
/// Wizards are never given anything: they cannot be summoned, so the register never lets them in.
/// </summary>
public class HiddenGiver : Trainer
{
    /// <summary>The hidden class this person gives (its id).</summary>
    protected string HiddenId = "";
    /// <summary>The quest's journal title; the reward the journal shows for each answer.</summary>
    protected string QuestTitle = "";
    protected string RewardTakenText = "";
    protected string RewardDeclinedText = "";
    protected string RewardReleasedText = "";
    /// <summary>Paid for any answer: gold and experience.</summary>
    protected int PayGold = 1;
    protected int PayXp = 250;
    /// <summary>Keepsakes: a taker's and a decliner's (paths; "" for none).</summary>
    protected string KeepsakeTaken = "";
    protected string KeepsakeDeclined = "";
    /// <summary>What the giver says (spoken as the giver; {name} is the character).</summary>
    protected string OfferLine = "";
    protected string PermanenceLine = "";
    protected string DeclineWarning = "";
    protected string AcceptedLine = "";
    protected string DeclinedLine = "";
    protected string ReleaseLine = "";
    protected string PledgeLateLine = "";

    /// <summary>The seconds in which the second word must follow the first.</summary>
    protected virtual int ConfirmSeconds => 60;

    private readonly Dictionary<string, (string Kind, long Until)> pending = new();

    private ObjectRef Register => LoadObject("/secure/hidden");

    protected override bool TeachesTo(ObjectRef who) => who.Call<bool>("QueryHasClass", HiddenId);

    /// <summary>The shop, like the teaching, is for holders of the class only; for anyone else its words are the unknown-command answer.</summary>
    protected override bool SellsTo(ObjectRef who) => who.Call<bool>("QueryHasClass", HiddenId);

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Interactive(other)) return;
        AddAction("pledge", CmdPledge);
        AddAction("decline", CmdDecline);
        CallOut(() => Welcome(other), 1); // after the arrival has been described
    }

    // ---- The quest's beginning and the offer ----

    /// <summary>A character has come in. Begin the quest for one whose summons is outstanding; repeat the offer for one it is open to.</summary>
    private void Welcome(ObjectRef who)
    {
        if (!who.IsAlive || Environment(who) != Environment() || !Interactive(who)) return;
        var stage = Register.Call<int>("StageFor", who, HiddenId);
        var outcome = Register.Call<int>("OutcomeFor", who, HiddenId);
        if (outcome != 0 || stage == 0 || stage >= 9) return;
        // Another advanced class was taken while the summons was outstanding: released.
        var other = who.Call<string>("QueryClassOfTier", "advanced") ?? "";
        if (other != "" && other != HiddenId) { Release(who); return; }
        if (stage == 1)
        {
            if (!Register.Call<bool>("Advance", who, HiddenId, 2)) return;
            who.Call("SetQuest", QuestId, 1, QuestTitle);
            Begin(who);
        }
        else if (stage == 5) Speak(who, OfferLine.Replace("{name}", who.Call<string>("QueryName") ?? "you"));
        else Resume(who, stage);
    }

    protected string QuestId => ClassQuestId();

    /// <summary>One of the giver's lines to a character, written in their quest journal. By default spoken as "NAME says: ...", to the room; a content file overrides it to narrate (print the line as it is, to the character).</summary>
    protected virtual void Speak(ObjectRef who, string line) => QuestSay(who, QuestId, line);

    /// <summary>Just before the class is applied, once the class's own requirements are known to pass: the ceremony of taking it. Does nothing here.</summary>
    protected virtual void Ceremony(ObjectRef who) { }

    private string ClassQuestId()
    {
        try { return LoadObject("/secure/classes").Call<ObjectRef>("Class", HiddenId)?.Call<string>("QueryQuestId") ?? ""; }
        catch (MudRuntimeException) { return ""; }
    }

    /// <summary>The quest has just begun for this character: the content speaks the Registrar's first words.</summary>
    protected virtual void Begin(ObjectRef who) { }

    /// <summary>The character came back in the middle of the case: the content picks the thread up.</summary>
    protected virtual void Resume(ObjectRef who, int stage) { }

    /// <summary>Is the offer open to this character now?</summary>
    protected bool OfferOpen(ObjectRef who) => Register.Call<bool>("OfferIsOpen", who, HiddenId);

    /// <summary>The content moves the case on (stage 1 to 8) and, when the case is over, opens the offer with stage 5.</summary>
    protected bool AdvanceTo(ObjectRef who, int stage) => Register.Call<bool>("Advance", who, HiddenId, stage);

    /// <summary>One line the journal shows under the objectives of the active quest, for the character's stage (null for none).</summary>
    public override string? QueryProgressNote(string questId, ObjectRef who)
    {
        if (questId != QuestId) return null;
        return Note(who, Register.Call<int>("StageFor", who, HiddenId));
    }

    protected virtual string? Note(ObjectRef who, int stage) => null;

    // ---- The two-step answer ----

    private bool Confirm(ObjectRef who, string kind)
    {
        var name = who.Call<string>("QueryName") ?? "";
        var now = Time();
        if (pending.TryGetValue(name, out var p) && p.Kind == kind && p.Until >= now) { pending.Remove(name); return true; }
        pending[name] = (kind, now + ConfirmSeconds);
        if (pending.Count > 200) foreach (var key in pending.Where(kv => kv.Value.Until < now).Select(kv => kv.Key).ToList()) pending.Remove(key);
        return false;
    }

    private bool CmdPledge(string args)
    {
        var who = ThisPlayer();
        if (who == null || !OfferOpen(who)) return false;
        if (!Confirm(who, "pledge")) { Speak(who, PermanenceLine); return true; }
        var cls = LoadObject("/secure/classes").Call<ObjectRef>("Class", HiddenId);
        if (cls != null && cls.Call<string>("CheckRequirements", who) == null && (who.Call<string>("QueryClassOfTier", "advanced") ?? "") == "") Ceremony(who);
        var problem = who.Call<string>("ApplyClass", HiddenId); // the class file checks the level, the calling and that the offer is open
        if (problem != null) { Speak(who, problem); return true; }
        who.Call("SetQuest", QuestId, 2, QuestTitle, RewardTakenText);
        Register.Call<bool>("Answer", who, HiddenId, 1);
        Pay(who);
        Retain(who);
        Keepsake(who, KeepsakeTaken);
        try { LoadObject("/secure/events").Call<string>("Report", "class.hidden", who, new List<ObjectRef> { who }, Environment(), "", new Dictionary<string, string>()); }
        catch (MudRuntimeException) { }
        Speak(who, AcceptedLine);
        return true;
    }

    private bool CmdDecline(string args)
    {
        var who = ThisPlayer();
        if (who == null || !OfferOpen(who)) return false;
        if (!Confirm(who, "decline")) { Speak(who, DeclineWarning); return true; }
        who.Call("SetQuest", QuestId, 2, QuestTitle, RewardDeclinedText);
        Register.Call<bool>("Answer", who, HiddenId, 2);
        Pay(who);
        Retain(who);
        Keepsake(who, KeepsakeDeclined);
        Speak(who, DeclinedLine);
        return true;
    }

    private void Release(ObjectRef who)
    {
        who.Call("SetQuest", QuestId, 1, QuestTitle);
        Speak(who, ReleaseLine);
        who.Call("SetQuest", QuestId, 2, QuestTitle, RewardReleasedText);
        Register.Call<bool>("Answer", who, HiddenId, 3);
        who.Call<bool>("AddGold", PayGold);
        Retain(who);
    }

    private void Pay(ObjectRef who)
    {
        if (PayGold > 0) who.Call<bool>("AddGold", PayGold);
        if (PayXp > 0) who.Call("AddXp", PayXp);
    }

    /// <summary>The court keeps the paper.</summary>
    private void Retain(ObjectRef who)
    {
        var cls = LoadObject("/secure/classes").Call<ObjectRef>("Class", HiddenId);
        var path = cls?.Call<string>("QuerySummonsItem") ?? "";
        if (path == "") return;
        foreach (var item in AllInventory(who).ToList())
            if (item.IsAlive && item.ProgramPath == path) item.Call("Vanish");
    }

    private void Keepsake(ObjectRef who, string path)
    {
        if (path == "") return;
        try { MoveObject(CloneObject(path), who); } catch (MudRuntimeException) { }
    }
}
