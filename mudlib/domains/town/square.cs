// @inherit /std/room

public class Square : Room
{
    public override void Create()
    {
        ShortDesc = "Town Square";
        LongDesc = "Cobblestones spread out around a dry fountain. A tavern door stands open to the north.";
        Exits["north"] = "/domains/town/tavern";
        MoveObject(CloneObject("/domains/town/lantern"), Self);
    }
}
