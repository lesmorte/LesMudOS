// @inherit /std/trait

/// <summary>Warrior trait: +1 to hit with any weapon.</summary>
public class WeaponProficiencyTrait : Trait
{
    public override void Create()
    {
        Title = "Weapon Proficiency";
        Summary = "+1 to hit with any weapon.";
        Mods["tohit_armed"] = 1;
    }
}
