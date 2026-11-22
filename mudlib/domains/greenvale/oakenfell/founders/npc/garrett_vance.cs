// @inherit /std/npc

public class FdJoryVance : Npc
{
    public override void Create()
    {
        Name = "Garrett Vance";
        Ids.Add("garrett");
        Ids.Add("vance");
        Ids.Add("adventurer");
        Ids.Add("retired adventurer");
        Ids.Add("old man");
        ShortDesc = "Garrett Vance, a retired adventurer";
        LongDesc = "A weathered old man on the stone bench, with a scarred left hand, a stiff left knee and a walking stick he uses mostly to point at things. His cloak is travel-faded green; his boots have been resoled so many times they are more resole than boot.";
        Level = 3;
        MaxVitality = 35;

        AddChat("Thirty years I walked the roads of Greenvale. Now I walk this street, and it is further than it looks.");
        AddChat("Sit a while, friend. The bench is free, and the city will not miss you for a minute.");
        AddChat("Everyone wants to hear about the dragons. There were no dragons. There was a lot of rain.");
        AddTopic("oak,great oak,stump,tree,old city,old oakenfell,grove,hidden grove,old walls", "Ah. You are an observant one, then. The Great Oak fell, aye, but the stump never went. It still stands behind the old walls at the end of Elder Lane, if you know to look. Go up the lane to the end. Look hard at the wall, and the ivy. That is all I will say, and more than I should.");
        AddTopic("adventure,adventurers,roads,roads of greenvale,past", "I carried a borrowed sword out of the Southgate with four friends and came back with a name and one friend. It is a good trade, if you count the name.");
        AddTopic("elder lane,lane,houses", "The oldest houses in the city, and the quietest lane. Nobody lives at the end any more except the ivy.");
        AddTopic("city,oakenfell,founders,street", "I have been here all my life, and I still find new things. The old city is hiding in plain sight. You only have to want it.");
        AddTopic("vigor,vitality,survive,survival,scars,scar,remembered,wounds", "I was remembered, for a short time, for my Vigor. I took more wounds than the other four put together, and kept getting up. It is a poor thing to be remembered for. The ones remembered for their Insight lived longer.");
        AddEmote("Garrett Vance rubs his stiff knee and mutters something about the weather.", 6);
        AddEmote("Garrett Vance taps the pavement with his walking stick and gives the lane to the west a long, thoughtful look.", 5);
    }
}
