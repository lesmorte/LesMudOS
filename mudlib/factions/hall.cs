// @inherit /std/faction

/// <summary>The Hall of Legends, the guild confederation. Data for docs/FACTIONS.md; bible chapter "The Hall of Legends".</summary>
public class FactionHall : Faction
{
    public override void Create()
    {
        FactionId = "hall";
        FactionName = "The Hall of Legends";
        ShortName = "the Hall";
        Motto = "Greatness Is Chosen";
        Headquarters = "Oakenfall";
        Kind = "Guild Confederation";
        Description = "The Hall of Legends oversees advanced training, guild certification, and profession advancement. Though officially neutral, the Hall wields enormous influence throughout Greenvale. Its leaders believe that exceptional individuals shape history. Players choose their advanced classes through the Hall.";
        Hidden = false;
        StartsKnown = true;
        Relations = new Dictionary<string, int>();
        Remarks = new string[]
        {
            "",                                                                                       // 0 Unknown
            "You may stand in the Hall, but you have not yet earned a place in it.",                  // 1 Hostile
            "The Hall has heard of your deeds, and it does not yet think them worthy of record.",     // 2 Suspicious
            "",                                                                                       // 3 Neutral
            "Welcome to the Hall; we are pleased to count you among those who strive.",               // 4 Friendly
            "Your achievements are a credit to the Hall, and we say so openly.",                      // 5 Trusted
            "The Hall honors you as one of its own, for your deeds are the kind it was founded to celebrate.", // 6 Honored
            "Masters of the Hall now point to your deeds as the standard that new members must reach.", // 7 Exalted
            "Your name is written into the Hall's own record of greatness, and students will read it for years to come.", // 8 Legendary
        };
        TitleHonored = "Companion of the Hall";
        TitleExalted = "Exemplar of the Hall";
        TitleLegendary = "Legend of the Hall";
        WhereFound = "Met in Oakenfall, at the Hall of Legends.";
    }
}
