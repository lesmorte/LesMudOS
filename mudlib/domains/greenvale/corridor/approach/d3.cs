// @inherit /std/room

public class OaD3 : Room
{
    public override void Create()
    {
        ShortDesc = "The Veterans' Den";
        LongDesc = "The slope falls away into a sheltered hollow where a toppled barn-sized slab of old masonry has made a roof over a long, low den. Weapons are racked along the walls in a neat row, a drill-ground of beaten earth has been swept in front, and the rags of a faded banner hang on a pole. Everything here is orderly, which is somehow worse than a goblin's usual squalor: these are soldiers, and this is a barracks. Bones lie in a neat midden at one side, and the whole place smells of oil, woodsmoke and old cruelty.";
        Exits["south"] = "/domains/greenvale/corridor/approach/d2";
        Exits["east"] = "/domains/greenvale/corridor/approach/w5";
        ResetSeconds = 120;
        DangerBand(9, 10);

        AddDetail("den,hollow,slab,masonry,roof", "A great slab of tumbled stone, fallen from some older wall, has been propped on stacked rubble to make a low hall. Inside it is dark and dry and smells of leather.");
        AddDetail("weapons,rack,racks,spears,swords,axes", "Spears, hatchets and short swords lie in racks, oiled and counted. Not one of them is a goblin's make; every blade came from a human armoury.");
        AddDetail("drill ground,ground,earth,swept earth,drill", "The earth in front of the den is swept bare, and marked with lines drawn by a stick. Along them, deep impressions show where men have stood shoulder to shoulder.");
        AddDetail("banner,rags,faded banner,pole,standard", "A pole carries a tatter of faded cloth, the ghost of a device that no one now could name: three bars, one above another, like three notches cut in a stick. It has been mended with careful stitches, which seems an extraordinary thing for goblins to do.");
        AddDetail("midden,bones,pile,refuse", "A tidy mound of bones and shell, kept at the downwind end of the hollow. Someone has even raked it.");
        AddDetail("tally sticks,sticks,tallies,notches", "Bundles of peeled willow sticks hang from the roof, each notched with long rows of cuts. They look like campaigns, and the rows of cuts are many. Every stick begins with the same three notches.");

        AddSpawn("/domains/greenvale/corridor/mob/rogue_goblin_veteran", 3);
    }
}
