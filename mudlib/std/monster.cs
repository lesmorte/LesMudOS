// @inherit /std/npc

/// <summary>
/// A hostile creature. Set Level, MaxHp, DamageMin/Max, ToHit, Armor, XpValue,
/// GoldMin/GoldMax and AddLoot() in Create(). On death the killer receives the
/// experience and gold, loot falls to the floor, and the room's reset brings a
/// new one.
/// </summary>
public class Monster : Npc
{
    /// <summary>Attacks any player who enters, after a one-round pause.</summary>
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
        if (Aggressive && Interactive(other)) CallOut(() => Ambush(other), 2);
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
        StartFight(attacker);
    }

    public override string Stat() => base.Stat() + $"  Monster: aggressive {Aggressive}, assist {Assist}, xp {XpValue}, gold {GoldMin}-{GoldMax}, loot {lootPaths.Count}\n";

    public override void Die(ObjectRef? killer)
    {
        if (Dead) return;
        Dead = true;
        var room = Environment();
        var label = Cap(QueryLabel());
        if (room != null) TellRoom(room, (DeathMessage == "" ? $"{label} dies." : DeathMessage) + "\n");

        if (killer != null && killer.IsAlive)
        {
            var gold = GoldMax > 0 ? GoldMin + Random(Math.Max(1, GoldMax - GoldMin + 1)) : 0;
            if (gold > 0)
            {
                killer.Call("AddGold", gold);
                TellObject(killer, $"You find {gold} gold on the body.\n");
            }
            if (XpValue > 0) killer.Call("AddXp", XpValue);
            killer.Call("NotifyKill", Self.ProgramPath);
        }

        if (room != null)
        {
            for (var i = 0; i < lootPaths.Count; i++)
            {
                if (Random(100) >= lootPercents[i]) continue;
                try
                {
                    var loot = CloneObject(lootPaths[i]);
                    MoveObject(loot, room);
                    TellRoom(room, $"{Cap(loot.Call<string>("Short") ?? "Something")} falls to the floor.\n");
                }
                catch (MudRuntimeException)
                {
                    // A broken loot file must not keep the monster alive.
                }
            }
        }
        Destruct();
    }
}
