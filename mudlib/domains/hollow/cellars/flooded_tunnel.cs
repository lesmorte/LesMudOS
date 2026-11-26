// @inherit /std/room

/// <summary>A water-filled tunnel between Smuggler Hall and the mushroom caverns, home to a giant cave toad.</summary>
public class FloodedTunnel : Room
{
    public override void Create()
    {
        ShortDesc = "Flooded Tunnel";
        LongDesc = "Ankle-deep water covers the floor while distant drips echo through darkness.";
        Exits["west"] = "/domains/hollow/cellars/smuggler_hall";
        Exits["east"] = "/domains/hollow/cellars/mushroom_cavern";
        Safe = false;
        Light = LightDark;
        Wet = true;
        DarkDesc = "Cold water laps at your ankles. Somewhere ahead drops fall into the dark, and the sound goes a long way.";

        AddDetail("water,ankle-deep water,pool,puddles", "Cold, brackish water covers the floor, black and still except where a drip breaks its surface.");
        AddDetail("drips,drip,ceiling,roof", "Water gathers on the roof and falls with a soft tock, and the sound travels far off into the dark.");
        AddDetail("debris,driftwood,bones,wood", "Splintered crate wood and small bones have drifted against the walls and caught in the cracks of the stone.");

        AddSpawn("/domains/hollow/mob/giant_cave_toad", 1);
    }
}
