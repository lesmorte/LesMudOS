// @inherit /std/room

public class HwM06 : Room
{
    public override void Create()
    {
        ShortDesc = "The Heartwood Road, the Standing Trunks";
        LongDesc = "Here the giants are dead, and have been for a very long time. Their trunks still stand, stripped of bark and bleached to a smooth silver, each hollow or split along its length and taller than any tower you have seen. The road runs among them as it would among pillars, and between them stand young trees in the light of the gaps, pale and thin. A wind that does not reach the ground sets the bare tops sighing, high overhead. Beneath them the claw marks on the silver wood begin at the height of a man's reach and go higher.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m05";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m07";

        AddDetail("trunks,trunk,giants,dead trees,standing trunks,pillars,snags", "The dead trunks have lost every limb and most of the bark and stand like columns, grey-silver and cracked from root to crown. Some are hollow enough that you could walk into them upright. A smell of old damp wood and of fox comes from the openings.");
        AddDetail("claw marks,claws,scratches,marks,gouges", "Four parallel grooves, each as long as your hand, are torn in the silver wood at the height of your head. Above them are more, and above those more still. Whatever made them stood on its hind legs and was not afraid of being seen.");
        AddDetail("saplings,young trees,young,sapling,birch", "In every gap grow young trees, thin and pale, leaning toward the light. They make a second, smaller forest between the dead pillars.");
        AddDetail("wind,sighing,tops,crowns,sound", "High in the broken crowns a wind is moving that you cannot feel. The dead wood hums and clicks in it, a low note and a dry one.");
        AddDetail("hollow,hollows,opening,openings,inside", "Inside the nearest hollow the wood is smooth, charred black in a ring where a fire was once lit, long ago, and bedded with the dust of rot. A single bone lies at the back. It is a hare's.");
        AddDetail("road,ground,dirt", "The road is firm here, packed by roots that lie just under it, and the leaves lie on it unmarked except for one set of broad prints that crosses it from east to west.");
        AddEncounter("/domains/greenvale/corridor/mob/black_bear", 10);
    }
}
