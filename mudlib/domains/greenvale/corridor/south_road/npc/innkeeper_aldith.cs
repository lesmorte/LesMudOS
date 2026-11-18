// @inherit /std/shopkeeper

public class SrInnkeeperAldith : Shopkeeper
{
    public override void Create()
    {
        Name = "Aldith";
        Ids.Add("aldith");
        Ids.Add("innkeeper");
        Ids.Add("landlady");
        Ids.Add("host");
        Ids.Add("woman");
        Ids.Add("hostess");
        ShortDesc = "Aldith, the innkeeper";
        LongDesc = "A stout, rosy woman in a spotless apron with a ring of keys on her belt and a ladle in her fist. Her grey-blonde hair is pinned in a bun, her sleeves are rolled to the elbow, and she can find a dirty boot at forty paces.";
        Level = 3;
        MaxHp = 40;

        AddChat("Welcome to the Rest. Hot food, a clean bed and water for your horse. Nothing else, and nothing more.");
        AddChat("Try {cmd}browse{/} if you're hungry, and {cmd}buy bed{/} if you're weary.");
        AddChat("Everyone who passes this yard is a guest. That's the rule. Nobody asked me who made it.");
        AddTopic("bed,room,sleep,rest", "Five gold and you'll wake whole as a new loaf. Say the word: {cmd}buy bed{/}, and I'll show you a corner by the fire. Sleep here without fear; nobody's drawn a blade in this yard in thirty years.");
        AddTopic("food,drink,meal,stew,pasty,bread,cheese", "Bread and cheese for the hurried, a mutton pasty for the hungry. {cmd}browse{/} and {cmd}buy{/} whatever takes your fancy.");
        AddTopic("road,north,wood,forest,eldergreen", "The road is fine to the bridge. Beyond, you're in the Eldergreen. Take bread for the road, and a candle, and a friend if you can find one.");
        AddTopic("rumor,rumors,news,gossip,travelers", "Ask the three in the yard. Ives drives cattle, Jocosa sells ribbons, and Kenrick has seen more of the world than is good for him. They all talk for a drink.");
        AddTopic("inn,traveler's rest,history", "The Rest has stood here three hundred years, they say. The road was cut around it, not the other way. There's always been someone here, with a lamp lit.");
        AddTopic("riverwatch,rangers,ranger,notice", "The Rangers' notice is by the door. I put it up myself. Heed it. They know what they're writing.");
        AddTopic("safe,log out,logout,leave", "You're safe in this yard. Sleep, stay, or log out. Nobody will trouble you, and your things will be where you left them.");
        AddEmote("Aldith wipes down the bench under the ash and straightens a stool.", 8);
        AddEmote("Aldith sets a steaming bowl in front of a dozing drover.", 6);

        AddStock("/domains/greenvale/corridor/south_road/obj/bread_and_cheese", 3);
        AddStock("/domains/greenvale/corridor/south_road/obj/mutton_pasty", 6);
        AddStock("/domains/greenvale/corridor/south_road/obj/rest_bed", 5);
    }

    protected override bool OnBuy(ObjectRef buyer, string path)
    {
        if (path != "/domains/greenvale/corridor/south_road/obj/rest_bed") return base.OnBuy(buyer, path);
        buyer.Call("Heal", 9999);
        TellObject(buyer, "Aldith points you to a settle by the kitchen fire, under a heavy blanket, and you sleep as sound as a stone. You wake rested and whole, with the smell of bread in your nose.\n");
        return true;
    }
}
