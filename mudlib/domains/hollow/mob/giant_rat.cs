// @inherit /std/monster

/// <summary>The first dungeon enemy: a larger, meaner cousin of the large rat, found in the Collapsed Passage.</summary>
public class GiantRat : Monster
{
    public override void Create()
    {
        Name = "rat";
        Ids.Add("rat");
        Ids.Add("giant rat");
        ShortDesc = "a giant rat";
        LongDesc = "A rat as long as your forearm, with a pointed grey snout, bald pink tail and teeth worn yellow from gnawing on rubble. It shuffles toward you without any fear at all.";
        Level = 1;
        MaxHp = 8;
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
