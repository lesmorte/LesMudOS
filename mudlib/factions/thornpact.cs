// @inherit /std/faction

/// <summary>The Thorn Pact. Data for docs/FACTIONS.md. The bible's Thorn Pact chapter gives no Type or Description; see the report.</summary>
public class FactionThornpact : Faction
{
    public override void Create()
    {
        FactionId = "thornpact";
        FactionName = "The Thorn Pact";
        ShortName = "the Thorn Pact";
        Motto = "The Forest Remembers";
        Headquarters = "Unknown";
        Kind = "";
        Description = "The Thorn Pact believes Greenvale is expanding too quickly. Forests are cleared. Ancient sites are disturbed. Wild places disappear each year. While many members simply seek balance, radical factions have begun sabotaging logging camps and trade roads. The Assembly considers them troublesome. The Pact views itself as necessary.";
        Hidden = false;
        StartsKnown = false;
        Relations = new Dictionary<string, int>
        {
            { "assembly", -10 },
            { "riverwatch", -10 },
            { "consortium", -25 },
        };
        Remarks = new string[]
        {
            "",                                                                                                  // 0 Unknown
            "The forest has marked you, and the Pact will not let you walk beneath its boughs unwatched.",      // 1 Hostile
            "The Pact remembers what you did to the wild places, and it is slow to forgive.",                    // 2 Suspicious
            "",                                                                                                  // 3 Neutral
            "The forest lets you pass, and the Pact has begun to take note of your steps.",                      // 4 Friendly
            "You have shown respect for the wild, and the Pact will speak plainly with you.",                    // 5 Trusted
            "The Pact counts you a friend of the forest, and that is not a word it gives lightly.",              // 6 Honored
            "Those who keep the old places have begun to tell one another of your deeds.",                       // 7 Exalted
            "The forest itself remembers you, and the Pact will not forget that you once stood for it.",        // 8 Legendary
        };
        TitleHonored = "Friend of the Forest";
        TitleExalted = "Thornwarden";
        TitleLegendary = "Champion of the Thorn Pact";
        WhereFound = "Met in the North Rows, where the herbalist can point the way.";
    }
}
