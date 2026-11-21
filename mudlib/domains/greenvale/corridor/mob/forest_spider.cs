// @inherit /std/monster

/// <summary>
/// Forest Spider, level 5. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmForestSpider : Monster
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
        Name = "Forest Spider";
        Ids.Add("spider");
        Ids.Add("forest spider");
        ShortDesc = "a forest spider";
        LongDesc = "A spider the size of a hunting dog hangs from a rope of silk between two oaks, bristling with pale hairs and eight glittering eyes. Fresh webs cross the path at head height. Its fangs drip a clear fluid that stings wherever it falls.";
        Level = 5;
        MaxVitality = 57;
        DamageMin = 4;
        DamageMax = 8;
        ToHit = 2;
        Armor = 0;
        XpValue = 135;
        Aggressive = true;
        BaseReaction = 1.5;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
        AddCombatAction("venom", "Slowing Bite", preparation: 0, recovery: 0.5);
    }

    /// <summary>The once-only triggers first (by Vitality), then the opening actions, then the cycle, round and round.</summary>
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
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-forest_spider", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-forest_spider", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "venom") return "The spider's fangs sink in and a cold, creeping numbness spreads along your limbs. You are slowed!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
