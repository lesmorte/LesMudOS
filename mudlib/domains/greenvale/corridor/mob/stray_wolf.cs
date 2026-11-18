// @inherit /std/monster

/// <summary>
/// Stray Wolf, level 5. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmStrayWolf : Monster
{

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "wolf";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "wolf";

    public override void Create()
    {
        Name = "Stray Wolf";
        Ids.Add("wolf");
        Ids.Add("stray wolf");
        ShortDesc = "a stray wolf";
        LongDesc = "A rangy grey wolf, cut off from its pack and thin with hunger, shadows the road at a distance. Its ribs show through a matted coat and its lips are drawn back from yellow teeth. Hunger has made it bold enough to bite.";
        Level = 5;
        MaxHp = 60;
        DamageMin = 6;
        DamageMax = 10;
        ToHit = 1;
        Armor = 1;
        XpValue = 140;
        Aggressive = false;
        BaseReaction = 2;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
    }
}
