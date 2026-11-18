// @inherit /std/room

/// <summary>
/// The Healer's Cottage: home of the Herbalist Table (a described detail for now; the crafting
/// station comes later) and of Elspeth Wortley.
/// </summary>
public class VhHealersCottage : Room
{
    public override void Create()
    {
        ShortDesc = "The Healer's Cottage";
        LongDesc = "Bunches of drying herbs hang from every beam of this low, whitewashed cottage, and the air is thick with mint, smoke and something bitter underneath. A broad scrubbed table fills the middle of the floor, crowded with a stone mortar, brass scales and rows of stoppered jars. Drying racks line the walls, and a kettle ticks on a small iron stove in the corner. A chalked list hangs by the door.";
        Exits["out"] = "/domains/hollow/village/square";
        Safe = true;

        AddDetail("table,herbalist table,herbalist's table,broad table,scrubbed table", "A broad table of pale pine, scrubbed so often that the grain stands up in ridges. Stains of green, brown and a surprising purple have soaked in anyway. This is where salves, tonics, poultices and potions are made, and Elspeth will tell you so if you ask.");
        AddDetail("mortar,pestle,stone mortar", "A heavy stone mortar, worn smooth inside, with a pestle whose handle has been polished by years of one particular hand.");
        AddDetail("scales,brass scales,weights", "Brass scales with a neat set of weights beside them, each no bigger than a lentil. Elspeth measures everything, even the things she says she does by feel.");
        AddDetail("racks,drying racks,rack,herbs,bunches", "Slatted racks of wicker, hung with healing herbs, wildflowers, thistle heads and strings of dried berries. Each bunch is tied with a scrap of coloured wool so she can tell which is which.");
        AddDetail("jars,stoppered jars,shelf", "Squat clay jars with cork stoppers, each labelled in a small, firm hand: honey, willow bark, clean lint, and one marked only \"NO\".");
        AddDetail("stove,kettle", "A small iron stove with a dented kettle on top. Whatever is in it is not tea, or not only tea.");
        AddReadable("list,chalked list,chalk list,slate", "The chalked list reads:\n  \"Cuts, burns and bad chests: ask.\n  Bring your own cloth.\n  No wolves' teeth in my kitchen.\n  The jar marked NO means NO.\"", "A list is chalked on a slate by the door. You could {cmd}read{/} it.");

        AddSpawn("/domains/hollow/npc/elspeth");
    }
}
