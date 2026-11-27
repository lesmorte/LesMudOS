// @inherit /std/monster

public class WanderingWolf : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system ("wolf"), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "wolf";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "wolf";

    public override void Create()
    {
        Name = "Wandering Wolf";
        Ids.Add("wolf");
        ShortDesc = "a wandering wolf";
        Tags.Add("beast"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("bite");
        LongDesc = "A lean grey wolf with a ragged coat and a scarred muzzle. It moves along the edge of the road with its head low, and its yellow eyes never leave you.";
        BaseReaction = 2.0;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 1.0);
        Level = 2;
        MaxVitality = 26;
        DamageMin = 2;
        DamageMax = 4;
        ToHit = 1;
        Armor = 1;
        XpValue = 25;
        Aggressive = true;
    }
}
