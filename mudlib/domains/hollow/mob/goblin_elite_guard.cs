// @inherit /std/monster

/// <summary>The Overseer's chosen bodyguards: big, well-armoured goblins who guard the trophy hall as a mini-boss.</summary>
public class GoblinEliteGuard : Monster
{
    public override void Create()
    {
        Name = "elite guard";
        Ids.Add("goblin");
        Ids.Add("elite");
        Ids.Add("elite guard");
        Ids.Add("goblin elite guard");
        ShortDesc = "a goblin elite guard";
        LongDesc = "A goblin twice the size of its kin, wearing a helm of black iron and a breastplate studded with bone. It carries a long-hafted axe in both hands and stands between you and the Overseer's door as if it has been waiting all its life for you to try.";
        Level = 4;
        MaxHp = 22;
        DamageMin = 3;
        DamageMax = 5;
        ToHit = 2;
        Armor = 3;
        XpValue = 85;
        GoldMin = 12;
        GoldMax = 22;
        Aggressive = true;
    }
}
