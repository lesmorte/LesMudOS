// @inherit /std/material
public class CrBloodInk : Material
{
    public override void Create()
    {
        Name = "Blood Ink";
        Ids.Add("ink");
        Ids.Add("blood ink");
        ShortDesc = "a vial of blood ink";
        Plural = "vials of blood ink";
        LongDesc = "Dark red ink, thick with goblin blood, that dries nearly black. Scribes, scholars and rune crafters will want it.";
        Value = 6;
    }
}
