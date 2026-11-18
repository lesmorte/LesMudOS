// @inherit /std/room

public class BgWestwoodRoad : Room
{
    public override void Create()
    {
        ShortDesc = "The Westwood Road";
        LongDesc = "The Westwood Road leaves Westgate between low stone walls and runs west across rolling fields, rutted and patched where the carters' wheels have bitten into it. A mile out, the fields give way to a dark line of trees along the horizon: the edge of the Elderwood, tall and old and silent. A milestone leans at the roadside with moss on its north face. The road keeps going, but there are no wagons ahead, only a single hawk circling over the verge.";
        Exits["east"] = "/domains/greenvale/oakenfell/market/westgate";
        BlockExit("west", "The Westwood Road runs on west toward the Elderwood, but it is not yet open to travelers. The Riverwatch no longer patrols it past the second milestone.");

        AddDetail("road,westwood road,ruts,wheels,ground,verge", "A packed earth and gravel road, deeply rutted, its edges rimmed with weeds. It runs straight west toward the trees, and far ahead the ruts fade into the shade.");
        AddDetail("fields,walls,stone walls,low walls,hawk", "Low dry-stone walls divide the fields, empty of workers. A hawk hangs above them, motionless on the wind.");
        AddDetail("trees,elderwood,forest,wood,horizon,tree line,edge", "A long, dark line of trees stands along the western horizon, taller than any wood you have seen. That is the Elderwood. Even from here it feels old, and quiet in a way that makes you lower your voice.");
        AddReadable("milestone,stone,marker,mile stone", "The milestone's face has been scraped clear of moss to read:\n\"OAKENFELL, 1.\nTHE ELDERWOOD.\"", "A milestone leans at the roadside. You could {cmd}read{/} it.");
    }
}
