// @inherit /std/room

public class OaW2 : Room
{
    public override void Create()
    {
        ShortDesc = "The Bandits' Road";
        LongDesc = "A narrow rutted track runs north beside the line of the aqueduct, hemmed in by hazel and bracken and just wide enough for a laden pack-mule. The ground is trampled flat by many boots, branches have been cut and laid to turn a wagon aside, and a cord stretched at ankle height between two trees runs away into the undergrowth toward a small bell. A smell of woodsmoke and rancid fat hangs in the still air, and every bird in the thicket has gone silent.";
        Exits["south"] = "/domains/greenvale/corridor/approach/w1";
        Exits["north"] = "/domains/greenvale/corridor/approach/w3";
        ResetSeconds = 120;
        DangerBand(8, 9);

        AddDetail("ruts,track,boots,trampled,ground,tracks", "Heavy, hobnailed boots, a dozen pairs at least, have worn the track to bare earth. Between them, deep and narrow, are the grooves of a handcart that has been dragged out heavily loaded.");
        AddDetail("cord,tripwire,wire,string,bell", "A tarred cord, ankle-high, stretched between hazels. Following it with your eye you find the bell, a cracked cow-bell tied in a laurel, and a second cord that leads off north to someone who would hear it.");
        AddDetail("branches,cut branches,barricade,hazel,wagon", "Hazel poles cut and stacked make a crude chicane across the track, so that a wagon cannot pass but a man on foot can. It is exactly what soldiers do to a road they intend to keep.");
        AddDetail("smoke,smell,woodsmoke,air,fat", "Woodsmoke, rancid mutton fat and wet wool: someone is cooking not far ahead, and not carefully.");
        AddDetail("notches,marks,blazes,trees,bark", "Notches have been cut in the bark of the nearest trees at head height, one, two, three, in a rhythm. They are signs for a man who walks this track in the dark.");
        AddDetail("thicket,hazel thicket,bracken,undergrowth", "The hazel and bracken close in tight on both sides. A man could lie in it a yard from the track and never be seen.");

        AddSpawn("/domains/greenvale/corridor/mob/highway_bandit");
    }
}
