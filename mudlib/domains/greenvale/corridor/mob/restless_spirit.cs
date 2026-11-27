// @inherit /std/monster

/// <summary>
/// Restless Spirit, level 9. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmRestlessSpirit : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "touch", "touch", "touch", "chill" };
    private int step;
    private string chosen = "";
    private readonly string[] fxAction = { "chill" };
    private readonly string[] fxKind = { "slow" };
    private readonly double[] fxValue = { 1.2 };
    private readonly string[] fxMods = { "" };
    private readonly int[] fxSeconds = { 8 };

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Restless Spirit";
        Ids.Add("spirit");
        Ids.Add("restless spirit");
        Ids.Add("ghost");
        ShortDesc = "a restless spirit";
        Tags.Add("spirit"); Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("touch");
        LongDesc = "A pale, ragged figure hangs in the air above the stones, its outline blurred as though seen through water. Its face is too indistinct to read, but the hollows where its eyes should be follow you. The air near it is cold enough to numb your fingers.";
        Level = 9;
        MaxVitality = 64;
        DamageMin = 8;
        DamageMax = 13;
        ToHit = 3;
        Armor = 0;
        Dodge = 15;
        XpValue = 520;
        Aggressive = true;
        DeathMessage = "The restless spirit lets out a long sigh and unravels into the dark.";
        BaseReaction = 2.5;
        AddCombatAction("touch", "Grave Touch", preparation: 0, recovery: 1);
        AddCombatAction("chill", "Chill of the Grave", preparation: 0, recovery: 1);
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
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-restless_spirit", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-restless_spirit", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "chill") return "A deathly chill seeps into your bones. Your movements slow!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
