// @inherit /std/room

/// <summary>Where objects wait while their room is being updated.</summary>
public class Void : Room
{
    public override void Create()
    {
        ShortDesc = "The Void";
        LongDesc = "You float in nothingness while the world is rebuilt around you.";
        Exits["out"] = "/domains/town/square";
    }
}
