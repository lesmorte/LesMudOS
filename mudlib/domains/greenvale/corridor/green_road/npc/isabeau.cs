// @inherit /std/npc

public class GrIsabeau : Npc
{
    public override void Create()
    {
        Name = "Isabeau";
        Ids.Add("isabeau");
        Ids.Add("pilgrim");
        Ids.Add("young woman");
        Ids.Add("woman pilgrim");
        ShortDesc = "Isabeau, a pilgrim";
        LongDesc = "A young woman in a plain brown cloak with a pack on her back, a walking staff and a long red ribbon wound round her wrist. Her boots are muddy to the knee, and her face is tired and a little hopeful.";
        Level = 1;
        MaxHp = 15;

        AddChat("I am walking to Oakenfell to find my brother. He took this road in the spring, and I have had no letter.");
        AddChat("I tied my ribbon to the rail. I do not know who it is for, but it feels right.");
        AddChat("The shrine is the first kind place on this road, I think.");
        AddTopic("brother,find,spring,letter", "He went north to work for a carter. The last word was from the Hollow. I tell myself he is busy.");
        AddTopic("shrine,lamp,ribbon,ribbons,rail", "I am told you leave a thing for the ones who did not arrive. I left a ribbon. I do not want to think about why.");
        AddTopic("road,journey,walk,walking,alone", "I joined the caravan at the Edge and it has been kind. They halted at the camp and I walked ahead. I should have waited. It is very dark.");
        AddTopic("goblins,raiders,wood", "I have heard them in the night, little voices. I try to think of them as foxes.");
        AddTopic("oakenfell,city,north", "I have never seen a city. They say you see it first from a hill, with all the roofs. I do not think I will be brave enough to look.");
        AddEmote("Isabeau winds her ribbon round her wrist and unwinds it again.", 6);
        AddEmote("Isabeau bows her head before the lamp for a long moment.", 6);
    }
}
