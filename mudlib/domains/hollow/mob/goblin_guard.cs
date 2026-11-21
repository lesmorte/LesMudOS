// @inherit /std/monster

/// <summary>A stout goblin who holds the barricade at the Guard Post and does not give ground.</summary>
public class GoblinGuard : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system (none), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Guard";
        Ids.Add("goblin");
        Ids.Add("guard");
        ShortDesc = "a goblin guard";
        LongDesc = "A broad, scarred goblin in a dented iron breastplate, with a spiked shield strapped to one arm and a short-handled hammer in the other hand. It plants its feet behind the barricade and glares at you as if you were the one who had to get past it.";
        BaseReaction = 3.5;
        AddCombatAction("jab", "Spear Jab", preparation: 0, recovery: 1.0);
        Level = 2;
        MaxVitality = 26;
        DamageMin = 2;
        DamageMax = 4;
        ToHit = 0;
        Armor = 2;
        XpValue = 32;
        GoldMin = 4;
        GoldMax = 12;
        Aggressive = true;
    }
}
