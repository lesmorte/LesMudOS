// @inherit /std/npc

public class MkChangerLucan : Npc
{
    public override void Create()
    {
        Name = "Lucan Teague";
        Ids.Add("lucan");
        Ids.Add("lucan teague");
        Ids.Add("money-changer");
        Ids.Add("moneychanger");
        Ids.Add("changer");
        ShortDesc = "Lucan Teague, a money-changer";
        LongDesc = "A small, neat man with silver spectacles, a pointed beard and a fine brass scale on his table. His fingers move over his coins quickly, as if they were counting themselves.";
        Level = 2;
        MaxHp = 25;

        AddChat("Coins changed, coins weighed, coins tested. Foreign silver, clipped gold, river scrip: all welcome, none trusted.");
        AddChat("A short coin is a shorter friend. I weigh everything.");
        AddChat("Gold from the Hollow is good gold. Vincent's Hollow has an honest reputation.");
        AddTopic("coins,coin,gold,silver,exchange,change", "The gold of Greenvale is good in every shop of the city, and you need no change for it. I weigh the rest, the foreign silver and the river scrip, and tell you what it is worth.");
        AddTopic("bank,banker,aldous", "The bank stands north of here, across the street. Master Aldous will see to accounts when his books are open to new customers.");
        AddTopic("forgery,false coins,clipped,clipping", "Clipped and shaved and washed with acid. I've seen all the tricks. My scales don't lie.");
        AddTopic("street,coin street,consortium", "Coin Street is where Greenvale counts its money, and some of the Consortium's, too. Mind your purse; nobody here is as honest as I am.");
        AddEmote("Lucan Teague weighs a coin on his scales and shakes his head.", 8);
        AddEmote("Lucan Teague polishes his spectacles and peers at the next customer.", 6);
    }
}
