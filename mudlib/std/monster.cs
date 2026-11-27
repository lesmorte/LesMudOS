// @inherit /std/npc

/// <summary>
/// A hostile creature. Set Level, MaxVitality, DamageMin/Max, ToHit, Armor, XpValue,
/// GoldMin/GoldMax and AddLoot() in Create(). Set BaseReaction (1.0 fast to 6.0 slow) and, for something other than
/// a plain attack, AddCombatAction() calls. On death the killer receives the
/// experience; a corpse holding the gold and loot is left in the room, and the room's reset brings a
/// new one.
/// </summary>
public class Monster : Npc
{
    /// <summary>Attacks any player who enters, as soon as the arrival is complete: the fight begins then (the player cannot walk on, only flee), and the creature's first action waits its Reaction Time.</summary>
    protected bool Aggressive;
    /// <summary>Always notices a sneaking Scout: it never rolls (docs/specs/sneak.md). Set on the bosses.</summary>
    protected bool Boss;
    /// <summary>A mini-boss (the tag "elite", docs/specs/tags-and-saves.md): its saves against control effects are 15 points harder. Not a Boss; set one or the other.</summary>
    protected bool Elite;
    /// <summary>Keeps watch: +30 points to its chance to notice a sneaking Scout. Set on guards and sentries.</summary>
    protected bool Alert;
    /// <summary>Joins a fight when another monster in the room is attacked.</summary>
    protected bool Assist = true;
    protected int XpValue;
    protected int GoldMin;
    protected int GoldMax;
    /// <summary>The line the room sees on death.</summary>
    protected string DeathMessage = "";

    private readonly List<string> lootPaths = new();
    private readonly List<int> lootPercents = new();

    public Monster()
    {
        Peaceful = false;
    }

    public bool IsMonster() => true;

    /// <summary>Sees in the dark as in a lit room, for its own to-hit and for fighting, and as a dim room for noticing a sneaker (docs/specs/light.md). Set false in Create() for a creature that cannot (the Giant Cave Toad).</summary>
    protected bool DarkSight = true;

    public override bool QueryDarkSight() => HasTag("darksight");

    protected override string TagCarrier() => "creature";

    /// <summary>The flags appear as tags: Boss and Elite as the rank, Alert and DarkSight as senses. A creature declares them with the flags, never with Tags.Add.</summary>
    protected override void CollectTags(List<string> tags)
    {
        if (Boss) tags.Add("boss");
        if (Elite) tags.Add("elite");
        if (Alert) tags.Add("alert");
        if (DarkSight) tags.Add("darksight");
    }

    /// <summary>An item that may fall to the floor on death, with this percent chance.</summary>
    protected void AddLoot(string path, int percent)
    {
        lootPaths.Add(path);
        lootPercents.Add(percent);
    }

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Aggressive || !(Interactive(other) || other.Call<bool>("QueryLinkDead"))) return;
        // A sneaking character is asked once, at the entry: the creature may not notice them (0). -1: no standing to ask, it acts as always.
        if (other.Call<bool>("QuerySneaking") && other.Call<int>("SneakNotice", Self) == 0) return;
        CallOut(() => Ambush(other), 0); // after the arrival has finished, so the room is described first
    }

    public bool QueryBoss() => HasTag("boss");
    public bool QueryAlert() => HasTag("alert");
    public bool QueryAggressive() => Aggressive;

    /// <summary>
    /// A character has been noticed (or has made a noise) and asks this creature to attack them. Only the character's own object may ask,
    /// of an aggressive creature in the same room; no other object can set a creature on anyone.
    /// </summary>
    public void Engage()
    {
        var target = PreviousObject();
        if (target == null || !Aggressive || Dead || !(Interactive(target) || target.Call<bool>("QueryLinkDead")) || !target.IsAlive || Environment(target) != Environment()) return;
        CallOut(() => Ambush(target), 0);
    }

    private void Ambush(ObjectRef target)
    {
        if (Dead || !target.IsAlive || (QueryFightingWith(target) && target.Call<bool>("QueryFightingWith", Self)) || Environment(target) != Environment()) return; // (a creature still hunting a player who fled gets them back in the fight)
        BeginFight(target, true); // the fight starts now; the creature's first action waits its Reaction Time (DelayFirstAction)
    }

    private ObjectRef? spawnRoom;
    private string spawnKey = "";

    /// <summary>The room that cloned this creature for one of its spawns or encounters says which one ("s0", "e1"), once, while the creature is still in that room. The corpse remembers it (docs/BUILDING.md, refill).</summary>
    public void SetSpawnSlot(ObjectRef room, string key)
    {
        if (spawnRoom != null || PreviousObject() != room || Environment() != room) return;
        spawnRoom = room;
        spawnKey = key;
    }

    public ObjectRef? QuerySpawnRoom() => spawnRoom;

    public string QuerySpawnKey() => spawnKey;

    /// <summary>Called by a creature that has just started a fight in the room.</summary>
    public void JoinFight(ObjectRef attacker, ObjectRef victim)
    {
        if (!Assist || Dead || !victim.Call<bool>("IsMonster") || attacker.Call<bool>("IsMonster")) return;
        BeginFight(attacker, true); // drawn in by someone else: waits its Reaction Time before the first action
    }

    public override string Stat() => base.Stat() + $"  Monster: aggressive {Aggressive}, assist {Assist}, xp {XpValue}, gold {GoldMin}-{GoldMax}, loot {lootPaths.Count}\n";

    /// <summary>What kind of creature this is to the Chronicle ("goblin", "wolf"). Default: the file name.</summary>
    public string ChronicleKind { get; set; } = "";
    /// <summary>For a creature of rank: the event type its death reports ("boss.dungeon", "boss.world", or its own), else "".</summary>
    public string ChronicleEvent { get; set; } = "";
    /// <summary>For a creature of rank: the subject that tells it apart ("goblin-overseer"). Default: the file name.</summary>
    public string ChronicleSubject { get; set; } = "";

    // Tells the event service about this death: the actor is the player who struck the killing blow (else the one who dealt most damage),
    // and everyone who shared in it takes part. History is best-effort: a failure here must never stop a death.
    private void ReportDeath(ObjectRef actor, List<ObjectRef> participants, ObjectRef? room)
    {
        try
        {
            if (!Interactive(actor)) return;
            var file = Self.ProgramPath.Substring(Self.ProgramPath.LastIndexOf('/') + 1);
            var names = new Dictionary<string, string> { ["name"] = Cap(QueryLabel()) };
            var boss = ChronicleEvent != "";
            LoadObject("/secure/events").Call<string>("Report", boss ? ChronicleEvent : "kill.ordinary", actor, participants, room,
                boss ? (ChronicleSubject == "" ? file.Replace('_', '-') : ChronicleSubject) : (ChronicleKind == "" ? file : ChronicleKind), names);
        }
        catch (MudRuntimeException) { }
    }

    /// <summary>
    /// The creature's kind for kill counters ("goblin"): the beast-table kind it declares (its Skin, else its Blood, the names of
    /// mudlib/crafting/beasts.cs), else its ChronicleKind (by default its file name). docs/specs/skill-ranks.md, Unlocks.
    /// </summary>
    public string QueryCreatureKind()
    {
        string Prop(string name) { try { return (Self.Call<string>("get_" + name) ?? "").Trim().ToLowerInvariant(); } catch (MudRuntimeException) { return ""; } }
        var beast = Prop("Skin");
        if (beast == "") beast = Prop("Blood");
        if (beast != "") return beast;
        return ChronicleKind != "" ? ChronicleKind : Self.ProgramPath.Substring(Self.ProgramPath.LastIndexOf('/') + 1);
    }

    private static readonly string[] RankTrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private static bool RankDaemonTrusts(string path)
    {
        if (path.Contains("..") || path.Contains("//")) return false;
        foreach (var tree in RankTrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    // Tells the rank daemon who shares this death and which skill struck the killing blow. Best-effort: a failure never stops a death.
    private void CreditRankCounters(List<ObjectRef> participants, ObjectRef? killer)
    {
        try
        {
            var skill = killer != null && killer.IsAlive && participants.Contains(killer) ? killer.Call<string>("QueryStrikingSkill") ?? "" : "";
            LoadObject("/secure/ranks").Call("CreditKill", participants, killer, QueryCreatureKind(), skill);
        }
        catch (MudRuntimeException) { }
    }

    public override void Die(ObjectRef? killer)
    {
        if (Dead) return;
        // A death the game itself caused (this creature's own code, a blow or spell from a trusted tree, the driver) counts toward the
        // kill counters of docs/specs/skill-ranks.md; one a wizard's object forced by calling Die or FinishDamage does not.
        var cause = PreviousObject();
        var counted = cause == null || cause == Self || (cause.Domain == "" && RankDaemonTrusts(cause.ProgramPath));
        Dead = true;
        ResetCombat();
        var room = Environment();
        var label = Cap(QueryLabel());
        if (room != null) TellRoomLit(room, (DeathMessage == "" ? $"{label} dies." : DeathMessage) + "\n", "Something dies.\n");

        var gold = GoldMax > 0 ? GoldMin + Random(Math.Max(1, GoldMax - GoldMin + 1)) : 0;
        // Experience goes by damage dealt among the players still alive, in the room and awake (see Living, "Experience").
        // Each of them gets the kill credit; a blow from anything else is no player's, and gives no bonus.
        var eligible = EligibleDamage(killer);
        if (eligible.Count > 0)
        {
            foreach (var (who, share) in DivideExperience(XpValue, killer, eligible)) who.Call("AddXp", share);
            foreach (var (who, _) in eligible) who.Call("NotifyKill", Self.ProgramPath);
            if (counted) CreditRankCounters(eligible.Select(e => e.who).ToList(), killer);
            var actor = eligible.Any(e => e.who == killer) ? killer! : eligible.OrderByDescending(e => e.damage).First().who;
            ReportDeath(actor, eligible.Select(e => e.who).ToList(), room);
        }

        if (room != null)
        {
            ObjectRef? corpse = null;
            try
            {
                corpse = CloneObject("/std/corpse");
                MoveObject(corpse, room);
                if (gold > 0)
                {
                    var coins = CloneObject("/std/coins");
                    coins.Call("SetAmount", gold);
                    MoveObject(coins, corpse);
                }
                for (var i = 0; i < lootPaths.Count; i++)
                {
                    if (Random(100) >= lootPercents[i]) continue;
                    try { MoveObject(CloneObject(lootPaths[i]), corpse); }
                    catch (MudRuntimeException)
                    {
                        // A broken loot file must not keep the monster alive.
                    }
                }
                corpse.Call("Setup", QueryLabel(), killer ?? Self, Self);
                foreach (var (who, _) in eligible) corpse.Call("Entitle", who); // the loot lock belongs to everyone who shared in the kill
            }
            catch (MudRuntimeException)
            {
                // No corpse is better than an immortal monster.
            }
        }
        Destruct();
    }
}
