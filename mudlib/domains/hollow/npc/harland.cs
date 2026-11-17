// @inherit /std/shopkeeper

public class Harland : Shopkeeper
{
    public override void Create()
    {
        Name = "Harland";
        Ids.Add("harland");
        Ids.Add("innkeeper");
        Ids.Add("harland the innkeeper");
        ShortDesc = "Harland the Innkeeper";
        LongDesc = "A broad, red-faced man with a grey beard tied in a knot and a stained apron over his barrel chest. He keeps the Hollowman's Rest with a firm hand and a ready smile, and he has a nervous glance for the trapdoor in the floor.";
        Level = 2;
        MaxHp = 30;

        AddChat("Started with rats years ago. Then goblins moved in. Now something's organizing them.");
        AddChat("Vincent used to talk about tunnels beneath the village. Nobody believed him.");
        AddTopic("cellar,goblins,goblin,rats,rat", "Rats, then goblins, and now something is organizing the goblins. I've paid two men to look down there, and neither came back up talking.");
        AddTopic("vincent,tunnels,tunnel,underground", "Vincent used to talk about tunnels under the village, always hungry to dig further. Nobody believed him, and I'm not sure I do yet.");
        AddTopic("bed,room,rest,sleep", "A bed for the night is five gold, and you'll wake up fit as a fiddle. Say the word and I'll show you upstairs myself.");
        AddTopic("food,drink,ale,stew", "Hot stew, cold ale, and a warm corner by the fire. Browse my shelves and take your pick.");
        AddEmote("Harland polishes a mug with his apron and eyes the trapdoor.", 6);
        AddEmote("Harland calls out to the musicians to play something cheerier.", 4);

        AddStock("/domains/hollow/obj/stew", 4);
        AddStock("/domains/hollow/obj/ale", 2);
        AddStock("/domains/hollow/obj/tavern_bed", 5);
    }

    protected override bool OnBuy(ObjectRef buyer, string path)
    {
        if (path != "/domains/hollow/obj/tavern_bed") return base.OnBuy(buyer, path);
        buyer.Call("Heal", 9999);
        TellObject(buyer, "Harland shows you upstairs. You sleep soundly and wake fully rested.\n");
        var tavern = Environment();
        if (tavern != null) tavern.Call("MoveThrough", buyer, "up");
        return true;
    }
}
