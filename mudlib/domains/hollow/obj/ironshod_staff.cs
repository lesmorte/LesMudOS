// @inherit /std/weapon

/// <summary>The improved step of the staff line: sold at the Hedge-Scholar's Loft. Adds 1 Insight while wielded.</summary>
public class IronshodStaff : Weapon
{
    public override void Create()
    {
        Name = "Ironshod Staff";
        Ids.Add("staff");
        Ids.Add("ironshod staff");
        Ids.Add("iron staff");
        ShortDesc = "an ironshod staff";
        LongDesc = "A staff of dark ash capped at both ends with iron, the upper cap worked with a ring of small, careful runes. It is a harder thing to be hit with than an apprentice's staff, and it seems to want you to think.";
        Category = "staff";
        Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("staff"); Tags.Add("wood");
        DamageMin = 9;
        DamageMax = 13;
        ToHit = 1;
        ReactionMod = 0.5;
        Recovery = 1.0;
        AttributeBonus["insight"] = 1;
        Magic = "the runes sharpen the mind: +1 Insight while wielded";
        Value = 55;
    }
}
