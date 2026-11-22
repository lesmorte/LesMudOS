// @inherit /std/room

public class HwM07 : Room
{
    public override void Create()
    {
        ShortDesc = "The Hunter's Memorial";
        LongDesc = "A cairn of grey river-stones stands at the side of the road, higher than a man and as wide as a wagon, with a slab of dark slate set into its face and a lean-to of poles and hide built against its flank. Small stones have been laid along the top of the cairn by passers-by, and a Riverwatch pennant of green and grey hangs from a pole beside it. A narrow trail leaves the road to the east, between two trunks marked with a ring cut deep through the bark. The place smells of woodsmoke and old leather, and it is the first sign of a living person you have seen for some time.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m06";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m08";
        Exits["east"] = "/domains/greenvale/corridor/heartwood/w5";
        DangerBand(7, 8);

        AddDetail("cairn,stones,stone,river-stones,river stones,pile", "The cairn is built of rounded stones carried a long way from a river bed, laid dry and without any order that you can see. Some are fresh and white, and others are grey with lichen. Each stone was set there by someone who knew the names.");
        AddDetail("slab,slate,plaque,memorial,inscription,stone slab", "A slab of dark slate is set into the face of the cairn at the height of a man's chest. It is cut with a ring of linked chain at the head and a long inscription below, and the cut letters are filled with moss in places. You could {cmd}read{/} it.");
        AddDetail("lean-to,lean to,station,shelter,hide,poles,camp", "The lean-to is a slope of poles roofed with hides and branches, open on the road side, with a rolled bed, a pot, a bundle of arrows and a lantern hung from a peg. It looks lived in and kept dry, and by someone who is used to being alone.");
        AddDetail("pennant,flag,banner,pole,green and grey", "The pennant is green and grey, faded by weather, and bears a single silver eye. It hangs straight down in the still air.");
        AddDetail("rings,ring,trunks,marks,blaze,blazes,cut", "On two trunks at the trail mouth a ring has been cut through the bark to the wood, and kept open. The Riverwatch blazes the paths it trusts and the ones it watches, and this one is both.");
        AddDetail("trail,east,path,track", "The trail east is narrow and trodden by something that does not mind the dark. It leads between the trunks and is lost to sight in twenty paces.");
        AddDetail("pebbles,small stones,offerings,top", "The small stones on the top of the cairn are not all of one kind. There are flat river pebbles, a bright quartz, a button, an arrowhead and a child's marble. Nobody has taken any away.");
        AddReadable("slab,slate,plaque,memorial,inscription,stone slab", "The inscription reads:\n\nTO THE HUNTERS OF THE RIVERWATCH\nwho went into the Heartwood and did not come out of it.\n\nEDRIC VANE      JOAN PRYOR\nHOB TARRANT      SEFA LUND\nAND ONE WHOSE NAME WAS NEVER KNOWN\n\nThe forest took them. We carried the stones.\nEdric Vane kept the dusk watch for eleven years, and was remembered for his Resolve.\nEVER VIGILANT.\n\nBelow, in newer and shallower letters, a hand has scratched: \"Room left.\"", "A slab of dark slate is set into the cairn. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/npc/patrol_heartwood");
        AddSpawn("/domains/greenvale/corridor/heartwood/npc/cassian");
    }
}
