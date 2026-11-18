// @inherit /std/monster

/// <summary>
/// Woodland Spider, level 8. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmWoodlandSpider : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "bite", "bite", "bite", "venom" };
    private int step;
    private string chosen = "";
    private readonly string[] fxAction = { "venom" };
    private readonly string[] fxKind = { "slow" };
    private readonly double[] fxValue = { 1.25 };
    private readonly string[] fxMods = { "" };
    private readonly int[] fxSeconds = { 8 };

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Woodland Spider";
        Ids.Add("spider");
        Ids.Add("woodland spider");
        ShortDesc = "a woodland spider";
        LongDesc = "A dark, long-legged spider with mottled green markings clings to the underside of a branch, forelegs raised and trembling. Its body is the size of a ham and its legs could span the path. Strands of silk glisten around it and trail down into the gloom.";
        Level = 8;
        MaxHp = 91;
        DamageMin = 3;
        DamageMax = 5;
        ToHit = 3;
        Armor = 1;
        XpValue = 420;
        Aggressive = true;
        BaseReaction = 1.5;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
        AddCombatAction("venom", "Slowing Bite", preparation: 0, recovery: 0.5);
    }

    /// <summary>The once-only triggers first (by HP), then the opening actions, then the cycle, round and round.</summary>
    protected override CombatAction ChooseAction(ObjectRef target)
    {
        var n = step++;
        chosen = n < opening.Length ? opening[n] : cycle[(n - opening.Length) % cycle.Length];
        return FindAction(chosen)!;
    }

    /// <summary>What a hit with one of the special attacks does besides damage: slow, stagger or weaken.</summary>
    protected override int ModifyDamage(int damage, ObjectRef target)
    {
        for (var i = 0; i < fxAction.Length; i++)
        {
            if (chosen != fxAction[i]) continue;
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-woodland_spider", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-woodland_spider", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "venom") return "The spider's bite burns like ice and your limbs go heavy. You are slowed!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
