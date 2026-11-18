// @inherit /std/faction

/// <summary>The Riverwatch Rangers, the military frontier order. Data for docs/FACTIONS.md; bible chapter "The Riverwatch Rangers".</summary>
public class FactionRiverwatch : Faction
{
    public override void Create()
    {
        FactionId = "riverwatch";
        FactionName = "The Riverwatch Rangers";
        ShortName = "the Rangers";
        Motto = "Ever Vigilant";
        Headquarters = "Watchfort Crossing";
        Kind = "Military Frontier Order";
        Description = "The Riverwatch Rangers patrol the borders of Greenvale. They protect roads, escort travelers, hunt dangerous creatures, and identify threats before they reach the settled lands. Among common folk they are considered heroes.";
        Hidden = false;
        StartsKnown = true;
        Relations = new Dictionary<string, int>
        {
            { "assembly", 10 },
            { "hall", 5 },
            { "thornpact", -25 },
        };
        Remarks = new string[]
        {
            "",                                                                   // 0 Unknown
            "You have nerve, showing your face to the Riverwatch.",               // 1 Hostile
            "The Rangers have not forgotten what you did.",                       // 2 Suspicious
            "",                                                                   // 3 Neutral
            "The Riverwatch knows your name.",                                    // 4 Friendly
            "Riverwatch speaks highly of your efforts.",                          // 5 Trusted
            "An honor. The roads are safer for you.",                             // 6 Honored
            "The Captain asks after you by name.",                                // 7 Exalted
            "They tell recruits about you at Watchfort Crossing.",                // 8 Legendary
        };
        TitleHonored = "Riverwatch Scout";
        TitleExalted = "Riverwatch Warden";
        TitleLegendary = "Captain of the Watch";
        WhereFound = "Met at Watchfort Crossing.";
    }
}
