// @inherit /std/item

public class SgStagBed : Item
{
    public override void Create()
    {
        Name = "Bed for the Night";
        Ids.Add("bed");
        Ids.Add("room");
        Ids.Add("bed for the night");
        ShortDesc = "a bed for the night";
        LongDesc = "A bed in one of the Silver Stag's guest rooms, with clean linen and a thick wool blanket. Orla sells it by the night.";
        Value = 0;
    }
}
