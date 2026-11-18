// @inherit /std/material
public class CtHealingHerb : Material
{
    public override void Create()
    {
        Name = "Healing Herb";
        Ids.Add("herb");
        Ids.Add("healing herb");
        ShortDesc = "a healing herb";
        LongDesc = "A sprig of grey-green leaves that smell of camphor.";
        Value = 3;
    }
}
