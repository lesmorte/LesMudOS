// @inherit /std/room

public class GrTheGoblinRaidersCamp : Room
{
    public override void Create()
    {
        ShortDesc = "The Goblin Raiders' Camp";
        LongDesc = "A hollow in the forest has been trampled bare and ringed with a rough palisade of sharpened stakes, and inside it are low shelters of branches, hide and stolen canvas. A cookfire smolders in the middle, ash grey and stinking, with a haunch of something roasting on a spit. The ground is littered with broken crates, wagon wheels and the rags of travelers' packs. Gaudy trinkets hang from the stakes, and at every pause in the wood a goblin voice laughs, short and ugly.";
        Exits["east"] = "/domains/greenvale/corridor/green_road/w3";
        Exits["north"] = "/domains/greenvale/corridor/green_road/d2";
        Safe = false;

        AddDetail("camp,hollow,ground,clearing", "A trampled hollow of mud, ash and bones, and the sour smell of goblins. The raiders are not hiding anything. They know no one comes here and returns.");
        AddDetail("palisade,stakes,stake,fence,wall", "A fence of green stakes, sharpened at the tops and lashed with strips of hide. It is not strong but it is intended to be seen.");
        AddDetail("shelters,shelter,huts,hut,tents,hides,canvas", "Low lean-tos of branches, with ox hides and stolen wagon canvas for roofs. The canvas has a Consortium scale painted on it.");
        AddDetail("post,scratches,scratchings,pictures,drawing,drawings,figures", "Low on a shelter post, at the height a small child could reach, a nail has scratched a row of stick figures holding hands under a round sun with rays. One figure is larger than the rest, and under it, in wobbling capitals: BIG GRIT.");
        AddDetail("fire,cookfire,spit,haunch,ash,embers", "A fire pit of black stones. On the spit is a haunch of venison, or what the goblins have chosen to call venison.");
        AddDetail("crates,crate,wheels,wheel,wreckage,packs,pack,rags", "Splintered crates, wheels with the spokes hacked out and rotting packs. All of it was taken from the road. Little of it is of any use any more.");
        AddDetail("trinkets,trinket,bones,skulls,charms,tokens", "Strings of small stolen things hang from the stakes: spoons, buckles, a child's bell, a tin cup. A goblin's idea of a trophy.");
        AddDetail("tracks,paths,trail,trails", "Many paths lead out of the camp, all well trodden. Only one, to the east, goes toward the road.");

        AddSpawn("/domains/greenvale/corridor/mob/goblin_raider", 3);
    }
}
