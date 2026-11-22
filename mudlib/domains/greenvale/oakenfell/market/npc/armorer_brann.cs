// @inherit /std/npc

public class MkArmorerBrann : Npc
{
    public override void Create()
    {
        Name = "Brann Iveson";
        Ids.Add("brann");
        Ids.Add("brann iveson");
        Ids.Add("armorer");
        Ids.Add("armourer");
        Ids.Add("smith");
        ShortDesc = "Brann Iveson, an armorer";
        LongDesc = "A thick-chested, soot-grimed man with a bald head and a leather apron full of tools. He stands behind a stall with an empty rack and a single polished shield, looking like a man whose cart has not yet arrived.";
        Level = 3;
        MaxVitality = 40;

        AddChat("Come back later, friend. New stock's still being unloaded.");
        AddChat("The best armor in Greenvale is made in the Guild Quarter. I'm just the fellow who sells it.");
        AddChat("That shield? Display only. It's my pride.");
        AddTopic("armor,armour,mail,shield,shields,plate", "The new stock is still being unloaded from the cart. Come back later and I'll have something worth your gold.");
        AddTopic("stock,shipment,cart,delivery", "Due this week, or the one after. The road's not what it was, and the carters are slow.");
        AddTopic("road,western road,wagons", "Wagons are held at the Westgate, or turned back. My iron plate was on one of them.");
        AddEmote("Brann Iveson polishes his single shield with a rag, then glances down the lane for his cart.", 8);
        AddEmote("Brann Iveson taps the empty rack and sighs.", 6);
    }
}
