// @inherit /std/monster

/// <summary>A seasoned goblin fighter who keeps the barracks in order and fights with the discipline of an organized army.</summary>
public class GoblinWarrior : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system (none), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Warrior";
        Ids.Add("goblin");
        Ids.Add("warrior");
        ShortDesc = "a goblin warrior";
        LongDesc = "A lean goblin with a chipped spear, a patched leather jerkin and a crow's feather tied into its hair. It has the look of a soldier rather than a scavenger, and it steps to one side so that its friends can reach you too.";
        BaseReaction = 3.0;
        AddCombatAction("slash", "Slash", preparation: 0, recovery: 1.0);
        Level = 3;
        MaxHp = 28;
        DamageMin = 2;
        DamageMax = 4;
        ToHit = 0;
        Armor = 2;
        XpValue = 42;
        GoldMin = 5;
        GoldMax = 14;
        Aggressive = true;
        AddLoot("/domains/hollow/obj/goblin_war_spear", 6);
    }
}
