// @inherit /std/monster

/// <summary>A huge, slow-looking toad that lurks in the flooded tunnel and takes a great deal of punishment.</summary>
public class GiantCaveToad : Monster
{
    public override void Create()
    {
        Name = "toad";
        Ids.Add("toad");
        Ids.Add("cave toad");
        Ids.Add("giant cave toad");
        ShortDesc = "a giant cave toad";
        LongDesc = "A toad the size of a sheep, its warty back slick with black water and its throat pulsing slowly. Its pale eyes do not blink, and it waits in the shallows with a patience that is much more unsettling than speed would be.";
        Level = 3;
        MaxHp = 30;
        DamageMin = 2;
        DamageMax = 5;
        ToHit = 0;
        Armor = 0;
        XpValue = 50;
        GoldMin = 0;
        GoldMax = 0;
        Aggressive = true;
    }
}
