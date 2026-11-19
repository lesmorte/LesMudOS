// @inherit /std/room

public class SrM08 : Room
{
    public override void Create()
    {
        ShortDesc = "The South Road, the Long Meadows";
        LongDesc = "Beyond the inn the land opens into long meadows, hay-gold and cut in strips, with haycocks standing in rows like a small town. The hedges are low and few, and the whole country lies open to the wind and the light. Butterflies lift out of the grass at your step, blue and white, and drift away. Far to the north the horizon is no longer sky but a long dark line, level as a ruled edge, which you realise is trees.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m07";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m09";
        ResetSeconds = 120;
        DangerBand(5, 6);

        AddDetail("meadows,meadow,hay,grass", "Long meadows, half-mown, the cut grass drying in swathes. The smell is a thing you could eat.");
        AddDetail("haycocks,haycock,stooks,ricks", "Haycocks stand in orderly ranks, each tied against the wind with a plait of straw. In the shadow of one a farmer's coat lies forgotten.");
        AddDetail("butterflies,butterfly", "Small blues and the occasional white, moving as though blown. They settle on the clover and fold their wings.");
        AddDetail("trees,line,dark line,horizon,forest,wood", "A level dark line on the northern horizon, miles away. That is the Eldergreen Wood, and the road is going straight into it.");
        AddDetail("wind,sky,light", "The wind is steady from the west and carries the smell of rain a long way off. The shadows of clouds cross the meadows like slow ships.");
        AddReadable("milestone,stone,marker", "A milestone, leaning, with its lettering almost swallowed by moss:\n\"TO THE HOLLOW, SOUTH. NORTH, THE ELDERGREEN WOOD.\"\nSomeone has scratched below it, with a nail: \"Mind the wolves.\"", "A mossy milestone leans by the road. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/field_rabbit", 8);
        AddEncounter("/domains/greenvale/corridor/mob/grey_fox", 10);
    }
}
