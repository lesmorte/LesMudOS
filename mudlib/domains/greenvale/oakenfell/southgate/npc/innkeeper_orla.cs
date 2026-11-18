// @inherit /std/shopkeeper

public class SgInnkeeperOrla : Shopkeeper
{
    public override void Create()
    {
        Name = "Orla Penhallow";
        Ids.Add("orla");
        Ids.Add("innkeeper");
        Ids.Add("landlady");
        Ids.Add("orla penhallow");
        Ids.Add("host");
        ShortDesc = "Orla Penhallow, the innkeeper";
        LongDesc = "A broad-shouldered woman of fifty, with iron-grey hair under a white cap and sleeves rolled to the elbow. She runs the Silver Stag like a captain, and her glance finds a dirty tankard at forty paces.";
        Level = 3;
        MaxHp = 40;

        AddChat("Welcome to the Silver Stag. Food's hot, ale's cold, beds are clean. Everything else is your own business.");
        AddChat("Try {cmd}browse{/} if you're hungry, and {cmd}buy bed{/} if you're weary.");
        AddChat("Half of Greenvale's heroes drank at this bar. A good few of them even paid.");
        AddChat("If you hear a rumor, share it. If you've a tale, tell it. That's the house rule.");
        AddTopic("bed,room,rest,sleep", "Eight gold a night, and you'll wake whole as a new loaf. Say the word: {cmd}buy bed{/}, and I'll show you upstairs.");
        AddTopic("food,drink,ale,stew,meal", "Venison stew from the spit and Stag Amber, our own brew. Try {cmd}browse{/}, then {cmd}buy stew{/}.");
        AddTopic("rumor,rumors,news,gossip", "Ask the three by the fire. Old Wick, Dessa and Fenn know more than the Assembly's spies, and they'll tell you for a drink.");
        AddTopic("stag,inn,history", "The Silver Stag's been on this corner longer than the Southgate has had a name. The stag's head over the mantel, now, the regulars say the first owner took it with a single arrow, and the arrow gets longer every year.");
        AddTopic("quest,work,job,jobs,adventure", "Read the board by the stairs. Posters come and go. If you're quick, and capable, you'll find coin in it.");
        AddEmote("Orla Penhallow wipes down a table and shoos a cat from the bar.", 8);
        AddEmote("Orla Penhallow tops up a drover's tankard without being asked.", 6);

        AddStock("/domains/greenvale/oakenfell/southgate/obj/stag_stew", 6);
        AddStock("/domains/greenvale/oakenfell/southgate/obj/stag_amber", 3);
        AddStock("/domains/greenvale/oakenfell/southgate/obj/stag_bed", 8);
    }

    protected override bool OnBuy(ObjectRef buyer, string path)
    {
        if (path != "/domains/greenvale/oakenfell/southgate/obj/stag_bed") return base.OnBuy(buyer, path);
        buyer.Call("Heal", 9999);
        TellObject(buyer, "Orla shows you upstairs to a clean room with a window over the street. You sleep soundly and wake fully rested.\n");
        var inn = Environment();
        if (inn != null) inn.Call("MoveThrough", buyer, "up");
        return true;
    }
}
