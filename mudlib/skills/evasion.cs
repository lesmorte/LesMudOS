// @inherit /std/trait

/// <summary>Scout trait: 10% dodge.</summary>
public class EvasionTrait : Trait
{
    public override void Create()
    {
        Title = "Evasion";
        Summary = "8% chance to dodge an attack.";
        Mods["dodge"] = 8;
    }
}
