// @inherit /std/npc

public class SgPorterJoss : Npc
{
    public override void Create()
    {
        Name = "Joss Barrow";
        Ids.Add("joss");
        Ids.Add("porter");
        Ids.Add("big joss");
        Ids.Add("joss barrow");
        ShortDesc = "Joss Barrow, a porter";
        LongDesc = "A huge, slow-moving man with a bull's neck, a leather harness across his shoulders and hands like spades. He sits on a battered trunk, watching the street with a mild, patient air.";
        Level = 3;
        MaxHp = 40;

        AddChat("Carry your bags, friend? A couple of coins to the Stag, a few more to the Market. No haggling.");
        AddChat("I've carried a duke's wardrobe and a dead man's chest. This street has everything.");
        AddChat("Mind your back, friend. Mine's not what it was.");
        AddTopic("bags,luggage,carry,porter", "I've a strong back and a free hour, but your coin's better spent on a hot meal. I only carry if you're laden, and you don't look laden.");
        AddTopic("street,row,wayfarers", "Everyone comes through here, sooner or later. The cheap beds are in the Row, the good ones at the Stag. Never sleep in a house with no lamp.");
        AddTopic("free lodge,sellswords,lodge", "Hard folk, but square. They pay their bills, and they pay me double when they've had a bad week.");
        AddTopic("gate,city,watch", "Hobb will talk your ear off, and Dagna won't say a word. Between them you'll learn what you need.");
        AddEmote("Joss Barrow hoists a trunk onto his shoulder as if it were a loaf of bread.", 8);
        AddEmote("Joss Barrow sits on his trunk and watches the carts.", 6);
    }
}
