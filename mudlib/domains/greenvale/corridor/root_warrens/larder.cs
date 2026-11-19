// @inherit /std/room

public class RwLarder : Room
{
    public override void Create()
    {
        ShortDesc = "The Cold Larder";
        LongDesc = "A cramped dry chamber has been cut into the hard clay behind the hearth, no taller than a stooping goblin, and the rats have never found it. Shelves of split root line three sides, and on them stand rows of stoppered clay jars, most of them cracked or empty. The air is cold, still and dry, and smells of dust and old smoke. Sacks have sunk to grey heaps on the floor. In the corner, set apart from the rest, a small chest lies under a scrap of hide, as though someone had hidden it from their own people.";
        Exits["west"] = "/domains/greenvale/corridor/root_warrens/quarters";
        ResetSeconds = 300;
        DangerBand(7, 8);

        AddDetail("shelves,shelf,jars,jar,stoppers,pots", "The jars are the squat grey pots of the clay seam, stoppered with wax and rag. Most are empty, or hold a dry brown powder that was once grain, and a few have cracked and spilled. One row has been carefully set aside and each pot is sealed and heavy, but they hold only salt, and the salt has gone hard as stone.");
        AddDetail("sacks,sack,heaps,grain,dust", "The sacks have fallen in on themselves and are grey, with a fur of mould. Whatever they held has long since gone to dust.");
        AddDetail("chest,box,corner,hide,scrap", "The chest is small, plain and iron-bound, and it has stood unopened so long that the scrap of hide over it has dried to a board. It is a goblin's private hoard: the hidden savings of a digger who meant to come back for them.");
        AddDetail("hearth,flue,wall,back,warmth", "The back wall of the larder is the other face of the hearth, and the clay there is still faintly warm and baked hard. That warmth is why the stores lasted so long.");
        AddDetail("clay,floor,walls,dry,air", "The walls and floor are of clay baked as hard as brick, with not a trace of damp. No rat has ever dug here, and the only prints in the dust are the ones you are making.");

        AddSpawn("/domains/greenvale/corridor/root_warrens/obj/larder_chest");   // a find: each character takes it once, for less each time
    }
}
