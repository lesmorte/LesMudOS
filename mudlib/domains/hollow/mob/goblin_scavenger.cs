// @inherit /std/monster

/// <summary>A quick, fragile goblin that picks through the smugglers' storeroom and attacks anything it thinks it can rob.</summary>
public class GoblinScavenger : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system (none), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Scavenger";
        Ids.Add("goblin");
        Ids.Add("scavenger");
        ShortDesc = "a goblin scavenger";
        Tags.Add("goblin"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("dagger");
        LongDesc = "A thin, quick goblin with a sack slung over one shoulder and a bent knife in each hand. Its eyes dart to your purse and then to the nearest exit, as if it is weighing which it would rather have.";
        BaseReaction = 1.5;
        AddCombatAction("stab", "Quick Stab", preparation: 0, recovery: 0.5);
        Level = 2;
        MaxVitality = 13;
        DamageMin = 1;
        DamageMax = 3;
        ToHit = 2;
        Armor = 1;
        XpValue = 22;
        GoldMin = 2;
        GoldMax = 8;
        Aggressive = true;
    }
}
