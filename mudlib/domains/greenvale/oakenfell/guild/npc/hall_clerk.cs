// @inherit /std/npc

public class GqHallClerk : Npc
{
    public override void Create()
    {
        Name = "Mabyn";
        Ids.Add("mabyn");
        Ids.Add("clerk");
        Ids.Add("hall clerk");
        Ids.Add("clerk of the hall");
        Ids.Add("man");
        ShortDesc = "Mabyn, Clerk of the Hall of Legends";
        LongDesc = "A neat, middle-aged man in a long oak-green coat with the Hall's five joined bars on the breast, his hair cut precisely and his cuffs spotless. He has a ledger, a pen, and a tidy answer to most questions, and he is visibly proud to be the one who holds the Hall's pen.";
        Level = 18;
        MaxVitality = 75;

        AddChat("Good day. Mabyn, clerk of the Hall of Legends. Our motto is Greatness Is Chosen. I speak for the Hall itself. What may I tell you?");
        AddChat("The Hall of Legends is a confederation of guilds. We oversee advanced training, certify the guilds, and see that professions advance in good order. We also keep the Chronicle.");
        AddChat("Officially, we are neutral. In practice, we are on the side of exceptional people, wherever they come from.");
        AddTopic("hall of legends,confederation,guilds,the hall,your hall,this hall", "The Hall of Legends: a confederation of guilds, with its seat here in Oakenfell. Our motto is Greatness Is Chosen. We want to train exceptional adventurers, preserve ancient knowledge, support the development of guilds, and prepare for the threats still to come. We count most of the established powers among our friends. Our rivals are any who would seek to monopolize power. Officially we are neutral, and I can say that we take that seriously.");
        AddTopic("motto,greatness,chosen", "Greatness Is Chosen. Our leaders believe that exceptional individuals shape history, and that they do so by choosing to.");
        AddTopic("goals,want,wants,aims,seek,purpose", "To train exceptional adventurers, preserve ancient knowledge, support guild development, and prepare for future threats. The Chronicle serves the second goal, and the hallmasters the first.");
        AddTopic("allies,friends,rivals,enemies,opponents", "Most established factions are our friends. Our rivals are anyone who would seek to monopolize power. We are not specific, because we do not need to be.");
        AddTopic("circle,circle of ash", "I am afraid it is not a name in our ledgers. Is it a guild? A guild that is not on the register does not, I fear, exist.");
        AddTopic("assembly,greenvale assembly,government,assembly hall", "The Assembly: a good friend of the Hall, and a firm believer in order. We respect their law and they respect our neutrality, which is the best arrangement a confederation can ask for.");
        AddTopic("consortium,merchant,merchants,merchant consortium", "The Consortium is wealthy, and wealth is useful to guilds. We do not take sides in trade, but we are glad of those who endow our libraries.");
        AddTopic("rangers,riverwatch,watchfort,ranger", "The Riverwatch Rangers are friends of the Hall, and many a graduate of ours has served at Watchfort Crossing. They keep the frontier, and we keep the people who serve it.");
        AddTopic("thorn,pact,thorn pact,thorns", "The Thorn Pact wants to preserve sacred and forgotten places, which is not so far from our own aim of preserving ancient knowledge. But the Hall does not endorse sabotage, and it stays neutral in their quarrels with the Assembly.");
        AddTopic("free companies,free company,companies,mercenaries,sellswords,coin before crown", "The Free Companies: many a sellsword has walked these halls to ask for advancement, and we do not ask whose coin they carry. We would simply prefer them not to bring their quarrels through the Great Doors.");
        AddTopic("standing,reputation,favor,favour,rank", "The Hall's ledgers of standing are not yet open. When they are, I shall be the first to tell you your place in them. Until then, I can only offer you a seat.");
        AddEmote("Mabyn blots a line in his ledger and examines it with satisfaction.", 8);
        AddEmote("Mabyn adjusts the angle of his pen to the edge of the desk.", 6);
    }
}
