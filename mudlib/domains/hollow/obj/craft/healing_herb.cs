// @inherit /std/material
public class CrHealingHerb : Material
{
    public override void Create()
    {
        Name = "Healing Herb";
        Ids.Add("herb");
        Ids.Add("healing herb");
        Ids.Add("herbs");
        ShortDesc = "a healing herb";
        Plural = "healing herbs";
        LongDesc = "Soft grey-green leaves that smell of camphor and cut grass. Elspeth says half her medicine begins with this one plant.";
        Value = 3;
    }
}
