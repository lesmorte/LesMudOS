// @inherit /std/room

public class GrCrossroadsStone : Room
{
    public override void Create()
    {
        ShortDesc = "The Crossroads Stone";
        LongDesc = "An old marker stone stands at the middle of a small open space where the road meets an older way. The stone is waist-high and square, black with age, and the track that leaves it eastward is almost swallowed by brambles and by fern. The road itself goes on north into deeper, older woods whose trunks are broad enough to hide a wagon. Behind you the way south is dark and close. Nobody waits here, and the stone seems to prefer it.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m10";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m01";
        Safe = false;
        DangerBand(6, 7);

        AddDetail("stone,marker,crossroads stone,marker stone,pillar", "A square pillar of dark stone waist-high, set in the ground long before the road was cut. Its top is worn into a shallow dish that holds rainwater and a few dead leaves.");
        AddDetail("track,overgrown track,old way,path,east track,heartwood track", "An old way, no wider than a cart, running off east between the trunks and nearly lost under bramble and fern. It heads east toward the Heartwood, and the carvings on the stone call it the Old Way.");
        AddDetail("brambles,bramble,fern,ferns,undergrowth", "The undergrowth has swallowed the track, but a gap has been cut recently by someone with a billhook. Only a very sure traveller would follow it.");
        AddDetail("trees,woods,north,deeper woods,old growth", "To the north the trunks grow broader and darker. The road enters them like a thread into cloth.");
        AddDetail("dish,hollow,rainwater,leaves,top", "Rainwater lies in the shallow dish worn into the top. A single copper coin lies in the water, green with age.");
        AddDetail("south,way south,dark,behind", "The road you came by looks narrow and dark behind you. You have not seen the sky for a long time.");
        AddReadable("carving,carvings,writing,letters,inscription,marker words", "Cut on the four faces of the stone, faint with age, in four different hands:\n\nNORTH: OAKENFELL, FOR THE LIVING\nSOUTH: THE HOLLOW, FOR THE HOMESICK\nEAST: THE OLD WAY. NO CARTS.\nWEST: (a name cut out with a chisel)", "Letters are cut on all four faces of the stone. You could {cmd}read{/} them.");

        AddEncounter("/domains/greenvale/corridor/mob/boar_brute", 6);
    }
}
