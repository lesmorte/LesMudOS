// @inherit /std/npc

public class GqNewcomer : Npc
{
    public override void Create()
    {
        Name = "Wick";
        Ids.Add("newcomer");
        Ids.Add("young man");
        Ids.Add("wick");
        Ids.Add("traveler");
        ShortDesc = "Wick, a nervous newcomer";
        LongDesc = "A lanky young traveler in a road-stained cloak, standing very straight and clutching the strap of a pack that is clearly too new. He keeps looking up the Row toward the Hall, drawing breath as though to start, and letting it go.";
        Level = 3;
        MaxVitality = 25;

        AddChat("That is the Hall of Legends, is it not? I had pictured it smaller. Or no, I had pictured it bigger, and now it is bigger than that.");
        AddChat("I am only going to look. I am nowhere near ready to choose anything. I only want to see the doors, and then I will go and find an inn.");
        AddChat("Do you think they mind if you just walk in and stand there? Everyone looks like they belong.");
        AddTopic("hall,hall of legends,doors", "They say it is where a person decides who they will become. I have only just stopped being a farmhand. I am not sure which of those I am.");
        AddTopic("class,classes,calling,callings,level", "I have heard there is a day when you choose, and that it is for good. I am told it comes when you are stronger, and not before. That is a comfort and a worry in about equal parts.");
        AddTopic("home,hollow,vincent", "Vincent's Hollow. A fine place. A very small place. I had no notion how small until I came over the rise.");
        AddEmote("Wick starts toward the Hall, stops, and straightens his pack strap.", 10);
        AddEmote("Wick mouths something to himself, perhaps a speech, perhaps a prayer.", 6);
    }
}
