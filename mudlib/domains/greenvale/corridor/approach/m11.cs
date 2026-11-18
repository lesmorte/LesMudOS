// @inherit /std/room

public class OaM11 : Room
{
    public override void Create()
    {
        ShortDesc = "The Approach Road, the Carters' Mile";
        LongDesc = "The last mile before the city is a broad stone road crowded with carters' yards, wheelwrights' sheds and the long low roofs of drovers' stables. Everything here is built for wagons: wide gateways, tall doors, troughs at every corner and hoof-scarred cobbles puddled with water. The wall ahead now fills the northern sky, and you must lean back to find the top of it. The noise of the road thickens toward a deep, continuous roar that can only be the gate.";
        Exits["south"] = "/domains/greenvale/corridor/approach/m10";
        Exits["north"] = "/domains/greenvale/vale_road/approach";
        ResetSeconds = 120;

        AddDetail("wall,city wall,sky,towers", "At this distance the wall is no longer a line but a cliff, grey and streaked, with square towers crowning it and small figures walking the parapet between them. The size of it sinks in all at once.");
        AddDetail("carters,carters' yards,yards,stables,sheds", "Carters' yards line both sides, their gates open on cobbled courts where wagons are backed in for loading. Men in leather aprons call to each other across the lane, and everyone is in a hurry.");
        AddDetail("wheelwright,wheelwrights,sheds,wheels,spokes", "A wheelwright's shed stands open to the road, with a dozen wheels hanging from its beams like enormous clocks. The wheelwright is fitting an iron tyre with a roaring fire and a lot of shouting.");
        AddDetail("troughs,trough,water,cobbles,puddles", "Stone troughs stand at every corner, long enough for a team, scummed with straw and sky. The cobbles around them are polished by hooves.");
        AddDetail("noise,roar,gate,sound", "A great single sound, made of wheels, hooves, hawkers, hammers and bells, rolls down the road from the gate. It does not seem to come from anywhere in particular.");
        AddDetail("dung,smell,smells,dust", "Dung, dust, hot iron and horse sweat hang over the mile in a rich brown cloud. It is not a pleasant smell, and for some reason it is the first one that makes you think of home.");
        AddReadable("last milestone,milestone,stone,marker", "OAKENFELL, THE SOUTHGATE, HALF A MILE\nKeep to the right with loaded wagons. Riders to walk their beasts under the arch.\nFrom Strong Roots Grow Great Things", "The last milestone stands at the corner of the nearest yard. You could {cmd}read{/} it.");
    }
}
