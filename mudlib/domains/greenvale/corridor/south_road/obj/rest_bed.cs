// @inherit /std/item

public class SrRestBed : Item
{
    public override void Create()
    {
        Name = "A Bed at the Rest";
        Ids.Add("bed");
        Ids.Add("room");
        Ids.Add("bed at the rest");
        Ids.Add("settle");
        ShortDesc = "a bed at the Rest";
        LongDesc = "A warm settle by the kitchen fire of Traveler's Rest, with a feather tick and a heavy blanket. Aldith sells it by the night.";
        Value = 0;
    }
}
