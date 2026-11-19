// @inherit /std/room

public class SrM09 : Room
{
    public override void Create()
    {
        ShortDesc = "The South Road, Willow Brook";
        LongDesc = "The road dips to meet a quick, clear brook that crosses it on a paved ford, the water shin deep and chattering over pebbles. Willows lean over both banks, their long leaves trailing in the current, and the shade after the open meadows is a blessing. Watercress grows green at the margins, and a kingfisher flashes blue-green across the stream and is gone. Stepping stones for the careful stand to the right of the ford, and a footbridge of two planks crosses the narrowest part.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m08";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m10";
        ResetSeconds = 120;
        DangerBand(5, 6);

        AddDetail("brook,stream,water,current", "The brook is cold, clear and busy, talking to itself over stones the colour of tea. It runs north, toward the river.");
        AddDetail("willows,willow,leaves,banks", "Old pollard willows with rough crowns of whips. Their roots have laid hold of the bank and hold it up.");
        AddDetail("ford,paving,pebbles", "The paving of the ford is old flagstone, set level and green with algae. Cart wheels have worn two shallow grooves across it.");
        AddDetail("watercress,cress,margins,reeds", "Peppery watercress, green and thick, in the shallows. Someone has picked a handful recently and left the roots.");
        AddDetail("kingfisher,bird", "A kingfisher in a flash of electric blue, there and gone. It left a small ring on the water where it dived.");
        AddDetail("stepping stones,stones,planks,footbridge,bridge", "Flat stones set in the stream at a stride's length, and a footbridge of two planks beside them. A hand rope is strung over the planks, frayed but sound.");

        AddEncounter("/domains/greenvale/corridor/mob/grey_fox", 10);
        AddEncounter("/domains/greenvale/corridor/mob/field_rabbit", 6);
    }
}
