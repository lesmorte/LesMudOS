// @inherit /std/monster

/// <summary>A quick, fragile goblin that picks through the smugglers' storeroom and attacks anything it thinks it can rob.</summary>
public class GoblinScavenger : Monster
{
    public override void Create()
    {
        Name = "scavenger";
        Ids.Add("goblin");
        Ids.Add("scavenger");
        Ids.Add("goblin scavenger");
        ShortDesc = "a goblin scavenger";
        LongDesc = "A thin, quick goblin with a sack slung over one shoulder and a bent knife in each hand. Its eyes dart to your purse and then to the nearest exit, as if it is weighing which it would rather have.";
        Level = 2;
        MaxHp = 10;
        DamageMin = 2;
        DamageMax = 3;
        ToHit = 2;
        Armor = 1;
        XpValue = 22;
        GoldMin = 2;
        GoldMax = 8;
        Aggressive = true;
    }
}
