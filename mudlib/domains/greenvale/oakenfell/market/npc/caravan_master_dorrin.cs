// @inherit /std/npc

public class MkCaravanMasterDorrin : Npc
{
    public override void Create()
    {
        Name = "Dorrin Stavely";
        Ids.Add("dorrin");
        Ids.Add("dorrin stavely");
        Ids.Add("caravan master");
        Ids.Add("caravan");
        Ids.Add("master");
        ShortDesc = "Dorrin Stavely, the caravan master";
        LongDesc = "A stern woman of fifty, with a bunch of heavy keys at her belt and a face weathered by sun and sleeplessness. She wears a plain blue coat with a scale badge, and she looks at everything as if counting it.";
        Level = 4;
        MaxVitality = 50;

        AddChat("Keep clear of the wagons. Mind the hooves.");
        AddChat("Four wagons held at the Westgate, two turned back, one burned. I've lost count of the rest.");
        AddChat("If you want a seat on a caravan, you'll need a name. If you want a name, you'll need a deed.");
        AddTopic("routes,route,destinations,caravan,caravans", "We run to Watchfort Crossing, the mountain passes, the river towns and, when it is safe, the Elderwood road. The Elderwood road is held.");
        AddTopic("western road,west,elderwood,trouble", "Wagons turned back near the Elderwood, loggers' camps burned, trade roads cut with fallen trees. Some say the Pact, some say bandits, some say both. I say I've lost more wagons this season than in the last three.");
        AddTopic("pact,thorn pact,druids", "The Consortium says the Pact. The Riverwatch says extremists within it. I say it's not for a carter to say, but I'd pay well for an honest scout.");
        AddTopic("work,job,guard,guards,hire", "I need guards, but I need names that I can trust. Prove yourself in the field, then talk to me.");
        AddTopic("passage,seat,travel", "Passage on a caravan is for known customers. Come back when you're known.");
        AddEmote("Dorrin Stavely checks a manifest and crosses something out with a stub of chalk.", 8);
        AddEmote("Dorrin Stavely shouts to the boy to bring the ledger.", 6);
    }
}
