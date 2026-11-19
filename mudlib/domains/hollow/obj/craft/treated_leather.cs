// @inherit /std/material
public class CrTreatedLeather : Material
{
    public override void Create()
    {
        Name = "Treated Leather";
        Ids.Add("leather");
        Ids.Add("treated leather");
        ShortDesc = "a piece of treated leather";
        Plural = "pieces of treated leather";
        LongDesc = "A piece of hide cured with blood and oil until it is supple and dark. It keeps a faint tannery smell, and a leatherworker would call it a good start.";
        Value = 4;
    }
}
