// @inherit /std/monster

/// <summary>The simplest monster: just numbers in Create().</summary>
public class LargeRat : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system ("rat"), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "rat";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "rat";

    public override void Create()
    {
        Name = "Large Rat";
        Ids.Add("rat");
        ShortDesc = "a large rat";
        Tags.Add("beast"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("bite");
        LongDesc = "A rat the size of a small cat, with matted fur, yellow teeth and a long scabby tail. It watches you with red, hungry eyes.";
        BaseReaction = 2.0;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
        Level = 1;
        MaxVitality = 12;
        DamageMin = 1;
        DamageMax = 2;
        XpValue = 8;
        Aggressive = true;
    }
}
