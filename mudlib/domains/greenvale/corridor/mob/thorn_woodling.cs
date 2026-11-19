// @inherit /std/monster

/// <summary>
/// Thorn Woodling, level 9. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmThornWoodling : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "lash", "lash", "thorns" };
    private int step;
    private string chosen = "";
    private readonly string[] fxAction = { "thorns" };
    private readonly string[] fxKind = { "weaken" };
    private readonly double[] fxValue = { 0 };
    private readonly string[] fxMods = { "armor=-2" };
    private readonly int[] fxSeconds = { 12 };

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Thorn Woodling";
        Ids.Add("woodling");
        Ids.Add("thorn woodling");
        Ids.Add("tree");
        ShortDesc = "a thorn woodling";
        LongDesc = "A figure of knotted wood and thorny vine, roughly the height of a man, stands among the saplings so still that you do not notice the pale green light in its hollow eyes until you are close. Its branches are hung with red berries that are not berries. Thorns as long as daggers jut from its arms.";
        Level = 9;
        MaxHp = 53;
        DamageMin = 10;
        DamageMax = 16;
        ToHit = 2;
        Armor = 4;
        XpValue = 410;
        Aggressive = false;
        BaseReaction = 4;
        AddCombatAction("lash", "Branch Lash", preparation: 0, recovery: 1.5);
        AddCombatAction("thorns", "Thorn Barrage", preparation: 0.5, recovery: 1.5, windUp: "draws its branches taut, thorns bristling", strike: "whips its barbed branches across the path!");
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
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-thorn_woodling", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-thorn_woodling", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "thorns") return "The thorns tear through your guard and lodge in your gear. You feel exposed!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
