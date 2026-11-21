// @inherit /std/npc

public class GqEnvoyConsortium : Npc
{
    public override void Create()
    {
        Name = "Dorian";
        Ids.Add("dorian");
        Ids.Add("envoy");
        Ids.Add("consortium envoy");
        Ids.Add("envoy of the consortium");
        Ids.Add("man");
        ShortDesc = "Dorian, Envoy of the Merchant Consortium";
        LongDesc = "A round, well-fed man in a waistcoat of gold-threaded brown, with a set of small brass scales pinned to his lapel and an abacus within easy reach of his right hand. His smile is open and his eyes are busy, and you suspect he has priced your boots.";
        Level = 20;
        MaxVitality = 85;

        AddChat("Ah, a visitor. I am Dorian, envoy of the Merchant Consortium. Our motto is Trade Builds Tomorrow. Do ask me anything. Information is the cheapest thing I sell, and sometimes I give it away.");
        AddChat("Caravans, shipping contracts, warehouses and banking: that is the Consortium. Behind every full market stall in Greenvale is one of our wagons.");
        AddChat("A road that is safe is a road that is profitable. Remember that, and you will understand most of what we do.");
        AddTopic("consortium,merchant consortium,merchant,merchants,trade", "The Merchant Consortium: the wealthiest merchants in Greenvale, with caravans, shipping contracts, warehouses and banking. Our motto is Trade Builds Tomorrow. We want to expand trade, increase profits, open new routes and keep the economy stable. Our friends are the Assembly and the Riverwatch Rangers. Our rivals are the Free Companies, smugglers and bandit organizations. Our house is on Coin Street, in the Market District, and you are welcome there.");
        AddTopic("motto,tomorrow,builds", "Trade Builds Tomorrow. It is good for the soul and better for the ledger.");
        AddTopic("goals,want,wants,aims,seek,profit,profits", "More trade, more profit, new routes and a steady economy. We are not ashamed to say so. A prosperous Greenvale is a safer Greenvale.");
        AddTopic("allies,friends,rivals,enemies,opponents", "The Assembly and the Riverwatch Rangers are our friends. Our rivals are the Free Companies, smugglers and bandit organizations.");
        AddTopic("circle,circle of ash", "Circle? I know of circles of merchants, circles of credit, even a circle of very bad cheese. I do not know that one.");
        AddTopic("assembly,greenvale assembly,government,assembly hall", "The Assembly: a good partner. Many of their decisions are, let us say, informed by our interests, and they are generous enough not to mind. Stable government is excellent for business.");
        AddTopic("rangers,riverwatch,watchfort,ranger", "The Riverwatch Rangers keep the roads open and our caravans alive. I have never regretted a coin spent on their good opinion. They are friends, in the plainest sense.");
        AddTopic("hall,hall of legends,hallmasters,guild confederation", "The Hall of Legends trains the sort of folk a caravan wishes to hire. They call themselves neutral. We find that a convenient arrangement.");
        AddTopic("thorn,pact,thorn pact,thorns", "The Thorn Pact: a costly nuisance. When their radicals burn a logging camp or sabotage a road, someone must pay for the delay, and it is generally me. They say they speak for the forest. I would like to know what the forest thinks of the invoice.");
        AddTopic("free companies,free company,companies,mercenaries,sellswords,coin before crown", "The Free Companies sell their swords to anyone, and a few to everyone. Some are honest professionals. Some are bandits with a ledger. Their loyalty goes to whoever paid last, which makes them poor partners and tiresome rivals. Our regulators have not made them love us.");
        AddTopic("standing,reputation,favor,favour,rank,credit", "The Hall's ledgers of standing are not yet open, and the Consortium will not extend credit on what has not been written. When they are open, I shall be happy to tell you how you stand.");
        AddEmote("Dorian clicks a bead along his abacus and nods to himself.", 8);
        AddEmote("Dorian weighs a coin in his palm, hums, and slips it away.", 6);
    }
}
