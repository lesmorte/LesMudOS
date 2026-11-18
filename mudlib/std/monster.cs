// @inherit /std/npc

/// <summary>
/// A hostile creature. Set Level, MaxHp, DamageMin/Max, ToHit, Armor, XpValue,
/// GoldMin/GoldMax and AddLoot() in Create(). Set BaseReaction (1.0 fast to 6.0 slow) and, for something other than
/// a plain attack, AddCombatAction() calls. On death the killer receives the
/// experience; a corpse holding the gold and loot is left in the room, and the room's reset brings a
/// new one.
/// </summary>
public class Monster : Npc
{
    /// <summary>Attacks any player who enters, after its Reaction Time x 2 seconds (the player may strike first or leave).</summary>
    protected bool Aggressive;
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

    /// <summary>An item that may fall to the floor on death, with this percent chance.</summary>
    protected void AddLoot(string path, int percent)
    {
        lootPaths.Add(path);
        lootPercents.Add(percent);
    }

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (Aggressive && Interactive(other)) CallOut(() => Ambush(other), QueryReaction() * 2);
    }

    private void Ambush(ObjectRef target)
    {
        if (Dead || !target.IsAlive || QueryFightingWith(target) || Environment(target) != Environment()) return;
        StartFight(target);
    }

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

    // Tells the event service about this death. History is best-effort: a failure here must never stop a death.
    private void ReportDeath(ObjectRef killer, ObjectRef? room)
    {
        try
        {
            if (!Interactive(killer)) return;
            var file = Self.ProgramPath.Substring(Self.ProgramPath.LastIndexOf('/') + 1);
            var names = new Dictionary<string, string> { ["name"] = Cap(QueryLabel()) };
            var boss = ChronicleEvent != "";
            LoadObject("/secure/events").Call<string>("Report", boss ? ChronicleEvent : "kill.ordinary", killer, new List<ObjectRef> { killer }, room,
                boss ? (ChronicleSubject == "" ? file.Replace('_', '-') : ChronicleSubject) : (ChronicleKind == "" ? file : ChronicleKind), names);
        }
        catch (MudRuntimeException) { }
    }

    public override void Die(ObjectRef? killer)
    {
        if (Dead) return;
        Dead = true;
        ResetCombat();
        var room = Environment();
        var label = Cap(QueryLabel());
        if (room != null) TellRoom(room, (DeathMessage == "" ? $"{label} dies." : DeathMessage) + "\n");

        var gold = GoldMax > 0 ? GoldMin + Random(Math.Max(1, GoldMax - GoldMin + 1)) : 0;
        if (killer != null && killer.IsAlive)
        {
            if (XpValue > 0) killer.Call("AddXp", XpValue);
            killer.Call("NotifyKill", Self.ProgramPath);
            ReportDeath(killer, room);
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
            }
            catch (MudRuntimeException)
            {
                // No corpse is better than an immortal monster.
            }
        }
        Destruct();
    }
}
