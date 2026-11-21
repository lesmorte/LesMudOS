// @inherit /std/monster

/// <summary>
/// Root Creeper, level 8. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmRootCreeper : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "lash", "lash", "grasp" };
    private int step;
    private string chosen = "";
    private readonly string[] fxAction = { "grasp" };
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
        Name = "Root Creeper";
        Ids.Add("creeper");
        Ids.Add("root creeper");
        Ids.Add("roots");
        ShortDesc = "a root creeper";
        LongDesc = "A mass of pale roots the thickness of a man's thigh heaves itself along the tunnel floor, a tangle of fibres with a dark hollow at its centre like a mouth. Little white tendrils reach and curl toward anything warm. It leaves a trail of sticky sap.";
        Level = 8;
        MaxVitality = 58;
        DamageMin = 8;
        DamageMax = 13;
        ToHit = 2;
        Armor = 3;
        XpValue = 310;
        Aggressive = true;
        BaseReaction = 3.5;
        AddCombatAction("lash", "Root Lash", preparation: 0, recovery: 1);
        AddCombatAction("grasp", "Clinging Grasp", preparation: 0, recovery: 1.5);
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
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-root_creeper", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-root_creeper", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "grasp") return "Sticky tendrils wrap around your legs and drag at you. You are slowed!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
