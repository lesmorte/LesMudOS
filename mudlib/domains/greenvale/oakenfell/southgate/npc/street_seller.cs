// @inherit /std/shopkeeper

public class SgStreetSeller : Shopkeeper
{
    public override void Create()
    {
        Name = "Nan Tuppence";
        Ids.Add("nan");
        Ids.Add("seller");
        Ids.Add("food seller");
        Ids.Add("street seller");
        Ids.Add("cart");
        Ids.Add("nan tuppence");
        Ids.Add("vendor");
        ShortDesc = "Nan Tuppence, a street food seller";
        LongDesc = "A short, round woman with flour in her hair and a leather apron over a bright patched skirt. She presides over a smoking cart with a ladle in one hand and a wooden spoon in the other, and her voice carries over the whole street.";
        Level = 1;
        MaxVitality = 15;

        AddChat("Hot pies! Hot pies and honeyed nuts! Fresh from the griddle, still too hot to hold!");
        AddChat("Try {cmd}browse{/} to see what's in the cart, dear, and {cmd}buy pie{/} if you've coin.");
        AddChat("You've the look of someone who walked through the night. Sit, eat, then tell me your troubles.");
        AddTopic("pie,pies,hand pie", "Mutton and onion, baked this morning and kept warm on my own griddle. Three gold, and worth every penny. {cmd}buy pie{/}.");
        AddTopic("nuts,honeyed nuts,honey", "Roasted with honey and a little salt. A bag is two gold, and children and sellswords love them the same. {cmd}buy nuts{/}.");
        AddTopic("row,wayfarers,street", "The Row's good for business. Everyone arrives hungry. It's the best cart in Oakenfell, and don't ask the pastry cook down the lane.");
        AddTopic("inn,stag,silver stag", "The Stag's food is good, but you'll pay for the fire. Mine's a pie for the road, and a good one.");
        AddEmote("Nan Tuppence flips a row of pies on the griddle and wipes her hands on her apron.", 8);
        AddEmote("Nan Tuppence shouts, \"Hot pies, still too hot to hold!\"", 8);

        AddStock("/domains/greenvale/oakenfell/southgate/obj/hand_pie", 3);
        AddStock("/domains/greenvale/oakenfell/southgate/obj/honeyed_nuts", 2);
    }
}
