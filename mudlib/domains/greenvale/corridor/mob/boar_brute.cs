// @inherit /std/monster

/// <summary>
/// Boar Brute, level 7. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmBoarBrute : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "gore", "gore", "trample" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "boar";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "boar";

    public override void Create()
    {
        Name = "Boar Brute";
        Ids.Add("boar");
        Ids.Add("brute");
        Ids.Add("boar brute");
        ShortDesc = "a boar brute";
        LongDesc = "A boar of monstrous size, its back scarred by old spear wounds and its tusks as long as a man's forearm, snorts in the shade. The ground shivers a little when it shifts its weight. The musk that clings to it is thick enough to taste.";
        Level = 7;
        MaxHp = 45;
        DamageMin = 8;
        DamageMax = 14;
        ToHit = 2;
        Armor = 3;
        XpValue = 180;
        Aggressive = false;
        BaseReaction = 4.5;
        AddCombatAction("gore", "Gore", preparation: 0, recovery: 1.5);
        AddCombatAction("trample", "Trample", preparation: 1, recovery: 2, windUp: "rears its great head and stamps the ground", damageMultiplier: 2, strike: "thunders down on its victim!");
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
