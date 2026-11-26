// @inherit /std/trait

/// <summary>Scout trait: a trained eye for what is hidden. +2 to every search check, which is 10 percentage points (docs/specs/search.md section 2).</summary>
public class KeenEyeTrait : Trait
{
    public override void Create()
    {
        Title = "Keen Eye";
        Summary = "+2 when you search for hidden things.";
        Mods["search"] = 2;
    }
}
