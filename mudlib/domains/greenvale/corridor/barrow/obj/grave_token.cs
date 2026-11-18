// @inherit /std/item

public class FbGraveToken : Item
{
    public override void Create()
    {
        Name = "Grave Token";
        Ids.Add("token");
        Ids.Add("grave token");
        Ids.Add("disc");
        Ids.Add("stone disc");
        ShortDesc = "a flat stone token";
        LongDesc = "A disc of green-grey stone no bigger than a coin, ground perfectly flat. One face is cut with a ring crossed by a bar; the other is blank. It was laid in a bowl in a tomb, and it is very cold to the hand.";
        Value = 15;
    }
}
