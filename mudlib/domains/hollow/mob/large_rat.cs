// @inherit /std/monster

/// <summary>The simplest monster: just numbers in Create().</summary>
public class LargeRat : Monster
{
    public override void Create()
    {
        Name = "Large Rat";
        Ids.Add("rat");
        ShortDesc = "a large rat";
        LongDesc = "A rat the size of a small cat, with matted fur, yellow teeth and a long scabby tail. It watches you with red, hungry eyes.";
        BaseReaction = 2.0;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
        Level = 1;
        MaxHp = 12;
        DamageMin = 1;
        DamageMax = 2;
        XpValue = 8;
        Aggressive = true;
    }
}
