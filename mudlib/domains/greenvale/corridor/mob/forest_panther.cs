// @inherit /std/monster

/// <summary>
/// Forest Panther, level 10. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmForestPanther : Monster
{
    private readonly string[] opening = { "pounce" };
    private readonly string[] cycle = { "claw", "claw", "rake", "claw" };
    private int step;
    private string chosen = "";
    private readonly string[] fxAction = { "rake" };
    private readonly string[] fxKind = { "weaken" };
    private readonly double[] fxValue = { 0 };
    private readonly string[] fxMods = { "armor=-2" };
    private readonly int[] fxSeconds = { 12 };

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "panther";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "panther";

    public override void Create()
    {
        Name = "Forest Panther";
        Ids.Add("panther");
        Ids.Add("forest panther");
        Ids.Add("cat");
        ShortDesc = "a forest panther";
        LongDesc = "A black panther, sleek and silent, lies along a ledge above the path with its tail twitching slowly. Its golden eyes do not blink and its shoulders flex with the promise of a spring. It looks like a piece of the night that has decided to hunt.";
        Level = 10;
        MaxHp = 120;
        DamageMin = 6;
        DamageMax = 10;
        ToHit = 5;
        Armor = 3;
        Dodge = 15;
        XpValue = 1550;
        Aggressive = true;
        BaseReaction = 2;
        AddCombatAction("claw", "Claw", preparation: 0, recovery: 1);
        AddCombatAction("pounce", "Pouncing Leap", preparation: 0.5, recovery: 1, windUp: "gathers itself on the ledge", damageMultiplier: 2, strike: "launches itself from the ledge in a pouncing leap!");
        FindAction("pounce")!.ToHitBonus = 4;
        AddCombatAction("rake", "Raking Frenzy", preparation: 0, recovery: 1);
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
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-forest_panther", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-forest_panther", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "rake") return "The panther's claws rake across your armor, tearing it from your guard. You feel exposed!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
