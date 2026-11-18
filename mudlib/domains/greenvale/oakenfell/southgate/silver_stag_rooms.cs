// @inherit /std/room

public class SgSilverStagRooms : Room
{
    public override void Create()
    {
        ShortDesc = "The Silver Stag Inn, Guest Rooms";
        LongDesc = "A long, quiet passage runs between the inn's guest rooms, lit by a single window at the far end. The floorboards are scrubbed pale and the doors are painted green, each with a brass number. Behind them are narrow beds with clean linen, thick wool blankets and a washstand. The noise of the common room below is a pleasant murmur through the floor, and the street outside is shut away by thick shutters. It is a place to sleep without fear.";
        Exits["down"] = "/domains/greenvale/oakenfell/southgate/silver_stag";
        Safe = true;
        LogoutRoom = true;
        ResetSeconds = 120;

        AddDetail("doors,door,brass numbers,numbers", "The doors are painted green, each with a brass number tacked above a lock. All stand ajar, ready for guests.");
        AddDetail("beds,bed,blankets,linen", "Narrow beds made up with white linen and heavy wool blankets. The pillows smell faintly of lavender.");
        AddDetail("window,shutters", "The shutters are bolted tight against the night, but light leaks around their edges. By day, you see a slice of red roofs and chimney smoke.");
        AddDetail("floor,floorboards,passage", "The boards are scrubbed to the colour of bone, and the passage smells of soap and beeswax.");
        AddDetail("washstand,basin,jug", "Each room has a washstand with a chipped basin and a jug of clean water, replaced every morning.");

    }
}
