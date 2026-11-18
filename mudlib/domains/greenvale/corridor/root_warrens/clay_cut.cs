// @inherit /std/room

public class RwClayCut : Room
{
    public override void Create()
    {
        ShortDesc = "The Clay Cut";
        LongDesc = "The goblins dug out a broad pale seam of clay here, and the walls of the cut are smooth and grey-white and slick with damp. Water drips somewhere in the dark with a patient, regular sound. Against the north wall, out of the wet, someone has made a camp of sorts: a sodden bedroll, a tin pan, a miner's pick of a different make from the goblin tools, and a leather satchel hanging open from a peg. No one is here. The tunnel runs on eastward, and the clay gives way to a thicket of pale roots.";
        Exits["north"] = "/domains/greenvale/corridor/root_warrens/spoil_heap";
        Exits["east"] = "/domains/greenvale/corridor/root_warrens/root_hall";
        ResetSeconds = 300;

        AddDetail("seam,clay,walls,wall,cut,grey-white", "The seam is as fine as flour and cold as a stone. Where the diggers cut it, it has a glossy surface printed with the marks of their broad hands and the grain of the picks. It would take a fine glaze, a potter might say, and fire hard.");
        AddDetail("camp,bedroll,bed,pan,tin", "The bedroll has rotted to a mat, and mould furs the blanket. The pan is a prospector's pan, rimmed with rust, and still holds a crust of old clay in the bottom where someone was washing a sample.");
        AddDetail("pick,tool,tools,miner", "The pick is long-handled and light, with a head of good steel and a ferrule worn bright with use. It is a man's tool, with a man's grip worn into the handle, and no goblin ever held it.");
        AddReadable("satchel,pages,journal,book,notebook,papers", "The notebook is swollen with damp and hard to turn, but the last pages can be made out:\n\n  'Day 1. North by the Greenvale road. Found a hollow under a big fallen oak, and below it goblin work, old, too square for a fox. Good clay in the west cut, near white. Worth a claim.'\n\n  'Day 2. Tools everywhere and nobody home. A cooking pot still on the hearth in the quarters, rotted to a skin. They left in a hurry. I do not like how quiet it is.'\n\n  'Day 3. Rats. Not the size a rat should be. I lit the lamp and they kept coming, so I stood in the clay cut with my back to the wall until they lost interest. Heard hammering to the south, behind a mass of roots. Goblins, living ones. I shall not go near.'\n\n  'Day 4. The draught is stronger at night and it comes from below. It smells like a nursery rat a hundred times over. I heard something gnaw through a timber as thick as my leg. Going up at first light.'\n\n  'Day 4, later. I cannot find the stair. I took the east cut and the roots have moved. No. I am confusing the cuts. West, then north, then'\n\nThe line stops there, and the rest of the page is a smear of clay.", "A leather satchel hangs from a peg, a notebook showing inside. You could {cmd}read{/} it.");
        AddDetail("drip,water,dripping,sound", "The water comes from a crack in the roof and falls into a hollow in the floor the size of a bowl, which it has worn for itself. It has been falling for years and has never once run over.");
        AddDetail("roots,thicket,pale roots,east", "East, the clay seam ends at the foot of a wall of pale roots that pour through the roof. They are the colour of old tallow, as thick as a man's arm and as thin as thread, and the air near them is cold and tastes of earth.");
    }
}
