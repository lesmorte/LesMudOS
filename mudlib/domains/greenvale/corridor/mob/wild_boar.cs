// @inherit /std/monster

/// <summary>
/// Wild Boar, level 6. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmWildBoar : Monster
{
    private readonly string[] opening = { "charge" };
    private readonly string[] cycle = { "gore" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "boar";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "boar";

    public override void Create()
    {
        Name = "Wild Boar";
        Ids.Add("boar");
        Ids.Add("wild boar");
        ShortDesc = "a wild boar";
        Tags.Add("beast"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("gore");
        LongDesc = "A wild boar the size of a calf roots through the leaf litter, bristling with coarse black hair, yellowed tusks curving from its lower jaw. Mud cakes its flanks and its small red eyes miss nothing. It wheels to face anything that comes too close.";
        Level = 6;
        MaxVitality = 40;
        DamageMin = 11;
        DamageMax = 19;
        ToHit = 1;
        Armor = 2;
        XpValue = 145;
        Aggressive = false;
        BaseReaction = 3.5;
        AddCombatAction("gore", "Gore", preparation: 0, recovery: 1);
        AddCombatAction("charge", "Charge", preparation: 1, recovery: 1.5, windUp: "lowers its tusks and paws at the ground", damageMultiplier: 2, strike: "crashes forward in a bone-shaking charge!", reach: "melee", damage: "blunt", tagKind: "slam");
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
