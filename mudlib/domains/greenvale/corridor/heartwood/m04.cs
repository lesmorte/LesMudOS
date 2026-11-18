// @inherit /std/room

public class HwM04 : Room
{
    public override void Create()
    {
        ShortDesc = "The Heartwood Road, the Moss Mile";
        LongDesc = "For a mile the moss has had its way. It covers the banks of the road, the fallen trunks along it and every boulder, and it has crept in from the edges until the wheel-ruts are the only bare earth. Water drips somewhere out of sight, slow and regular, and the sound is the only one there is. The trees are the same great pillars as before, but the green light is thicker here, and the air feels damp against the skin.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m03";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m05";

        AddDetail("moss,green,carpet,fur", "The moss is of several kinds: a deep dark green on the roots, a yellow-green on the stones, and a grey mat on the dead wood. It is so thick that a hand pressed into it vanishes to the knuckle, and the print fills slowly again.");
        AddDetail("trunks,trunk,deadfall,fallen trunks,logs,log", "The fallen trunks have lain so long that they are only long humps of moss with a hollow in the middle. One has a young beech growing from its back, taller than you are.");
        AddDetail("boulders,boulder,rocks,rock,stones", "The boulders are rounded and grey, scattered at random along the bank. Under the moss on one, you find a flat face that has been cut square, and then the moss closes over it again.");
        AddDetail("ruts,rut,road,earth,ground,wheel-ruts", "The ruts are old and half filled with leaf. They run straight, and the water that stands in them is clear and cold.");
        AddDetail("water,drip,dripping,sound", "The dripping comes from the left of the road, from a branch or a root or a stone you cannot see. It keeps a slow time of its own.");
        AddDetail("air,damp,light,mist", "The air is wet enough to bead on your sleeve. In the green light it has a faint, still look, as if the mile were underwater.");
        AddEncounter("/domains/greenvale/corridor/mob/heartwood_goblin", 8);
    }
}
