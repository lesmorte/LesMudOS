// @inherit /std/npc

public class MkFactorIsolde : Npc
{
    public override void Create()
    {
        Name = "Isolde Marrin";
        Ids.Add("isolde");
        Ids.Add("isolde marrin");
        Ids.Add("factor");
        Ids.Add("consortium factor");
        Ids.Add("merchant");
        ShortDesc = "Isolde Marrin, a Consortium factor";
        LongDesc = "A poised woman of forty-five, in a blue wool gown with a silver scale at the throat. Her dark hair is wound in a tight coil, and her smile is warm, friendly, and exactly as deep as it needs to be.";
        Level = 5;
        MaxVitality = 60;

        AddChat("Trade Builds Tomorrow. It's our motto, and we mean it.");
        AddChat("The Merchant Consortium is the commerce of Greenvale. If you buy it, sell it or carry it, we had a hand in it.");
        AddChat("Welcome to Consortium House. May I help you, or are you merely looking? Both are welcome.");
        AddTopic("consortium,merchants,coalition,trade", "The wealthiest merchants in Greenvale belong to the Consortium. We keep the caravans, the shipping contracts, the warehouses and a good part of the banking. Where trade flows, prosperity follows.");
        AddTopic("interests,goals,business", "We expand trade, open new routes, protect economic stability, and, yes, make a profit. The Assembly values our judgment. We value its peace.");
        AddTopic("rivals,enemies,free companies,smugglers,bandits", "The Free Companies, the smugglers and the bandit bands. The first sell steel to the highest bidder, the second sell untaxed goods, the third sell fear. None of them build anything.");
        AddTopic("assembly,government", "We are allies of the Assembly, naturally. Many of its decisions are influenced by our interests, because we know what trade requires.");
        AddTopic("thorn pact,pact,elderwood,western road,logging", "The Pact objects to every road we build. Some of its members go further, and our wagons burn for it. We have asked the Assembly and the Riverwatch to see to it.");
        AddTopic("riverwatch,rangers", "The Riverwatch are friends of the Consortium. Their rangers escort our wagons, and we pay a fair price in return.");
        AddEmote("Isolde Marrin signs a document with a sweep of her pen.", 8);
        AddEmote("Isolde Marrin studies a map of the western road and frowns.", 6);
    }
}
