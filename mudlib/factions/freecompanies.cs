// @inherit /std/faction

/// <summary>The Free Companies, the mercenary brotherhood. Data for docs/FACTIONS.md; bible chapter "The Free Companies".</summary>
public class FactionFreecompanies : Faction
{
    public override void Create()
    {
        FactionId = "freecompanies";
        FactionName = "The Free Companies";
        ShortName = "the Free Companies";
        Motto = "Coin Before Crown";
        Headquarters = "Various";
        Kind = "Mercenary Brotherhood";
        Description = "The Free Companies consist of sellswords, bounty hunters, adventurers, and mercenaries. Some are noble. Others are little better than bandits. Loyalty is earned through payment rather than politics.";
        Hidden = false;
        StartsKnown = false;
        Relations = new Dictionary<string, int>
        {
            { "assembly", -10 },
            { "consortium", -10 },
        };
        Remarks = new string[]
        {
            "",                                                                                        // 0 Unknown
            "You're bad for business, and nobody here will take your coin.",                          // 1 Hostile
            "I don't trust your word, so pay in advance or find another company.",                    // 2 Suspicious
            "",                                                                                        // 3 Neutral
            "You pay on time, which puts you ahead of most. Work for us if you want it.",             // 4 Friendly
            "You're reliable, and reliable gets the good contracts.",                                 // 5 Trusted
            "Blade for hire, that's what you are to us now, and the best coin goes your way.",        // 6 Honored
            "Every company between here and the river knows your name, and most of them are jealous.", // 7 Exalted
            "The free marshals sing of your contracts over the campfire, and they pay in full.",     // 8 Legendary
        };
        TitleHonored = "Blade for Hire";
        TitleExalted = "Company Captain";
        TitleLegendary = "Free Marshal";
        WhereFound = "Met at the Free Lodge.";
    }
}
