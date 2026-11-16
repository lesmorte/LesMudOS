// @inherit /std/room

public class Tavern : Room
{
    public override void Create()
    {
        ShortDesc = "The Compiled Griffin";
        LongDesc = "A low-beamed tavern that smells of woodsmoke. The square lies to the south.";
        Exits["south"] = "/domains/town/square";
        MoveObject(CloneObject("/domains/town/cat"), Self);
    }
}
