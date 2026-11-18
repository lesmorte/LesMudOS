// @inherit /std/room

public class RwNest : Room
{
    public override void Create()
    {
        ShortDesc = "The Broodmother's Nest";
        LongDesc = "The slope ends in a great round chamber, domed like a bell, and it is filled almost to the walls by a nest. The nest is a vast mound of shredded hide, fur, timbers and bone, packed and trampled into a bed, and it is warm. Smaller burrows open all round the walls at floor level, each as wide as a barrel. The reek is almost solid. At the back of the chamber, the far wall is a slope of fallen earth and broken stone, all that is left of the shaft the goblins dug and could not close.";
        Exits["up"] = "/domains/greenvale/corridor/root_warrens/brood_passage";
        ResetSeconds = 300;

        AddDetail("nest,mound,bed,bedding,hide,fur,timbers,heap", "The nest is a bed of everything the warren once held: hides, furs, timbers, splintered furniture, the scraps of goblin lives. It has been trampled flat in a hollow at its middle and packed so hard it could be walked on. It is warm to the touch, like a living thing.");
        AddDetail("bones,bone,skulls,skull,ribs", "Bones are bedded in the nest by the cartload, among them the great skulls of boars and the long ribs of deer, and also, in a layer near the bottom, many small bony hands and pointed skulls that are not rats'.");
        AddDetail("burrows,burrow,holes,hole,openings,runs", "The burrows are round and smooth-sided and slope steeply away into the earth, and the sound of scratching comes out of all of them. They are the runs of the rats, and they lead to every part of the warren.");
        AddDetail("dome,roof,ceiling,chamber,walls,wall", "The dome is cut from the bare clay, and it is scored all over with the marks of teeth, so deeply and so high that the thing that made them must have stood on the nest with its head thrown back. A few roots hang from the apex, grey and frayed to threads.");
        AddDetail("shaft,rockfall,fall,slope,stone,stones,masonry,lintel,blocked,collapse,earth", "The fallen earth fills what was once a shaft, the full width of the back wall, and heaped in it are broken blocks of dark green-grey stone, squared and fitted with no mortar. This is not goblin work. A single great lintel stands out of the fall, carved with a band of worn lines, and past it there is only rubble, packed tight and mounded to the dome. The rats' runs go into it and stop. Whatever lies below, there is no way through.");
        AddDetail("reek,smell,air,warmth,warm", "The reek is of rat in its thousands, and under it of something heavier and older. The warmth of the nest comes through the soles of your feet.");

        AddSpawn("/domains/greenvale/corridor/mob/warren_broodmother", 1);
        AddSpawn("/domains/greenvale/corridor/mob/tunnel_rat", 2);
    }
}
