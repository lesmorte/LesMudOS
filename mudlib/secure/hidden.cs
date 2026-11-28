/// <summary>
/// The register of hidden classes (docs/specs/hidden-classes.md section 3): who has been summoned for which hidden class, how
/// far each has got, and the pity counter of the roll that serves the summons.
///
/// A hidden class is an ordinary advanced class with Hidden = true that nobody offers: a character finds it by a summons that turns up
/// once in the loot of an ordinary creature, takes it to a gated door, does a short quest there and is offered the class at the end. This
/// daemon owns every number and every record, writes the file BEFORE it returns, and refuses (and records nothing) if the write fails.
///
/// What it keeps, per class id and lower-case character name, as flat numbers (key "class/name/field"):
///   looted   eligible loots counted so far
///   issued   0, or the time the summons was served; serial: its number in the order served
///   stage    0 nothing; 1 summoned; 2 admitted; 3 sworn; 4 the case heard; 5 the offer is open; 9 answered
///   outcome  0 none, 1 taken, 2 declined, 3 withdrawn
///   n.KEY    the class's own small integers (the case's choices; at most 16 keys of at most 24 characters)
///
/// Rules, all enforced here and not in the object that asks:
///  - The caller is read from PreviousObject() and must be code of the real game (a trusted tree, never a sandbox object); no function here
///    answers or changes anything while restricted code is anywhere on the call stack, so a wizard's object can neither grant, read nor
///    detect a hidden class or a summons on a character. Nothing in this file begins with Query/Is/Has/Can for that reason: the master
///    refuses restricted callers every other name.
///  - The roll belongs to a corpse (a clone of /std/corpse of a real creature, in a real room) and to the character whose command is running,
///    in the corpse's room, awake. A wizard of any level is answered "no" and recorded nowhere.
///  - The steps (Advance, Note, Answer) belong to the class's own files (its OwnPaths) and only for a character in the same room.
///
/// Blueprint only. Save file: save/secure/hidden.json.
/// </summary>
public class HiddenRegister : MudObject
{
    private const string SaveFile = "/secure/hidden";
    private const string WizListFile = "/secure/wizlist";
    private const string PlayerBody = "/std/player";
    private const string CorpsePath = "/std/corpse";

    /// <summary>The highest level of an ordinary creature that is not a boss. A test reads the creature files and fails if the world gains a higher one.</summary>
    public const int TopOrdinaryLevel = 9;
    /// <summary>The eligible creature window: from min(level - 1, TopOrdinaryLevel) up to level + WindowAbove.</summary>
    public const int WindowAbove = 2;
    public const int Taken = 1, Declined = 2, Withdrawn = 3;
    public const int Answered = 9;

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    [Saved] private Dictionary<string, long> records = new();
    [Saved] private Dictionary<string, int> serials = new();

    private int fixedRoll = -1;
    private readonly List<string> refusals = new();

    public override void Create()
    {
        if (!Self.IsClone) RestoreObject(SaveFile);
    }

    // ------------------------------------------------------------------ who may ask

    private static bool IsTrusted(ObjectRef? ob)
    {
        if (ob == null || !ob.IsAlive || ob.Domain != "") return false;
        var path = ob.ProgramPath;
        if (path.Contains("..") || path.Contains("//")) return false;
        foreach (var tree in TrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>The caller, or null when this call must be refused: a clone of the register, a sandbox object, restricted code anywhere on the stack.</summary>
    private ObjectRef? Caller()
    {
        var caller = PreviousObject();
        if (Self.IsClone || RestrictedCaller() != null || !IsTrusted(caller)) return null;
        return caller;
    }

    private void Refuse(string why)
    {
        refusals.Add(Time() + " " + why);
        while (refusals.Count > 50) refusals.RemoveAt(0);
    }

    private static bool IsPlayerBody(ObjectRef? ob)
        => ob != null && ob.IsAlive && ob.IsClone && ob.ProgramPath == PlayerBody && ob.Domain == "" && !string.IsNullOrEmpty(ob.Call<string>("QueryName"));

    private string NameOf(ObjectRef who) => (who.Call<string>("QueryName") ?? "").ToLowerInvariant();

    private bool IsWizard(string name) => LoadObject(WizListFile).Call<int>("QueryLevel", name) > 0;

    private static string Key(string classId, string name, string field) => classId + "/" + name + "/" + field;

    private long Get(string classId, string name, string field) => records.GetValueOrDefault(Key(classId, name, field));

    private ObjectRef? ClassFile(string id)
    {
        try { return LoadObject("/secure/classes").Call<ObjectRef>("Class", id); }
        catch (MudRuntimeException) { return null; }
    }

    private List<string> HiddenClassIds()
    {
        var result = new List<string>();
        foreach (var id in LoadObject("/secure/classes").Call<List<string>>("ClassIds") ?? new List<string>())
            if (ClassFile(id)?.Call<bool>("QueryHidden") == true) result.Add(id);
        return result;
    }

    private ObjectRef? HiddenClass(string id)
    {
        var cls = ClassFile(id);
        return cls != null && cls.Call<bool>("QueryHidden") ? cls : null;
    }

    /// <summary>The class that owns a gate key, or null.</summary>
    private (string id, ObjectRef cls)? ClassOfGate(string gateKey)
    {
        if (string.IsNullOrEmpty(gateKey)) return null;
        foreach (var id in HiddenClassIds())
            if (ClassFile(id) is { } cls && cls.Call<string>("QueryGateKey") == gateKey) return (id, cls);
        return null;
    }

    private bool InOwnPaths(ObjectRef caller, ObjectRef cls)
    {
        foreach (var prefix in cls.Call<List<string>>("QueryOwnPaths") ?? new List<string>())
            if (prefix != "" && caller.ProgramPath.StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>Write the records; on failure put the old values back and say so.</summary>
    private bool Commit(Dictionary<string, long?> undo)
    {
        try { SaveObject(SaveFile); return true; }
        catch (MudRuntimeException)
        {
            foreach (var (k, v) in undo) { if (v == null) records.Remove(k); else records[k] = v.Value; }
            Refuse("the register could not be saved");
            return false;
        }
    }

    private void Set(string key, long value, Dictionary<string, long?> undo)
    {
        if (!undo.ContainsKey(key)) undo[key] = records.TryGetValue(key, out var old) ? old : null;
        records[key] = value;
    }

    // ------------------------------------------------------------------ the roll

    /// <summary>
    /// The chance, in tenths of a percent (0 to 1000), of the n-th eligible loot (counting from 1): nothing up to the floor, then
    /// base percent plus step for each further one, and certain on the guarantee-th. Pure.
    /// </summary>
    public int ChanceTenths(int n, int floor, double basePercent, double step, int guarantee)
    {
        if (n >= guarantee) return 1000;
        if (n <= floor) return 0;
        return (int)Math.Round((basePercent + step * (n - floor - 1)) * 10);
    }

    /// <summary>
    /// Called by a corpse when a character takes something from it (the corpse has already checked the lock and that the character
    /// shared in the kill). Counts the loot, rolls, and if a summons is served records it (saved before this returns) and answers
    /// the class id; "" otherwise. Nothing is recorded for a wizard, for a character who does not qualify, or for a refused caller.
    /// </summary>
    public string RollOnLoot(ObjectRef who, int creatureLevel, bool creatureBoss, int creatureXp, bool noSummons)
    {
        var caller = Caller();
        if (caller == null || !caller.IsClone || caller.ProgramPath != CorpsePath) { Refuse("RollOnLoot: the caller is not a corpse of the real game"); return ""; }
        var room = Environment(caller);
        if (room == null || !IsTrusted(room)) { Refuse("RollOnLoot: the corpse is not in a real room"); return ""; }
        if (!IsPlayerBody(who) || who != ThisPlayer() || !Interactive(who) || Environment(who) != room) { Refuse("RollOnLoot: the looter is not the player here"); return ""; }
        if (creatureBoss || creatureXp <= 0 || noSummons || creatureLevel <= 0) return "";
        var name = NameOf(who);
        if (IsWizard(name) || who.Call<int>("QueryWizLevel") > 0 || who.Call<bool>("QueryIdle") || who.Call<bool>("IsDead")) return "";
        var level = who.Call<int>("QueryLevel");
        foreach (var id in HiddenClassIds())
        {
            var cls = ClassFile(id)!;
            if (!cls.Call<bool>("QuerySummonsOpen")) continue;
            if (who.Call<string>("QueryArchetype") != cls.Call<string>("QueryParent")) continue;
            if (level < cls.Call<int>("QuerySummonsMinLevel")) continue;
            if (who.Call<string>("QueryClassOfTier", "advanced") != "") continue;
            if (Get(id, name, "issued") != 0) continue;
            if (creatureLevel < Math.Min(level - 1, TopOrdinaryLevel) || creatureLevel > level + WindowAbove) continue;
            var undo = new Dictionary<string, long?>();
            var n = (int)Get(id, name, "looted") + 1;
            Set(Key(id, name, "looted"), n, undo);
            var tenths = ChanceTenths(n, cls.Call<int>("QuerySummonsFloor"), cls.Call<double>("QuerySummonsBase"), cls.Call<double>("QuerySummonsStep"), cls.Call<int>("QuerySummonsGuarantee"));
            var served = tenths >= 1000 || (tenths > 0 && (fixedRoll >= 0 ? fixedRoll : Random(1000)) < tenths);
            int? oldSerial = serials.TryGetValue(id, out var was) ? was : null;
            if (served)
            {
                var serial = (oldSerial ?? 0) + 1;
                serials[id] = serial;
                Set(Key(id, name, "issued"), Math.Max(1, Time()), undo);
                Set(Key(id, name, "serial"), serial, undo);
                Set(Key(id, name, "stage"), 1, undo);
            }
            if (!Commit(undo))
            {
                if (oldSerial == null) serials.Remove(id); else serials[id] = oldSerial.Value;
                return "";
            }
            return served ? id : "";
        }
        return "";
    }

    // ------------------------------------------------------------------ the door

    /// <summary>
    /// May this character pass the gate with this key? True for a holder of the class, for a character whose summons is outstanding
    /// (stage 1 to 8, no answer), and for one who took another advanced class meanwhile (released: the giver says so inside).
    /// Called by the room that declared the gate, for the player whose command is running.
    /// </summary>
    public bool Admits(ObjectRef who, string gateKey)
    {
        var caller = Caller();
        if (caller == null || !IsPlayerBody(who)) return false;
        var owner = ClassOfGate(gateKey);
        if (owner == null) return false;
        var (id, cls) = owner.Value;
        var name = NameOf(who);
        if (who.Call<int>("QueryWizLevel") > 0 || IsWizard(name)) return false;
        if (who.Call<bool>("QueryHasClass", id)) return true;
        var stage = Get(id, name, "stage");
        return Get(id, name, "issued") != 0 && stage >= 1 && stage <= 8 && Get(id, name, "outcome") == 0;
    }

    /// <summary>One of the class's lines for the door of a gate key ("knock", "again", "open", "look"); "" if the character may not pass.</summary>
    public string GateLine(ObjectRef who, string gateKey, string which)
    {
        if (!Admits(who, gateKey)) return "";
        var owner = ClassOfGate(gateKey);
        return owner == null ? "" : owner.Value.cls.Call<string>("QueryLine", which) ?? "";
    }

    /// <summary>The stage of the character for the class that owns a gate key (0 if none or not allowed to ask).</summary>
    public int GateStage(ObjectRef who, string gateKey)
    {
        if (!Admits(who, gateKey)) return 0;
        var owner = ClassOfGate(gateKey);
        return owner == null ? 0 : (int)Get(owner.Value.id, NameOf(who), "stage");
    }

    /// <summary>
    /// From the gated door only: the record says a summons was served and the pack holds none. Clones a new one into the pack. True if given.
    /// </summary>
    public bool Reissue(ObjectRef who, string gateKey)
    {
        var caller = Caller();
        if (caller == null || !caller.Call<bool>("HasGate", gateKey) || !IsPlayerBody(who) || Environment(who) != caller || who != ThisPlayer()) return false;
        var owner = ClassOfGate(gateKey);
        if (owner == null) return false;
        var (id, cls) = owner.Value;
        var name = NameOf(who);
        var stage = Get(id, name, "stage");
        if (Get(id, name, "issued") == 0 || stage < 1 || stage > 8 || Get(id, name, "outcome") != 0 || who.Call<bool>("QueryHasClass", id)) return false;
        var path = cls.Call<string>("QuerySummonsItem") ?? "";
        if (path == "") return false;
        foreach (var item in AllInventory(who))
            if (item.IsAlive && item.ProgramPath == path) return false;
        try { MoveObject(CloneObject(path), who); return true; }
        catch (MudRuntimeException) { return false; }
    }

    /// <summary>The number printed on a summons (class base plus the register's serial), for the paper being read. 0 if not the holder's.</summary>
    public int SerialOf(ObjectRef who, string classId)
    {
        var caller = Caller();
        if (caller == null || !caller.IsClone || !caller.Call<bool>("IsSummonsPaper") || !IsPlayerBody(who)) return 0;
        var cls = HiddenClass(classId);
        if (cls == null) return 0;
        var serial = (int)Get(classId, NameOf(who), "serial");
        return serial == 0 ? 0 : cls.Call<int>("QuerySerialBase") + serial;
    }

    // ------------------------------------------------------------------ the quest's own steps

    private (string name, ObjectRef cls)? StepCaller(ObjectRef who, string classId)
    {
        var caller = Caller();
        var cls = HiddenClass(classId);
        if (caller == null || cls == null || !InOwnPaths(caller, cls) || !IsPlayerBody(who)) { Refuse($"step on {classId}: not the class's own file"); return null; }
        var room = Environment(caller) ?? caller;
        if (Environment(who) != room) { Refuse($"step on {classId}: not in the same room"); return null; }
        var name = NameOf(who);
        if (IsWizard(name) || who.Call<int>("QueryWizLevel") > 0) return null;
        return (name, cls);
    }

    /// <summary>The stage of a character for a class, for the class's own files (0 for anyone else).</summary>
    public int StageFor(ObjectRef who, string classId)
    {
        var caller = Caller();
        var cls = HiddenClass(classId);
        if (caller == null || cls == null || !InOwnPaths(caller, cls) || !IsPlayerBody(who)) return 0;
        return (int)Get(classId, NameOf(who), "stage");
    }

    /// <summary>The answer given (0 none, 1 taken, 2 declined, 3 withdrawn), for the class's own files.</summary>
    public int OutcomeFor(ObjectRef who, string classId)
    {
        var caller = Caller();
        var cls = HiddenClass(classId);
        if (caller == null || cls == null || !InOwnPaths(caller, cls) || !IsPlayerBody(who)) return 0;
        return (int)Get(classId, NameOf(who), "outcome");
    }

    /// <summary>Is the offer open (stage 5, no answer)? Read by the class's requirement. Refuses restricted code.</summary>
    public bool OfferIsOpen(ObjectRef who, string classId)
    {
        if (Self.IsClone || RestrictedCaller() != null || !IsPlayerBody(who) || HiddenClass(classId) == null) return false;
        var name = NameOf(who);
        return Get(classId, name, "issued") != 0 && Get(classId, name, "stage") == 5 && Get(classId, name, "outcome") == 0;
    }

    /// <summary>Raise the stage (never lower it; 1 to 8). True if the record now says that stage or more and it is saved.</summary>
    public bool Advance(ObjectRef who, string classId, int stage)
    {
        var step = StepCaller(who, classId);
        if (step == null || stage < 1 || stage > 8) return false;
        var name = step.Value.name;
        if (Get(classId, name, "issued") == 0 || Get(classId, name, "outcome") != 0) return false;
        if (Get(classId, name, "stage") >= stage) return true;
        var undo = new Dictionary<string, long?>();
        Set(Key(classId, name, "stage"), stage, undo);
        return Commit(undo);
    }

    /// <summary>Keep a small number for the class (the case's choices). At most 16 keys, each at most 24 characters, lower case letters.</summary>
    public bool Note(ObjectRef who, string classId, string key, int value)
    {
        var step = StepCaller(who, classId);
        if (step == null || string.IsNullOrEmpty(key) || key.Length > 24 || !key.All(c => (c >= 'a' && c <= 'z') || c == '_')) return false;
        var name = step.Value.name;
        var prefix = classId + "/" + name + "/n.";
        if (!records.ContainsKey(prefix + key) && records.Keys.Count(k => k.StartsWith(prefix, StringComparison.Ordinal)) >= 16) return false;
        var undo = new Dictionary<string, long?>();
        Set(prefix + key, value, undo);
        return Commit(undo);
    }

    /// <summary>A number kept with Note, or the fallback.</summary>
    public int NoteOf(ObjectRef who, string classId, string key, int fallback)
    {
        var caller = Caller();
        var cls = HiddenClass(classId);
        if (caller == null || cls == null || !InOwnPaths(caller, cls) || !IsPlayerBody(who)) return fallback;
        return records.TryGetValue(classId + "/" + NameOf(who) + "/n." + key, out var v) ? (int)v : fallback;
    }

    /// <summary>The last step: record the answer (1 taken, 2 declined, 3 withdrawn). Stage becomes 9. Works once.</summary>
    public bool Answer(ObjectRef who, string classId, int outcome)
    {
        var step = StepCaller(who, classId);
        if (step == null || outcome < Taken || outcome > Withdrawn) return false;
        var name = step.Value.name;
        if (Get(classId, name, "issued") == 0 || Get(classId, name, "outcome") != 0) return false;
        var undo = new Dictionary<string, long?>();
        Set(Key(classId, name, "outcome"), outcome, undo);
        Set(Key(classId, name, "stage"), Answered, undo);
        return Commit(undo);
    }

    // ------------------------------------------------------------------ the level 10 command and tests

    private bool WizardTen(out ObjectRef? wizard)
    {
        wizard = ThisPlayer();
        var caller = Caller();
        return caller != null && wizard != null && IsPlayerBody(wizard) && Interactive(wizard) && wizard.Call<int>("QueryWizLevel") >= 10 && caller.ProgramPath == PlayerBody;
    }

    /// <summary>Fix the dice (a number 0 to 999) or put them back ("random"). Only trusted code acting for a wizard of level 10.</summary>
    public bool SetTestRolls(string mode)
    {
        if (!WizardTen(out _)) return false;
        if (mode == "random") { fixedRoll = -1; return true; }
        if (mode == null || !mode.StartsWith("roll:") || !int.TryParse(mode.Substring(5), out var v) || v < 0 || v > 999) return false;
        fixedRoll = v;
        return true;
    }

    public string DiceMode() => !WizardTen(out _) ? "" : fixedRoll < 0 ? "random" : "roll:" + fixedRoll;

    /// <summary>Level 10 wizards: one line per hidden class with the counts, or the names for one class.</summary>
    public List<string> Report(string classId)
    {
        var lines = new List<string>();
        if (!WizardTen(out _)) return lines;
        if (string.IsNullOrEmpty(classId))
        {
            foreach (var id in HiddenClassIds())
            {
                var names = Names(id);
                int Count(Func<string, bool> f) => names.Count(f);
                lines.Add($"{id}: summoned {Count(n => Get(id, n, "issued") != 0)}, admitted {Count(n => Get(id, n, "stage") >= 2)}, taken {Count(n => Get(id, n, "outcome") == Taken)}, declined {Count(n => Get(id, n, "outcome") == Declined)}, withdrawn {Count(n => Get(id, n, "outcome") == Withdrawn)}, counting {Count(n => Get(id, n, "looted") > 0)}");
            }
            return lines;
        }
        if (HiddenClass(classId) == null) return lines;
        foreach (var n in Names(classId))
            lines.Add($"{n}: looted {Get(classId, n, "looted")}, issued {(Get(classId, n, "issued") != 0 ? "yes #" + Get(classId, n, "serial") : "no")}, stage {Get(classId, n, "stage")}, outcome {Get(classId, n, "outcome")}");
        return lines;
    }

    private List<string> Names(string classId)
        => records.Keys.Where(k => k.StartsWith(classId + "/", StringComparison.Ordinal)).Select(k => k.Split('/')[1]).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();

    /// <summary>Level 10 wizards: serve a summons to an online mortal (a test aid). The name goes in the refusal log, not in the Chronicle. "" if done, else why not.</summary>
    public string Grant(string who, string classId)
    {
        if (!WizardTen(out var wizard)) return "That is not possible.";
        var target = FindPlayer((who ?? "").ToLowerInvariant());
        if (target == null) return "Nobody of that name is playing.";
        var cls = HiddenClass(classId);
        if (cls == null) return "There is no such hidden class.";
        if (!IsPlayerBody(target) || !Interactive(target)) return "Only a mortal who is here and playing.";
        var name = NameOf(target);
        if (IsWizard(name) || target.Call<int>("QueryWizLevel") > 0) return "A wizard cannot be summoned.";
        if (target.Call<string>("QueryClassOfTier", "advanced") != "") return "They already have an advanced class.";
        if (Get(classId, name, "issued") != 0) return "They have been summoned already.";
        var path = cls.Call<string>("QuerySummonsItem") ?? "";
        var undo = new Dictionary<string, long?>();
        var serial = serials.GetValueOrDefault(classId) + 1;
        var oldSerial = serials.TryGetValue(classId, out var s) ? s : (int?)null;
        serials[classId] = serial;
        Set(Key(classId, name, "issued"), Math.Max(1, Time()), undo);
        Set(Key(classId, name, "serial"), serial, undo);
        Set(Key(classId, name, "stage"), 1, undo);
        if (!Commit(undo)) { if (oldSerial == null) serials.Remove(classId); else serials[classId] = oldSerial.Value; return "The register could not be saved."; }
        Refuse($"granted by {wizard!.Call<string>("QueryName")} to {name}");
        try { if (path != "") MoveObject(CloneObject(path), target); } catch (MudRuntimeException) { }
        TellObject(target, (cls.Call<string>("QueryLine", "found") ?? "").Replace("{corpse}", "the air") + "\n");
        return "";
    }

    /// <summary>Level 10 wizards: clear a character's record for a class (a test aid), unless they hold it. "" if done, else why not.</summary>
    public string Reset(string name, string classId)
    {
        if (!WizardTen(out _)) return "That is not possible.";
        if (HiddenClass(classId) == null) return "There is no such hidden class.";
        name = (name ?? "").ToLowerInvariant();
        var holder = FindPlayer(name);
        if (holder != null && holder.Call<bool>("QueryHasClass", classId)) return "They hold the class; it is not undone.";
        var prefix = classId + "/" + name + "/";
        var old = records.Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var kv in old) records.Remove(kv.Key);
        try { SaveObject(SaveFile); }
        catch (MudRuntimeException) { foreach (var kv in old) records[kv.Key] = kv.Value; return "The register could not be saved."; }
        return "";
    }

    /// <summary>Level 10 wizards: set the eligible-loot counter of a character (a test aid for the roll). "" if done, else why not.</summary>
    public string SetLooted(string name, string classId, int count)
    {
        if (!WizardTen(out _)) return "That is not possible.";
        if (HiddenClass(classId) == null) return "There is no such hidden class.";
        var undo = new Dictionary<string, long?>();
        Set(Key(classId, (name ?? "").ToLowerInvariant(), "looted"), Math.Clamp(count, 0, 1000), undo);
        return Commit(undo) ? "" : "The register could not be saved.";
    }

    /// <summary>Level 10 wizards: run the roll's formula for a class this many times with the real dice and answer the number of eligible loots each trial needed.</summary>
    public List<int> Simulate(string classId, int trials)
    {
        var result = new List<int>();
        if (!WizardTen(out _)) return result;
        var cls = HiddenClass(classId);
        if (cls == null) return result;
        int floor = cls.Call<int>("QuerySummonsFloor"), guarantee = cls.Call<int>("QuerySummonsGuarantee");
        double b = cls.Call<double>("QuerySummonsBase"), st = cls.Call<double>("QuerySummonsStep");
        for (var t = 0; t < Math.Clamp(trials, 1, 100000); t++)
        {
            var n = 1;
            while (true)
            {
                var tenths = ChanceTenths(n, floor, b, st, guarantee);
                if (tenths >= 1000 || (tenths > 0 && Random(1000) < tenths)) break;
                n++;
            }
            result.Add(n);
        }
        return result;
    }

    private ObjectRef? FindPlayer(string name)
    {
        foreach (var ob in Users()) if (IsPlayerBody(ob) && NameOf(ob) == name) return ob;
        return null;
    }

    /// <summary>The last refusals (this run only), for builders who wonder why a summons was not served. Level 10 wizards only.</summary>
    public List<string> Refusals() => WizardTen(out _) ? new List<string>(refusals) : new List<string>();
}
