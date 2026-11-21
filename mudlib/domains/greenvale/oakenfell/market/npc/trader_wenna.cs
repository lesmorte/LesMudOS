// @inherit /std/npc

public class MkTraderWenna : Npc
{
    public override void Create()
    {
        Name = "Wenna Cobb";
        Ids.Add("wenna");
        Ids.Add("wenna cobb");
        Ids.Add("trader");
        Ids.Add("cheesemonger");
        Ids.Add("stallholder");
        ShortDesc = "Wenna Cobb, a cheesemonger";
        LongDesc = "A large, ruddy woman with sleeves rolled above floury forearms and a cheese-wire looped through her belt. Her stall is stacked with wheels of cheese in every shade from cream to deep orange.";
        Level = 1;
        MaxVitality = 15;

        AddChat("Cheese! Soft, hard, sharp, sweet! Taste before you buy, or go hungry!");
        AddChat("Three generations on this spot, and not one complaint we couldn't talk down!");
        AddChat("Oi, you! You look like someone who appreciates cheese!");
        AddTopic("cheese,wheels,sell,buy", "I sell by the wheel to the cookshops and inns of the city, on standing order. Nothing I could sell a traveler by the slice. Try the Outfitter on Stag Lane, or the street cart on Wayfarer's Row.");
        AddTopic("market,stalls,rows", "The Rows have been here longer than the Consortium, and don't let the blue-coats tell you otherwise.");
        AddTopic("consortium,toll,tolls", "The Consortium takes its cut of every wheel I sell. I say nothing. I just make the cheese sharper.");
        AddEmote("Wenna Cobb slices a sliver of cheese and holds it out to a passerby.", 8);
        AddEmote("Wenna Cobb bawls, \"Cheese! Come and get it!\"", 8);
    }
}
