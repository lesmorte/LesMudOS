// @inherit /std/weapon

/// <summary>The fine step of the staff line: found once by each character in the Mushroom Cavern. Adds 2 Insight while wielded.</summary>
public class HollowOakStaff : Weapon
{
    public override void Create()
    {
        Name = "Staff of the Hollow Oak";
        Ids.Add("staff");
        Ids.Add("oak staff");
        Ids.Add("hollow oak staff");
        Ids.Add("staff of the hollow oak");
        ShortDesc = "a staff of the Hollow Oak";
        LongDesc = "A straight rod of pale heartwood, as long as you are tall and warm to the hand, with a faint grain like the rings of a very old tree. Nothing that grows in the Hollow is this colour. Your thoughts come a little clearer when you hold it.";
        Category = "staff";
        Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("staff"); Tags.Add("wood");
        DamageMin = 10;
        DamageMax = 14;
        ToHit = 1;
        ReactionMod = 0.5;
        Recovery = 1.0;
        AttributeBonus["insight"] = 2;
        Magic = "grown from the roots of the Hollow Oak: +2 Insight while wielded";
        Value = 90;
    }
}
