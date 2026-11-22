// @inherit /std/trainer

public class Harland : Trainer
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
        MaxVitality = 30;

        AddChat("Started with rats years ago. Then goblins moved in. Now something's organizing them.");
        AddChat("Vincent used to talk about tunnels beneath the village. Nobody believed him.");
        AddChatAfterQuest("clear-the-cellar", "The cellar is quiet at last, and I have you to thank for it. The Hollowman's Rest will not forget what you did down there, friend.");
        AddTopic("cellar,goblins,goblin,rats,rat", "Rats, then goblins, and now something is organizing the goblins. I've paid two men to look down there, and neither came back up talking.");
        AddTopic("vincent,tunnels,tunnel,underground", "Vincent used to talk about tunnels under the village, always hungry to dig further. Nobody believed him, and I'm not sure I do yet.");
        AddTopic("bed,room,rest,sleep", "A bed for the night is five gold, and you'll wake up fit as a fiddle. Say the word and I'll show you upstairs myself. Just {cmd}buy bed{/}.");
        AddTopic("food,drink,ale,stew", "Hot stew, cold ale, and a warm corner by the fire. Browse my shelves and take your pick. Try {cmd}browse{/}, then {cmd}buy stew{/}.");
        AddTopic("berry,berries,blackberries,blueberries,juice,flour,honey,water,work,craft,learn,teach,teaching,recipe,recipes,lessons,lesson,fire,cooking,kitchen", "The fire is free to anyone who can use it, so long as you stay out of my stew. I'll show you how to press berry juice if you like: {cmd}lessons{/} for the price, {cmd}learn berry juice{/} to be taught, then {cmd}craft berry juice{/} at the hearth. I sell blackberries, blueberries, flour, honey and clean water for it, along with the food and drink. {cmd}browse{/} will show you.");
        AddEmote("Harland polishes a mug with his apron and eyes the trapdoor.", 6);
        AddEmote("Harland calls out to the musicians to play something cheerier.", 4);

        AddStock("/domains/hollow/obj/stew", 4);
        AddStock("/domains/hollow/obj/ale", 2);
        AddStock("/domains/hollow/obj/tavern_bed", 5);
        AddStock("/domains/hollow/obj/craft/blackberries", 2);
        AddStock("/domains/hollow/obj/craft/blueberries", 2);
        AddStock("/domains/hollow/obj/craft/flour", 1);
        AddStock("/domains/hollow/obj/craft/honey", 3);
        AddStock("/domains/hollow/obj/craft/clean_water", 1);
        AddLesson("berry_juice", 3);
    }

    protected override bool OnBuy(ObjectRef buyer, string path)
    {
        if (path != "/domains/hollow/obj/tavern_bed") return base.OnBuy(buyer, path);
        buyer.Call("Heal", 9999);
        buyer.Call("FillResources");
        TellObject(buyer, "Harland shows you upstairs. You sleep soundly and wake fully rested.\n");
        var tavern = Environment();
        if (tavern != null) tavern.Call("MoveThrough", buyer, "up");
        return true;
    }
}
