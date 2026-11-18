// @inherit /std/armor

public class CommandersVest : Armor
{
    public override void Create()
    {
        Name = "Leather Commander's Vest";
        Ids.Add("vest");
        Ids.Add("leather vest");
        Ids.Add("commander's vest");
        Ids.Add("commanders vest");
        ShortDesc = "a leather commander's vest";
        LongDesc = "A supple vest of boiled leather, studded at the shoulders. It was made for a human officer, long ago, and has been worn by goblins since.";
        ArmorValue = 3;
        Slot = "body";
        ReactionMod = 0.0;
        Category = "light";
        Value = 60;
    }
}
