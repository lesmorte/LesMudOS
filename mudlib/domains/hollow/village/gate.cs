// @inherit /std/room

public class VillageGate : Room
{
    public override void Create()
    {
        ShortDesc = "Village Gate";
        LongDesc = "A timber palisade surrounds Vincent's Hollow. The village gates stand open during daylight hours, allowing visitors to come and go freely. Watchtowers overlook the road while faded banners flutter in the wind.";
        Exits["north"] = "/domains/hollow/village/north_road";
        Exits["south"] = "/domains/hollow/village/square";
        Safe = true;

        AddDetail("palisade,stockade,timber,logs", "Sharpened timber logs, each as thick as a man's body, lashed together and set deep in the earth. Weather and years have silvered them, but they stand firm.");
        AddDetail("gates,gate,village gates", "Two heavy timber gates stand wide open on iron hinges. Their bars rest in brackets on the wall, ready to be dropped at nightfall.");
        AddDetail("watchtowers,towers,watchtower,tower", "Two timber watchtowers rise above the palisade, one on either side of the gate. Their platforms give a view of the road for miles.");
        AddDetail("banners,banner,flags", "Faded banners hang from poles above the gate. Their colors have bleached to a dull grey-green, but the crest of a spreading oak can still be made out.");

        AddSpawn("/domains/hollow/npc/guard");
    }
}
