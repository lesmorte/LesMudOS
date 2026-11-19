// @inherit /std/material
public class CrWildflower : Material
{
    public override void Create()
    {
        Name = "Wildflower";
        Ids.Add("flower");
        Ids.Add("wildflower");
        Ids.Add("flowers");
        ShortDesc = "a wildflower";
        Plural = "wildflowers";
        LongDesc = "A small yellow meadow flower with a honeyed scent. On its own it is only pretty, but healers and dyers both prize it.";
        Value = 2;
    }
}
