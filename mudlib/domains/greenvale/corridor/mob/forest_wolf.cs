// @inherit /std/monster

/// <summary>
/// Forest Wolf, level 6. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmForestWolf : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "bite", "bite", "bite", "savage" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "wolf";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "wolf";

    public override void Create()
    {
        Name = "Forest Wolf";
        Ids.Add("wolf");
        Ids.Add("forest wolf");
        ShortDesc = "a forest wolf";
        LongDesc = "A heavy-shouldered wolf with a coat the colour of wet bark and a pale ruff at its throat pads between the trunks. It moves with the unhurried confidence of an animal that knows these woods better than anyone. Its low growl rises and falls like breathing.";
        Level = 6;
        MaxVitality = 73;
        DamageMin = 3;
        DamageMax = 6;
        ToHit = 2;
        Armor = 1;
        XpValue = 185;
        Aggressive = false;
        BaseReaction = 2;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
        AddCombatAction("savage", "Savage Bite", preparation: 0, recovery: 0.5, damageMultiplier: 2);
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
