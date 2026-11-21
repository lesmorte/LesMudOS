// @inherit /std/shopkeeper

public class OaFarmWife : Shopkeeper
{
    public override void Create()
    {
        Name = "Griselda Hobday";
        Ids.Add("griselda");
        Ids.Add("hobday");
        Ids.Add("griselda hobday");
        Ids.Add("farm wife");
        Ids.Add("farmwife");
        Ids.Add("wife");
        Ids.Add("seller");
        Ids.Add("baker");
        Ids.Add("shopkeeper");
        ShortDesc = "Griselda Hobday, a farm wife";
        LongDesc = "A sturdy, rosy woman of forty with floury forearms, a spotted apron and a cloth tied over her hair. She keeps a baby on her hip on market days, a ladle in her free hand, and her eye on the children on the green.";
        Level = 2;
        MaxVitality = 25;

        AddChat("Bread and cheese, still warm! Baked apples with honey! Come and {cmd}browse{/}, dear, there's plenty.");
        AddChat("You look as if you've walked a long way. Sit on the wall and have something hot.");
        AddChat("Mind you leave the plate. I've only so many.");
        AddTopic("bread,cheese,bread and cheese,loaf,loaves", "Bread from my own oven and cheese from my brother's dairy, wrapped in a cloth. Three gold, and you'll not go hungry. {cmd}buy bread{/}.");
        AddTopic("apple,apples,baked apple,honey", "Baked in the oven with honey and a clove, and a little butter. Two gold a piece. {cmd}buy apple{/}.");
        AddTopic("southfarm,hamlet,green,village", "Eleven cottages, one mill, one well and the best apples in the county. We feed the city, and the city does not notice.");
        AddTopic("children,child,kids,boys,girls", "Mine are the ones making the most noise. Adelina and Emmeline, if you hear a name shouted, it will be one of those two.");
        AddTopic("oakenfell,city,rise,walls", "You'll see it from the Rise, past the mill. Whatever you think you're expecting, it will be bigger.");
        AddTopic("west,back fields,hedge,hedges,goblins,bandits,woods", "We don't go west past the last hedge. Nobody's told us not to, and nobody's needed to.");
        AddEmote("Griselda Hobday shifts the baby on her hip and straightens a row of loaves.", 8);
        AddEmote("The farm wife shouts across the green, \"Adelina! Emmeline! Not in the pond!\"", 8);

        AddStock("/domains/greenvale/corridor/south_road/obj/bread_and_cheese", 3);
        AddStock("/domains/greenvale/corridor/approach/obj/baked_apple", 2);
    }
}
