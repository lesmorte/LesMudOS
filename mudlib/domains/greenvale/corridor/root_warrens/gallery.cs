// @inherit /std/room

public class RwGallery : Room
{
    public override void Create()
    {
        ShortDesc = "The Gnawed Gallery";
        LongDesc = "A long straight gallery runs here, shored with timbers every few paces, and every one of the timbers has been gnawed. Some are chewed to hourglass waists, others are bitten right through and propped against their neighbours, and the roof between them sags in a belly of loose clay. The floor is deep in shredded bedding, hide and splinters, and mounded into nests along both walls. The air is rank and close, and it rustles. Roots hang down from the sagging roof like grey cords, and the gallery bends away west toward a pale dimness.";
        Exits["north"] = "/domains/greenvale/corridor/root_warrens/quarters";
        Exits["west"] = "/domains/greenvale/corridor/root_warrens/root_hall";
        ResetSeconds = 300;
        DangerBand(7, 8);

        AddDetail("timbers,timber,props,prop,posts,post,beams,waists", "The oak is dark with age and the teeth marks are pale and fresh, in neat parallel pairs. In a few places a prop has been gnawed through and the roof has begun to settle onto its stump. The rats are bringing the gallery down a mouthful at a time.");
        AddDetail("nests,nest,bedding,shreds,hide,splinters,litter", "The nests are dense heaps of shredded hide, fur and splinters, packed hard and lined with something soft. In the bigger heaps are the bones of whatever the rats have eaten: cave fish, birds, the long bones of a deer, and the cracked shells of beetles. Nothing in them is of any use.");
        AddDetail("roof,ceiling,belly,clay,sag", "The roof bellies down between the props in loose bags of clay, with fine roots coming through like hair. A little earth sifts down with every footstep, and now and then a small clod drops with a wet slap.");
        AddDetail("roots,root,cords,pale", "The roots that hang here are thin and grey and slick, and when you stand still they sway a little, though the air is almost still. They are thickest at the western end of the gallery.");
        AddDetail("floor,bones,bone,droppings", "The floor is a mash of gnawed bone, splinters and droppings. Among them, in one place, a ring of bones laid out in a careful circle, as if a rat had arranged them for a purpose.");
        AddDetail("dimness,west,glow,fungus", "To the west the gallery bends, and beyond the bend there is a faint cold pallor, as of moonlight through water, which does not come from any lamp. It is the colour of the roots.");

        AddSpawn("/domains/greenvale/corridor/mob/tunnel_rat", 2);
    }
}
