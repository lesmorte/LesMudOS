// @inherit /std/npc

public class OaWagonMaster : Npc
{
    public override void Create()
    {
        Name = "Baldric Teague";
        Ids.Add("baldric");
        Ids.Add("teague");
        Ids.Add("baldric teague");
        Ids.Add("wagon master");
        Ids.Add("wagonmaster");
        Ids.Add("master");
        Ids.Add("caravan master");
        ShortDesc = "Baldric Teague, the wagon master";
        LongDesc = "A barrel-chested man with a grey-shot beard, a whip coiled at his belt and a blue coat gone green at the seams. He walks the rows with a limp and a stick, and every wagon he passes seems to straighten a little.";
        Level = 5;
        MaxHp = 60;

        AddChat("Row two, hitch up! The road does not shorten for those who sleep late.");
        AddChat("A caravan is only as quick as its slowest axle, and that axle is always mine.");
        AddChat("If you want a seat on a wagon, speak to me. If you want a guard's wage, sign on, but I'll want to see your weapon first.");
        AddTopic("caravan,caravans,wagons,forming,forming up", "We form up here, in rows, with the oxen nearest the road. When the bell goes we go south together, on the Heartwood road, with a Ranger's escort for the dear loads.");
        AddTopic("south,hollow,vincent's hollow,heartwood,road south", "Down to the Hollow in three days with a heavy load, two with light. Past the Heartwood the trees take the wagons and the wolves take the stragglers, so we keep close.");
        AddTopic("guards,guard,hire,work,job,sign on", "I always want guards, and I always want ones who will not run. Prove yourself on the road, and Dorrin Stavely at the Caravan Office in the city will talk to you about the long hauls. For this row I have all I need.");
        AddTopic("bandits,bandit,camp,aqueduct,west,toll book", "We lose a wagon a month to them. They know our times. That is what troubles me; they know which row has the Ranger and which does not. Somebody talks.");
        AddTopic("consortium,merchant consortium,scales", "Trade builds tomorrow, says the banner. It builds my sciatica today.");
        AddTopic("oakenfell,city", "Oakenfell is the reason any of this makes money. Behind that wall there are ten thousand people who want what I carry.");
        AddEmote("Baldric Teague raps a wagon wheel with his stick and listens.", 8);
        AddEmote("The wagon master bellows a number, and a team begins to move.", 8);
    }
}
