// @inherit /std/room

public class HwM09 : Room
{
    public override void Create()
    {
        ShortDesc = "The Heartwood Road, the Paved Remnant";
        LongDesc = "For a hundred paces the road climbs onto paving: great flat slabs of dark stone laid edge to edge under the dirt, scoured clean by traffic and laid bare in the middle. The slabs are huge, each one a long man's height, and they have been set so true that the road has not shifted in all the years since. A low kerb runs on each side, buried in moss, and a shallow gutter along the east. At the end of the paving the dirt resumes, and the road forgets it was ever anything else.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m08";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m10";

        AddDetail("paving,slabs,slab,flagstones,flags,stone,stones", "The slabs are of the same green-grey stone as the arch. Each is cut to a long rectangle and fitted to the next without mortar. They are laid in a pattern of long and short that repeats every seventh course. The middle of the road is worn into a shallow trough by feet, not by wheels, and the worn part is a pace wide.");
        AddDetail("kerb,kerbs,kerbstone,edge,edges", "A low kerb of squared stone lines each side, half buried and green with moss. It is cut with a bevel that no mason of Oakenfell would put on a kerb, and which sheds water neatly in either direction.");
        AddDetail("gutter,drain,channel,water", "The gutter on the east side is a groove a hand wide, still running in wet weather. It carries the rain off the paving and into the roots, and it has done so for longer than there has been a city.");
        AddDetail("dirt,earth,soil,road,end", "At either end the dirt of the road has drifted over the slabs in a long tongue. The edge of the paving there is cleanly cut, as though it had been laid on purpose to stop.");
        AddDetail("wear,trough,worn,feet,middle", "The trough in the paving has a smooth, shallow shape, deepest where a person would walk. It holds rainwater after a shower. To wear stone so, many feet must have walked this way for a very long time.");
        AddDetail("trees,trunks,roots,roots under", "The roots of the nearest trees have lifted the edge of the paving a finger's width in places, and no more. Even the oldest of them seems to have been careful of it.");
        AddSpawn("/domains/greenvale/corridor/heartwood/npc/lysander");
    }
}
