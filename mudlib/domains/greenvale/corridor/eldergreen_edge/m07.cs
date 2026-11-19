// @inherit /std/room

public class EeCharcoalClearing : Room
{
    public override void Create()
    {
        ShortDesc = "The Edge Road, the Charcoal Clearing";
        LongDesc = "East of the road the trees fall back from a broad clearing blackened underfoot and strewn with the domed turf mounds of charcoal clamps, two of them smoking slow and blue. The smell of burnt oak is thick enough to taste, and a fine black dust lies over every leaf, the burners' huts and the burners themselves. A trail leaves the clearing's far side into the birches. The road itself is clean here, swept by the passing of loaded carts.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m06";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m08";
        Exits["east"] = "/domains/greenvale/corridor/eldergreen_edge/w5";
        ResetSeconds = 120;
        DangerBand(5, 7);

        AddDetail("clearing,ground,dust,black dust,soot,ash", "The ground of the clearing is black with charcoal fines, soft as soot, and every footprint in it is sharply cut. Even the grass at its edge is grey with dust.");
        AddDetail("mounds,mound,clamps,clamp,turf,kilns,kiln,smoke", "A clamp is a stack of split oak billets covered with turf and left to smoulder for days without ever catching flame. The nearest sweats blue smoke from vents along its top; the other has burned out and been opened, and its black heart steams.");
        AddDetail("huts,hut,shelter,shelters,beds", "Two bark-roofed huts, low enough to need stooping, stand at the clearing's edge, each with a straw bed and a pot hung over a cold hearth. They are rough but clean.");
        AddDetail("charcoal,sacks,sacking,baskets,billets", "Sacks of charcoal are stacked by the road for the carters, black and light and clinking. A basket of untouched billets stands ready by each clamp.");
        AddDetail("trail,east,birches,birch", "The trail east vanishes into the pale trunks of birches, which are noticeably whiter than everything the dust has touched.");
        AddDetail("tools,rakes,rake,shovel,shovels", "Long rakes and flat shovels lean against a hut, their handles black from use. Beside them sits a bucket of water, because the burners know how a clamp can turn.");
        AddDetail("road,carts", "The road is swept clean by cart wheels, which come and go on a ramp of packed earth the burners built.");

        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/berenice");
        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/cadoc");
    }
}
