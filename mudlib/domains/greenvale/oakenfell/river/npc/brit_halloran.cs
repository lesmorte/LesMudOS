// @inherit /std/npc

public class RvBritHalloran : Npc
{
    public override void Create()
    {
        Name = "Brit Halloran";
        Ids.Add("brit");
        Ids.Add("halloran");
        Ids.Add("dockhand");
        Ids.Add("docker");
        Ids.Add("woman");
        ShortDesc = "Brit Halloran, a dockhand";
        LongDesc = "A wiry young woman with her sleeves rolled to the shoulder and a coil of rope slung across her chest. She moves along the planks like a cat on a fence and laughs at the clumsy.";
        Level = 2;
        MaxVitality = 25;

        AddChat("Stand clear, if you please, or I will have to throw you in.");
        AddChat("I wanted to be a ranger once. Then I saw the pay and the weather, and chose barrels.");
        AddTopic("ranger,rangers,riverwatch,watch", "They keep a post by the old street, with a green door. Fine people, though they never smile. They are looking for something, all of them, and I would not like to know what.");
        AddTopic("work,docks,quay,job", "Mooring, unmooring, lifting and cursing. Dockhands get paid by the load, so I carry fast and curse faster.");
        AddTopic("harbormaster,rosamund,boards,board", "Harbormaster Kell? The best in the Mere, and the strictest. Tie up in the wrong place and you will hear about it for a week.");
        AddEmote("Brit Halloran tosses a line across a gap and makes it fast with a quick hitch.", 6);
        AddEmote("Brit Halloran hops onto a barge deck and back again, light as a leaf.", 5);
    }
}
