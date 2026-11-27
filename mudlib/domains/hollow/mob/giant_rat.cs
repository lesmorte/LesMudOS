// @inherit /std/monster

/// <summary>The first dungeon enemy: a larger, meaner cousin of the large rat, found in the Collapsed Passage.</summary>
public class GiantRat : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system ("rat"), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "rat";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "rat";

    public override void Create()
    {
        Name = "Giant Rat";
        Ids.Add("rat");
        ShortDesc = "a giant rat";
        Tags.Add("beast"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("bite");
        LongDesc = "A rat as long as your forearm, with a pointed grey snout, bald pink tail and teeth worn yellow from gnawing on rubble. It shuffles toward you without any fear at all.";
        BaseReaction = 2.0;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
        Level = 1;
        MaxVitality = 15;
        DamageMin = 1;
        DamageMax = 3;
        ToHit = 0;
        Armor = 0;
        XpValue = 12;
        GoldMin = 0;
        GoldMax = 0;
        Aggressive = true;
    }
}
