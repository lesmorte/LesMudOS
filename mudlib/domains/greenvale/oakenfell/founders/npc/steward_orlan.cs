// @inherit /std/npc

public class FdStewardOrlan : Npc
{
    public override void Create()
    {
        Name = "Orlan Hesk";
        Ids.Add("orlan");
        Ids.Add("hesk");
        Ids.Add("steward");
        Ids.Add("assembly steward");
        ShortDesc = "Orlan Hesk, the Assembly steward";
        LongDesc = "A spare, upright man in a plain green coat with a small oak pin at the collar, grey at the temples and perfectly composed. He sits behind the desk with his hands folded and sees everything that comes through the door without turning his head.";
        Level = 3;
        MaxHp = 40;

        AddChat("Welcome to the public gallery. Please sit where you like, and do not applaud.");
        AddChat("Prosperity Through Unity. It is our motto. It is also, if you attend long enough, a prayer.");
        AddChat("Mostly it is committees. Roads, tolls, grain. It is dull work, which is how a country stays quiet.");
        AddTopic("assembly,greenvale assembly,government", "The Greenvale Assembly governs most of the settled lands of Greenvale. It began as village leaders and merchants who wished to protect one another, and it has grown to be rather more than that. Most folk think well of it. A few say it has grown comfortable.");
        AddTopic("delegates,who sits,members,sits,chamber", "Delegates from the villages and towns, representatives of the great trades, and a few others whose names I will not trouble you with. They sit beyond the doors. They are, at present, disagreeing about the Westwood Road.");
        AddTopic("allies,friends,consortium,riverwatch,hall of legends", "The Merchant Consortium, the Hall of Legends and the Riverwatch Rangers are our friends, and our dealings with them are cordial. The Consortium, perhaps, is more cordial than the rest.");
        AddTopic("rivals,enemies,thorn pact,thorn,free companies,companies", "We have disputes with the Thorn Pact and with the Free Companies. A dispute is not a feud. I would not put it more strongly than that.");
        AddTopic("motto,unity,prosperity", "Prosperity Through Unity. It was chosen by those who founded the Assembly, when they could agree on nothing else. I find it modest.");
        AddTopic("business,politics,petition,quest,quests,join,help", "Political business is not open to newcomers, I am afraid. The Assembly knows those it knows. Come again when your name has gone ahead of you. You may sit in the gallery in the meantime and watch us be tedious.");
        AddEmote("From beyond the inner doors a delegate's voice rises: \"Three a wheel at the bridge, and not a coin more!\" There is a chorus of groans.", 6);
        AddEmote("Behind the carved screen a gavel raps, and a voice calls for order, and gets something else.", 6);
        AddEmote("Orlan Hesk turns a page of his ledger, makes a mark, and lifts his eyes to the inner doors for exactly one second.", 5);
        AddEmote("Through the lattice a delegate pounds the bench: \"Then let the Consortium pay for the road!\" Someone laughs, and someone else does not.", 5);
    }
}
