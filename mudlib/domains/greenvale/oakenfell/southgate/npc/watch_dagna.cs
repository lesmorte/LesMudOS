// @inherit /std/npc

public class SgWatchDagna : Npc
{
    public override void Create()
    {
        Name = "Dagna Rusk";
        Ids.Add("dagna");
        Ids.Add("watchwoman");
        Ids.Add("watch");
        Ids.Add("dagna rusk");
        Ids.Add("guard");
        ShortDesc = "Dagna Rusk, a gate watchwoman";
        LongDesc = "A tall, quiet woman with a straight back and a scar through one eyebrow. She stands at the gate as if she were part of the stone, and she misses nothing.";
        Level = 4;
        MaxHp = 50;

        AddChat("Mm.");
        AddChat("Move along.");
        AddTopic("city,oakenfell", "Big. Mind it.");
        AddTopic("hobb,watchman", "Talks.");
        AddTopic("trouble,danger,crime,thief,thieves", "Thieves work the Row. Watch your purse.");
        AddTopic("rules,board", "Read them.");
        AddEmote("Dagna Rusk's eyes follow a man with a bulky coat until he is past.", 10);
        AddEmote("Dagna Rusk says nothing, and the nothing seems to be disapproving.", 8);
    }
}
