// @inherit /std/item

public class TavernBed : Item
{
    public override void Create()
    {
        Name = "Bed for the Night";
        Ids.Add("bed");
        Ids.Add("room");
        Ids.Add("bed for the night");
        ShortDesc = "a bed for the night";
        LongDesc = "A bed in one of the guest rooms above the tavern, with clean linen and a thick wool blanket. Harland sells it by the night.";
        Value = 0;
    }
}
