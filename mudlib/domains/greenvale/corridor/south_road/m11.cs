// @inherit /std/room

public class SrM11 : Room
{
    public override void Create()
    {
        ShortDesc = "The South Road, the Far Bank";
        LongDesc = "The road runs up from the bridge between steep banks of bracken and bramble, and the first great trees of the Eldergreen Wood stand over it: oaks and elms, bigger than any in the farmland. The fences have stopped. The fields have stopped. Where the sun reaches the ground it picks out ferns and foxgloves, and the land smells of leaf-mould and cool stone. The road ahead narrows to a green gap between the trunks, and it is quiet in a way the open country never was.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m10";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m01";
        ResetSeconds = 120;

        AddDetail("oaks,oak,elms,elm,trees,trunks", "Oaks with trunks you could not reach round, and elms drowned in ivy. Their lowest branches reach out over the road like arms.");
        AddDetail("bracken,bramble,banks", "Steep banks of bracken, shoulder high, with a few blackberries ripening among them. Rabbits' runs thread through the bottom.");
        AddDetail("ferns,foxgloves,foxglove", "Spires of purple foxgloves stand in the patches of light. Wise children are told not to touch them.");
        AddDetail("gap,road,green gap,narrowing", "The road narrows to little more than a cart's width. Past that the trees seem to lean in.");
        AddDetail("fences,fields,farmland,behind", "Looking back, the farms lie bright and flat across the river, hedged and fenced and ordinary. The difference is startling.");
        AddReadable("milestone,stone,marker,waystone", "A grey stone, newer than the road, cut with a plain eye, the sign of the Riverwatch:\n\"TO THE HOLLOW, SOUTH. TO OAKENFELL, NORTH THROUGH THE ELDERGREEN WOOD. TRAVEL BY DAYLIGHT.\"", "A grey marker stands at the roadside. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/grey_fox", 12);
        AddEncounter("/domains/greenvale/corridor/mob/field_rabbit", 5);
    }
}
