// @inherit /std/npc

public class GqScholar : Npc
{
    public override void Create()
    {
        Name = "The visiting scholar";
        Ids.Add("scholar");
        Ids.Add("visiting scholar");
        Ids.Add("man");
        Ids.Add("thin man");
        ShortDesc = "a visiting scholar";
        LongDesc = "A thin, slightly stooped man in a dark, well-brushed coat, with a notebook open before him and a pair of brass dividers in his hand. His face is pleasant and forgettable, and his eyes are quick and attentive. He gives the impression of someone who takes in everything and gives out very little.";
        Level = 10;
        MaxVitality = 45;

        AddChat("Forgive me. You have the look of someone who knows the city. Tell me: which part of the Founder's District would you say is the oldest?");
        AddChat("The books all agree the oak fell. None of them says what was done with it afterward. Does that seem strange to you?");
        AddChat("Do you know why some of the old walls in the Founder's District are built so much thicker than the houses they surround? Has anyone ever told you?");
        AddChat("I am only a visiting scholar, of no importance. But I find that the interesting things in any city are always in the oldest places. Do you not think so?");
        AddTopic("walls,wall,old walls,stone,stones,thick", "The old walls of the Founder's District? You have seen them? Which streets? Do they follow the same line as the houses, or do they ignore them? And have you noticed whether any of them have been closed up, or grown over?");
        AddTopic("oak,great oak,tree,stump,remains,wood", "What became of the oak? Was it cut up, carted off, burned to nothing? The accounts say it fell, and they say the settlers rebuilt around it, and then they stop. Would they not have written down what was left? Does anyone remember? Do you?");
        AddTopic("founders,founder,founders district,founder's district,oldest", "The oldest streets. Have you walked them? Who lives there? Do any of them speak of what lies behind the walls, or beneath the lanes?");
        AddTopic("beneath,underneath,below,cellars,passages,tunnels,roots", "Beneath? Have you heard anything about what lies under the old streets? Cellars older than the streets themselves? You have not? Are you certain?");
        AddTopic("who are you,yourself,your name,faction,order,guild,who you", "I am a visiting scholar. No more. I belong to my notebooks, and to the Athenaeum's good graces. What would you like to know about the city?");
        AddTopic("circle,circle of ash,ash", "A circle? Of what, precisely? I confess I do not follow. I study old stone and older records. Have you read any of them?");
        AddTopic("books,library,athenaeum,histories", "I have read the four little works on the reading table. Have you? Did they not seem to you rather quiet on the matter of the oak's end?");
        AddEmote("The visiting scholar measures something on his map with a pair of brass dividers and writes down a number.", 9);
        AddEmote("The visiting scholar glances up, studies you for a moment, and then returns to his notes.", 6);
    }
}
