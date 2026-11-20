// @inherit /std/trait

/// <summary>Scout trait: 5% dodge (docs/ATTRIBUTES.md section 8).</summary>
public class EvasionTrait : Trait
{
    public override void Create()
    {
        Title = "Evasion";
        Summary = "5% chance to dodge an attack.";
        Mods["dodge"] = 5;
    }
}
