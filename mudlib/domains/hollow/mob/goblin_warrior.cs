// @inherit /std/monster

/// <summary>A seasoned goblin fighter who keeps the barracks in order and fights with the discipline of an organized army.</summary>
public class GoblinWarrior : Monster
{
    public override void Create()
    {
        Name = "Goblin Warrior";
        Ids.Add("goblin");
        Ids.Add("warrior");
        ShortDesc = "a goblin warrior";
        LongDesc = "A lean goblin with a chipped spear, a patched leather jerkin and a crow's feather tied into its hair. It has the look of a soldier rather than a scavenger, and it steps to one side so that its friends can reach you too.";
        Level = 3;
        MaxHp = 16;
        DamageMin = 2;
        DamageMax = 4;
        ToHit = 0;
        Armor = 2;
        XpValue = 42;
        GoldMin = 5;
        GoldMax = 14;
        Aggressive = true;
    }
}
