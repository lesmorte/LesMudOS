// @inherit /std/monster

/// <summary>
/// Goblin Tunneler, level 8. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmGoblinTunneler : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "pick", "pick", "overhead" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Tunneler";
        Ids.Add("goblin");
        Ids.Add("tunneler");
        Ids.Add("goblin tunneler");
        ShortDesc = "a goblin tunneler";
        LongDesc = "A squat, soot-black goblin in a miner's leather cap wields a short-hafted pick, his huge hands scarred by years of digging. He smells of damp earth and lamp-oil. He grips the pick like someone who has already decided to keep this tunnel.";
        Level = 8;
        MaxHp = 60;
        DamageMin = 5;
        DamageMax = 8;
        ToHit = 3;
        Armor = 3;
        XpValue = 240;
        GoldMin = 4;
        GoldMax = 10;
        Aggressive = true;
        BaseReaction = 3;
        AddCombatAction("pick", "Pick Strike", preparation: 0, recovery: 1);
        AddCombatAction("overhead", "Overhead Swing", preparation: 1, recovery: 1.5, windUp: "swings the pick up over his shoulder", damageMultiplier: 2, strike: "drives the pick down like a spike!");
        AddLoot("/domains/hollow/obj/iron_dagger", 15);
        AddLoot("/domains/hollow/obj/healing_potion", 4);
        AddLoot("/domains/hollow/obj/trail_ration", 20);
    }

    /// <summary>The once-only triggers first (by HP), then the opening actions, then the cycle, round and round.</summary>
    protected override CombatAction ChooseAction(ObjectRef target)
    {
        var n = step++;
        chosen = n < opening.Length ? opening[n] : cycle[(n - opening.Length) % cycle.Length];
        return FindAction(chosen)!;
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
