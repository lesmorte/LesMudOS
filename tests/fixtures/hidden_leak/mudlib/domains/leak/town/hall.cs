// @inherit /std/room
public class LeakHall : Room
{
    public override void Create()
    {
        ShortDesc = "Hall"; LongDesc = "A plain hall. Nothing to see.";
        Exits["out"] = "/domains/leak/own/room";
        AddDetail("sign", "A sign says: the Keeper of Leaks is in.");
        AddDetail("board", "The notice board lists the leaky class.");
        AddDetail("slate", "The slate: join the leak hunt today.");
        AddDetail("wall", "Scratched on the wall: Seep Away.");
        GatedExit("in", "/domains/leak/own/room", "leaky-door", "in");
    }
}
