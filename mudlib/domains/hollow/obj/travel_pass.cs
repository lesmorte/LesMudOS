// @inherit /std/item

public class TravelPass : Item
{
    public override void Create()
    {
        Name = "Travel Pass";
        Ids.Add("pass");
        Ids.Add("travel pass");
        Ids.Add("safe conduct");
        ShortDesc = "a travel pass";
        LongDesc = "A stiff card of waxed parchment stamped with the sword-and-banner badge of the Adventurers Guild. It is a pass of safe conduct, issued by the guild of the Village of Vincent's Hollow, and it opens the road north toward Oakenfell.";
        ReadText = "TRAVEL PASS\nIssued by the Adventurers Guild of the Village of Vincent's Hollow.\nThe bearer has proved themselves in the Forgotten Cellars and may travel the road north into Greenvale, toward Oakenfell.";
        QuestItem = true;
        Value = 0;
    }
}
