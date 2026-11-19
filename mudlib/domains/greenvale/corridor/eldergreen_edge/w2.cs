// @inherit /std/room

public class EeHuntersCamp : Room
{
    public override void Create()
    {
        ShortDesc = "The Hunter's Camp";
        LongDesc = "A scatter of patched hide tents and a long lean-to crowd a small clearing in the wood, around a fire pit ringed with blackened stones. Skins are pegged out to dry on frames, haunches of venison hang smoking over a smouldering fire, and a rack of antlers leans against a tree like a thorn hedge. The air smells of woodsmoke, brine, fat and leather. Boot-prints and the marks of dragged carcasses lead off in all directions, and a quiet watchfulness hangs over the place.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/w1";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/w3";
        ResetSeconds = 120;
        DangerBand(6, 7);

        AddDetail("tents,tent,hide tents,lean-to,leanto,shelter", "The tents are hide stretched over bent saplings, patched with other hides in other colours. Under the lean-to, bedrolls are laid out in a row with a bow beside each, and one bedroll has not been slept in for a long time.");
        AddDetail("fire,fire pit,pit,stones,ash", "The fire pit is ringed with river stones blackened by use. A spit stands over the embers with a hare turning on it, and a kettle sits at the edge, ticking.");
        AddDetail("skins,skin,hides,frames,pegs", "Deer, fox, and one huge boar hide are pegged out on frames, scraped clean and rubbed with something that stinks. The boar skin is the size of a door.");
        AddDetail("venison,haunches,meat,smoke,smoking,smokehouse", "The haunches hang in blue smoke over a pit of smouldering hickory, drying to leather. The smell makes your stomach turn over, in a hungry way.");
        AddDetail("antlers,antler,rack,horns", "The antlers are heaped against a tree by the dozen, branching and white, the tines still stained from the velvet. Someone has started to carve one into a handle.");
        AddDetail("tracks,boot-prints,drag marks,prints,trails", "Boot-prints cross the clearing in every direction, and the drag marks of carcasses run to and from the north and east. One set of small prints leads west and does not return.");
        AddReadable("tally,tally board,board,bark board,hunters tally", "A slab of bark propped against the lean-to is scratched with tallies in four hands, with names beside them:\n\nDELWYN. Deer, many. Boar, three.\nELOWEN. Deer, many. Hare, past counting. Spider, one (never again).\nMORWENNA. Deer, boar, wolf, two goblin. Her marks stop at the leaf-fall.\n\nBeneath the last mark, someone has drawn a small tree with a line through it, and a question mark.", "A bark slab scratched with tallies leans against the lean-to. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/delwyn");
        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/elowen");
    }
}
