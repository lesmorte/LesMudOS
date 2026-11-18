// @inherit /std/faction

/// <summary>The Merchant Consortium, the merchant coalition. Data for docs/FACTIONS.md; bible chapter "The Merchant Consortium".</summary>
public class FactionConsortium : Faction
{
    public override void Create()
    {
        FactionId = "consortium";
        FactionName = "The Merchant Consortium";
        ShortName = "the Consortium";
        Motto = "Trade Builds Tomorrow";
        Headquarters = "Greenvale Market";
        Kind = "Merchant Coalition";
        Description = "The wealthiest merchants in Greenvale belong to the Consortium. They maintain caravans, shipping contracts, warehouses, and banking operations. Many Assembly decisions are heavily influenced by Consortium interests.";
        Hidden = false;
        StartsKnown = true;
        Relations = new Dictionary<string, int>
        {
            { "assembly", 10 },
            { "riverwatch", 5 },
            { "freecompanies", -10 },
            { "thornpact", -25 },
        };
        Remarks = new string[]
        {
            "",                                                                                         // 0 Unknown
            "Your credit is worth nothing with the Consortium, and no ledger here will carry your name.", // 1 Hostile
            "The Consortium has a poor view of your accounts and will not extend you credit.",          // 2 Suspicious
            "",                                                                                         // 3 Neutral
            "You are a customer we trust with a fair price and a fair line of credit.",                 // 4 Friendly
            "Your name is good for credit at any Consortium counter, and our terms reflect it.",        // 5 Trusted
            "The Consortium extends you its best terms, for your trade has earned them.",              // 6 Honored
            "Factors speak of your trade as among the soundest in the Consortium's books.",             // 7 Exalted
            "The Consortium counts you among its great investors, and your name opens doors that gold alone cannot.", // 8 Legendary
        };
        TitleHonored = "Merchant Protector";
        TitleExalted = "Factor of the Consortium";
        TitleLegendary = "Master of Coin";
        WhereFound = "Met at Greenvale Market.";
    }
}
