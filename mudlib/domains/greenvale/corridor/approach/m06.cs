// @inherit /std/room

public class OaM06 : Room
{
    public override void Create()
    {
        ShortDesc = "The Approach Road, under the Old Aqueduct";
        LongDesc = "The road runs beneath a tall, pointed arch of dark green-grey stone, one of a line of piers and arches that strides across the land from the west and breaks off in the fields to the east. The stone is older than anything near it, black at the foot, bearded with ivy and fern, and the air beneath the vault is cool and smells of wet rock. Swifts scream in and out of the arch, wagons pass through it as if through a gate, and the line of arches runs off into the trees, plainly broken, having once brought water from somewhere to somewhere else.";
        Exits["south"] = "/domains/greenvale/corridor/approach/m05";
        Exits["north"] = "/domains/greenvale/corridor/approach/m07";
        ResetSeconds = 120;
        DangerBand(6, 8);

        AddDetail("arch,vault,arches,old aqueduct,aqueduct", "The arch is as tall as three wagons stacked and narrow for its height, rising to a point, its blocks the size of ploughshares and fitted without mortar. Those who built it did not stint, and did not ask permission; Oakenfell is nearly four centuries old, and this has plainly stood longer than that.");
        AddDetail("piers,pier,foot,base,masonry,blocks", "The piers are laid of huge blocks fitted so close that you could not slip a knife between them, with no trace of mortar. Round each at head height runs a band of shallow marks, weathered nearly smooth.");
        AddDetail("marks,band,band of marks,carvings,signs", "The marks are small, regular and repeated, a ring crossed by a bar, a wavering line like water, a ring with a gap in it, and they have worn so shallow that you feel them more than see them. They are not letters of any hand you know, and they are not masons' marks of the city.");
        AddDetail("ivy,fern,ferns,moss,growth", "Ivy hangs in dark green curtains from the spandrels, and ferns grow from every crack that holds a pinch of soil. Over the centuries the stone has become a kind of cliff.");
        AddDetail("swifts,birds,nests", "Swifts nest in the high joints of the stone. They hurtle through the arch just over the heads of the carters, who ignore them completely.");
        AddDetail("line,broken line,span,spans,trees,east,west", "To the west the arches march away into the wood, one after another, until they are lost among the trunks. To the east they end abruptly in broken piers in a field, and nothing now stands to carry the water onward.");
        AddReadable("plaque,bronze plaque,brass plaque,notice", "THIS ARCH IS KEPT SOUND FOR THE SAFETY OF TRAVELERS BY ORDER OF THE GREENVALE ASSEMBLY. Do not climb the piers. Do not tie beasts to the stonework. The Assembly claims no knowledge of its builders and no right to its water.", "A bronze plaque has been screwed to the foot of the nearest pier. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/highway_bandit", 10);
    }
}
