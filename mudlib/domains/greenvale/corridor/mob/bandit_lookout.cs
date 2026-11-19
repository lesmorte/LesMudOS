// @inherit /std/monster

/// <summary>
/// Bandit Lookout, level 6. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmBanditLookout : Monster
{

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Bandit Lookout";
        Ids.Add("bandit");
        Ids.Add("lookout");
        Ids.Add("bandit lookout");
        ShortDesc = "a bandit lookout";
        LongDesc = "A wiry man in a patched green cloak, a short sword at his hip and a horn on his belt, watches the road from the brush with the patience of someone paid to count travelers. He is quick to decide that you are worth the trouble. His dirt-stained fingers rest easily on the sword hilt.";
        Level = 6;
        MaxHp = 48;
        DamageMin = 10;
        DamageMax = 15;
        ToHit = 2;
        Armor = 2;
        XpValue = 145;
        GoldMin = 3;
        GoldMax = 8;
        Aggressive = true;
        BaseReaction = 3;
        AddCombatAction("slash", "Slash", preparation: 0, recovery: 1);
        AddLoot("/domains/hollow/obj/iron_dagger", 20);
        AddLoot("/domains/hollow/obj/trail_ration", 25);
    }
}
