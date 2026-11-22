// @inherit /std/npc

public class MkWatchHugh : Npc
{
    public override void Create()
    {
        Name = "Hugh Bellamy";
        Ids.Add("hugh");
        Ids.Add("hugh bellamy");
        Ids.Add("watchman");
        Ids.Add("watch");
        Ids.Add("guard");
        ShortDesc = "Hugh Bellamy, a Westgate watchman";
        LongDesc = "A grim, middle-aged man in a grey tabard and a dented helm, a crossbow slung across his shoulder. His eyes keep returning to the western road, as if expecting something.";
        Level = 4;
        MaxVitality = 50;

        AddChat("Westgate's quiet. Too quiet, if you ask me.");
        AddChat("The road beyond is not open to travelers. Not yet. Not to those who value their skins.");
        AddChat("Check your gear and your nerve. You'll need both.");
        AddTopic("road,west,westwood road,elderwood", "The western road leads to the Elderwood and is held by the Watch and the Riverwatch. We turn back wagons and travelers both. Too many have not returned.");
        AddTopic("thorn pact,pact,druids", "Officially absent from Oakenfell. Unofficially, I couldn't say. I only watch the road.");
        AddTopic("trouble,wagons,burned,logging", "Burned wagons, felled trees across the road, scorched canvas. We have found tracks and arrows, but no one willing to say whose.");
        AddTopic("gate,westgate,watch", "I've held the Westgate for twelve years. Never has it seen such a season.");
        AddEmote("Hugh Bellamy peers down the western road and frowns.", 8);
        AddEmote("Hugh Bellamy shifts his crossbow to the other shoulder.", 6);
    }
}
