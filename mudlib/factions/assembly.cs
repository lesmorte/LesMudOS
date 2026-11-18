// @inherit /std/faction

/// <summary>The Greenvale Assembly, the regional government. Data for docs/FACTIONS.md; bible chapter "The Greenvale Assembly".</summary>
public class FactionAssembly : Faction
{
    public override void Create()
    {
        FactionId = "assembly";
        FactionName = "The Greenvale Assembly";
        ShortName = "the Assembly";
        Motto = "Prosperity Through Unity";
        Headquarters = "Assembly Hall, Oakenfall";
        Kind = "Regional Government";
        Description = "The Greenvale Assembly governs most settled lands throughout Greenvale. Originally formed by village leaders and merchants seeking mutual protection, the Assembly now serves as the political center of the region. Most citizens view the Assembly favorably, though some believe it has become too comfortable and bureaucratic.";
        Hidden = false;
        StartsKnown = true;
        Relations = new Dictionary<string, int>
        {
            { "consortium", 10 },
            { "riverwatch", 10 },
            { "hall", 5 },
            { "freecompanies", -10 },
            { "thornpact", -25 },
        };
        Remarks = new string[]
        {
            "",                                                                                  // 0 Unknown
            "You will forgive the Assembly if it does not extend its usual courtesy to you.",    // 1 Hostile
            "The Assembly has taken note of your recent conduct, and it is not yet satisfied.",  // 2 Suspicious
            "",                                                                                  // 3 Neutral
            "The Assembly is glad to see you, and its doors are open to you.",                   // 4 Friendly
            "You have served the Assembly well, and it is prepared to rely on you.",             // 5 Trusted
            "The Assembly records your service with gratitude, and the town is better for it.", // 6 Honored
            "The Assembly's councillors speak of you with respect whenever your name comes before them.", // 7 Exalted
            "Your conduct is set before the Assembly's new members as an example of civic duty.", // 8 Legendary
        };
        TitleHonored = "Friend of Oakenfell";
        TitleExalted = "Warden of Greenvale";
        TitleLegendary = "Voice of the Assembly";
        WhereFound = "Met in Oakenfall, at Assembly Hall.";
    }
}
