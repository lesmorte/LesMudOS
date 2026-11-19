// @inherit /std/monster

/// <summary>
/// Goblin Warrior, level 8. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmHeartwoodGoblin : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "slash", "slash", "slash", "heavy" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Warrior";
        Ids.Add("goblin");
        Ids.Add("warrior");
        Ids.Add("goblin warrior");
        ShortDesc = "a goblin warrior";
        LongDesc = "A goblin in a rusted breastplate bound with bark twine, a spiked club in his fist and a dented round shield on his arm, keeps watch over the old way. The moss on his armor says he has stood in one place for days. His eyes are fixed on you and he is not afraid.";
        Level = 8;
        MaxHp = 56;
        DamageMin = 5;
        DamageMax = 8;
        ToHit = 3;
        Armor = 4;
        XpValue = 230;
        GoldMin = 5;
        GoldMax = 12;
        Aggressive = true;
        BaseReaction = 3;
        AddCombatAction("slash", "Slash", preparation: 0, recovery: 1);
        AddCombatAction("heavy", "Heavy Swing", preparation: 0.5, recovery: 1.5, windUp: "hauls the club back over his shoulder", damageMultiplier: 2, strike: "brings the club down with all his weight!");
        AddLoot("/domains/hollow/obj/training_spear", 15);
        AddLoot("/domains/hollow/obj/iron_dagger", 15);
        AddLoot("/domains/hollow/obj/healing_potion", 4);
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
