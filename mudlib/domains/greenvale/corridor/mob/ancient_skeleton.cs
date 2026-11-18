// @inherit /std/monster

/// <summary>
/// Ancient Skeleton, level 9. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmAncientSkeleton : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "blade", "blade", "blade", "crush" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Ancient Skeleton";
        Ids.Add("skeleton");
        Ids.Add("ancient skeleton");
        ShortDesc = "an ancient skeleton";
        LongDesc = "A skeleton in the remnants of bronze scale armor shambles upright, its jaw hanging a little open and a corroded blade still held in bony fingers. A faint green light pools in its eye sockets. It makes a dry clicking sound as it moves, like old shells shifting.";
        Level = 9;
        MaxHp = 54;
        DamageMin = 8;
        DamageMax = 13;
        ToHit = 3;
        Armor = 4;
        XpValue = 410;
        GoldMin = 2;
        GoldMax = 8;
        Aggressive = true;
        DeathMessage = "The ancient skeleton clatters to the floor in a heap of brittle bones.";
        BaseReaction = 3.5;
        AddCombatAction("blade", "Rusted Blade", preparation: 0, recovery: 1);
        AddCombatAction("crush", "Bone Crusher", preparation: 1, recovery: 1.5, windUp: "raises its blade high in both hands", damageMultiplier: 2, strike: "brings the blade down with bone-shattering force!");
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
