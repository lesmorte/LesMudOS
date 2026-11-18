// @inherit /std/item

public class SgTinderbox : Item
{
    public override void Create()
    {
        Name = "Tinderbox";
        Ids.Add("tinderbox");
        Ids.Add("tinder");
        Ids.Add("flint");
        Ids.Add("flint and steel");
        Ids.Add("box");
        ShortDesc = "a tinderbox";
        LongDesc = "A battered tin box holding flint, steel and a wad of dry tinder. It will light a fire in the wettest weather, with patience.";
        Value = 4;
    }
}
