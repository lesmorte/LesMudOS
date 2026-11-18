// @inherit /std/item

public class HwScorchedSatchel : Item
{
    public override void Create()
    {
        Name = "Scorched Satchel";
        Ids.Add("satchel");
        Ids.Add("scorched satchel");
        Ids.Add("bag");
        Ids.Add("leather satchel");
        ShortDesc = "a scorched satchel";
        LongDesc = "A plain leather satchel with a buckled flap, burned black down one side and sound on the other. It is empty. There is no name, mark or maker's stamp on it, and the stitching has been cut and re-sewn once with a different thread.";
        Value = 0;
    }
}
