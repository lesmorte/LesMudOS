// @inherit /std/weapon

/// <summary>The Clerk's Staff of Office (docs/specs/bureaucrat.md section 4): lighter in the hand than the Apprentice's, and weaker, on purpose. Sold by the Registrar to holders of the class.</summary>
public class ClerksStaff : Weapon
{
    public override void Create()
    {
        Name = "Clerk's Staff of Office";
        Ids.Add("staff");
        Ids.Add("clerk's staff");
        Ids.Add("staff of office");
        Ids.Add("clerk's staff of office");
        ShortDesc = "a Clerk's staff of office";
        LongDesc = "A staff of black ash, shod in brass, its head a flat seal-die. It is better for pressing wax than for hitting, and the Chancery does not recommend using it for anything else.";
        Category = "staff";
        Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("staff"); Tags.Add("wood");
        DamageMin = 5;
        DamageMax = 9;
        ToHit = 0;
        ReactionMod = 0.5;
        Recovery = 1.0;
        AttributeBonus["resolve"] = 1;
        Magic = "the seal-head carries the Chancery's mark: +1 Resolve while wielded";
        Value = 30;
    }
}
