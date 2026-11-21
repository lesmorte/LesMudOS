// @inherit /std/monster

/// <summary>
/// Forest Stalker, level 9. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmForestStalker : Monster
{
    private readonly string[] opening = { "ambush" };
    private readonly string[] cycle = { "claw" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Forest Stalker";
        Ids.Add("stalker");
        Ids.Add("forest stalker");
        ShortDesc = "a forest stalker";
        LongDesc = "A lean, long-limbed thing the colour of lichen and old bark crouches motionless on a fallen trunk, so well matched to the wood that only the faint movement of its breath betrays it. It has the gait of a hunting cat and the eyes of nothing you can name. Its claws flex slowly, like hooked twigs.";
        Level = 9;
        MaxVitality = 70;
        DamageMin = 4;
        DamageMax = 7;
        ToHit = 4;
        Armor = 2;
        Dodge = 15;
        XpValue = 550;
        Aggressive = true;
        BaseReaction = 2;
        AddCombatAction("claw", "Claw", preparation: 0, recovery: 1);
        AddCombatAction("ambush", "Ambush Strike", preparation: 0.5, recovery: 1, windUp: "uncoils from the bark without a sound", damageMultiplier: 2, strike: "springs from cover with its claws outstretched!");
        FindAction("ambush")!.ToHitBonus = 4;
    }

    /// <summary>The once-only triggers first (by Vitality), then the opening actions, then the cycle, round and round.</summary>
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
