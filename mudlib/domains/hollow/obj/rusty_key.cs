// @inherit /std/item

/// <summary>A key: nothing special in the object itself. A room's LockExit asks for an object that answers to its id.</summary>
public class RustyKey : Item
{
    public override void Create()
    {
        Name = "Rusty Key";
        Ids.Add("key");
        ShortDesc = "a rusty key";
        LongDesc = "A heavy iron key, orange with rust. Whatever it opens has been shut for a long time.";
    }
}
