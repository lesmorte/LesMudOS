// @inherit /std/npc

public class GqDoorwarden : Npc
{
    public override void Create()
    {
        Name = "Hild";
        Ids.Add("doorwarden");
        Ids.Add("warden");
        Ids.Add("hild");
        Ids.Add("woman");
        ShortDesc = "Hild, the doorwarden of the Hall";
        LongDesc = "A tall, grey-braided woman in a long oak-green coat with the Hall's emblem stitched on the shoulder, standing beside the great doors with a staff she has never been seen to use. She has the unhurried courtesy of someone who has explained the Hall to a thousand newcomers and enjoyed it each time.";
        Level = 15;
        MaxHp = 90;

        AddChat("Welcome to the Hall of Legends, traveler. Come in, come in. Through these doors is the Hall of Callings, the heart of the place; to its east is the Chronicle Gallery, and to its west the Envoys' Gallery.");
        AddChat("If you have not yet reached your fifth level, you are no less welcome. Look at everything, listen to everyone. The hallmasters like nothing better than a visitor who is curious early.");
        AddChat("At the fifth level a member of any calling may choose a path. Until then, learn what you can, and come in as often as you like.");
        AddChat("Mind the steps going up. Everyone looks at the roof and forgets them.");
        AddTopic("hall,hall of legends,building", "Built over generations by the five callings, piece by piece, joined so well that you cannot find where one builder stopped. It is not quite one building. It is certainly one hall.");
        AddTopic("callings,calling,hall of callings,hallmasters,hallmaster,classes,class,paths", "The Hall of Callings is just ahead of you. Five hallmasters stand there, one for each calling: Warrior, Scout, Priest, Mage and Adventurer. At the fifth level, one of your calling may choose among three paths. Ask the hallmaster of your own calling, or any of them out of curiosity; they will not mind.");
        AddTopic("level,level 5,fifth,five,visit,visitors,look,listen", "Below the fifth level, you are welcome to look, to listen and to ask. No one is turned away from these doors for being young. The choosing itself waits for the fifth level, but the learning does not.");
        AddTopic("chronicle,gallery,chronicle gallery", "Through the Hall of Callings, then east. The Grand Chronicle stands there, and the carved walls of heroes, legends and infamy. A chronicler will tell you how it works.");
        AddTopic("envoys,envoy,envoys gallery,factions,faction", "Through the Hall of Callings, then west. The envoys of several powers of Greenvale keep their desks there, and they like to be asked questions.");
        AddTopic("motto,greatness,chosen", "Greatness Is Chosen. It is cut over the door. Mind you, the founders did not say that greatness is given, or that it is easy. Only that it is chosen.");
        AddTopic("oak,great oak,city,oakenfell", "From Strong Roots Grow Great Things, as the city says. The Hall grew out of the same roots, I like to think.");
        AddEmote("Hild greets a passing visitor by name and sends them in with a nod.", 10);
        AddEmote("Hild shifts her staff to the other hand and surveys the Row.", 7);
    }
}
