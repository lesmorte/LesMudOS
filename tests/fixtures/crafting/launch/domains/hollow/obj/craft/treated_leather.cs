// @inherit /std/material
public class HcTreatedLeather : Material
{
    public override void Create()
    {
        Name = "Treated Leather";
        Ids.Add("leather");
        Ids.Add("treated leather");
        ShortDesc = "a piece of treated leather";
        Plural = "pieces of treated leather";
        LongDesc = "Supple leather, cured and oiled.";
        Value = 4;
    }
}
