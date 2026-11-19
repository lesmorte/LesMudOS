// @inherit /std/material
public class HcHealingHerb : Material
{
    public override void Create()
    {
        Name = "Healing Herb";
        Ids.Add("herb");
        Ids.Add("healing herb");
        ShortDesc = "a healing herb";
        LongDesc = "Grey-green leaves that smell of camphor.";
        Value = 3;
    }
}
