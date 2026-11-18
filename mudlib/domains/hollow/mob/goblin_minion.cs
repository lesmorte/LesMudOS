// @inherit /std/monster

/// <summary>Summoned by the Goblin Overseer only.</summary>
public class GoblinMinion : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system (none), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Minion";
        Ids.Add("minion");
        Ids.Add("goblin");
        ShortDesc = "a goblin minion";
        LongDesc = "A scrawny, wide-eyed goblin in a scrap of armor, clutching a notched blade and plainly more afraid of its master than of you.";
        BaseReaction = 2.5;
        AddCombatAction("stab", "Stab", preparation: 0, recovery: 0.5);
        Level = 2;
        MaxHp = 20;
        DamageMin = 2;
        DamageMax = 4;
        Armor = 1;
        XpValue = 15;
    }
}
