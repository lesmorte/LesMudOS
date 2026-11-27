// @inherit /std/monster

/// <summary>
/// Young Black Bear, level 8. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmYoungBlackBear : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "swipe", "swipe", "maul" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "bear";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "bear";

    public override void Create()
    {
        Name = "Young Black Bear";
        Ids.Add("bear");
        Ids.Add("young bear");
        Ids.Add("young black bear");
        ShortDesc = "a young black bear";
        Tags.Add("beast"); Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("claw");
        LongDesc = "A young black bear, all legs and shoulders, ambles between the trees with its muzzle lifted to scent the air. Its shaggy coat is still a size too large for it. Young or not, it weighs more than three men and has claws like iron hooks.";
        Level = 8;
        MaxVitality = 57;
        DamageMin = 8;
        DamageMax = 15;
        ToHit = 2;
        Armor = 2;
        XpValue = 300;
        Aggressive = false;
        BaseReaction = 4;
        AddCombatAction("swipe", "Swipe", preparation: 0, recovery: 1.5);
        AddCombatAction("maul", "Maul", preparation: 1, recovery: 2, windUp: "rears up on its hind legs, claws spread wide", damageMultiplier: 2, strike: "comes down in a crushing maul!");
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
