// @inherit /std/monster

/// <summary>Summoned by the Goblin Overseer only.</summary>
public class GoblinMinion : Monster
{
    public override void Create()
    {
        Name = "Goblin Minion";
        Ids.Add("minion");
        Ids.Add("goblin");
        ShortDesc = "a goblin minion";
        LongDesc = "A scrawny, wide-eyed goblin in a scrap of armor, clutching a notched blade and plainly more afraid of its master than of you.";
        Level = 2;
        MaxHp = 12;
        DamageMin = 2;
        DamageMax = 4;
        Armor = 1;
        XpValue = 15;
    }
}
