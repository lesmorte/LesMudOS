// @inherit /std/material
public class CrTallow : Material
{
    public override void Create()
    {
        Name = "Tallow";
        Ids.Add("tallow");
        Ids.Add("lump");
        Ids.Add("fat");
        ShortDesc = "a lump of tallow";
        Plural = "lumps of tallow";
        LongDesc = "A greasy grey-white lump of rendered fat, kitchen dripping or hide scrapings, that smells faintly of old mutton. It burns, slowly and smokily.";
        Value = 1;
    }
}
