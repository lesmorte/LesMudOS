// @inherit /std/monster

/// <summary>
/// Goblin Scout, level 7. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmGoblinScout : Monster
{

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Scout";
        Ids.Add("goblin");
        Ids.Add("scout");
        Ids.Add("goblin scout");
        ShortDesc = "a goblin scout";
        LongDesc = "A thin, grey-green goblin crouches in a cloak of leaves, a bent dagger in one hand and a string of bird skulls at his belt. His large ears twitch at every sound and his eyes dart from tree to tree. He is plainly counting how many of you there are, and how far it is to cover.";
        Level = 7;
        MaxVitality = 80;
        DamageMin = 5;
        DamageMax = 8;
        ToHit = 3;
        Armor = 2;
        XpValue = 270;
        GoldMin = 3;
        GoldMax = 9;
        Aggressive = true;
        BaseReaction = 2;
        AddCombatAction("stab", "Quick Stab", preparation: 0, recovery: 0.5);
        AddLoot("/domains/hollow/obj/iron_dagger", 20);
        AddLoot("/domains/hollow/obj/healing_potion", 4);
    }
}
