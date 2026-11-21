// @inherit /std/npc

public class FdWatchNorthgate : Npc
{
    public override void Create()
    {
        Name = "Halvard Pike";
        Ids.Add("halvard");
        Ids.Add("pike");
        Ids.Add("watchman");
        Ids.Add("watch");
        Ids.Add("guard");
        ShortDesc = "Halvard Pike, a city watchman";
        LongDesc = "A lean, long-faced watchman in a green surcoat marked with a white oak, a long spear in his hand and an old horn at his belt. His eyes are never still, and he has the habit of looking at the horizon when speaking to you.";
        Level = 3;
        MaxVitality = 40;

        AddChat("Northgate is open from dawn bell to dusk bell. Keep to the right with your carts.");
        AddChat("What lies beyond the gate is the Northern Road. I would walk it in daylight, and I would not walk it alone.");
        AddTopic("road,northern road,north,beyond,mountains", "The Northern Road runs up toward the mountains, and there is not much on it but farms, then forest, then rock. It is not yet open to travelers. The watch has had no orders to let anyone pass.");
        AddTopic("watch,gate,duty,city watch", "We keep the gates, we keep the peace in the streets, and we keep our opinions to ourselves. It is a quiet post. Mostly.");
        AddTopic("old stone street,elder lane,lane,oak", "Elder Lane is a dead end, friend. A fine place for a quiet walk, and not much else.");
        AddEmote("Halvard Pike shifts his spear and looks up the Northern Road for a long moment.", 6);
        AddEmote("Halvard Pike checks a cart as it rolls through the arch, nods, and waves it along.", 5);
    }
}
