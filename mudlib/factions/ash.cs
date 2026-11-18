// @inherit /std/faction

/// <summary>The Circle of Ash, the secret arcane society. Hidden until discovered. Data for docs/FACTIONS.md; bible chapter "The Circle of Ash".</summary>
public class FactionAsh : Faction
{
    public override void Create()
    {
        FactionId = "ash";
        FactionName = "The Circle of Ash";
        ShortName = "the Circle";
        Motto = "Knowledge Demands Sacrifice";
        Headquarters = "Hidden";
        Kind = "Secret Arcane Society";
        Description = "Officially the Circle does not exist. Rumors suggest the organization studies forbidden magic, ancient civilizations, necromancy, and artifacts predating Greenvale itself. Most stories are likely exaggerated. Not all are.";
        Hidden = true;
        StartsKnown = false;
        Relations = new Dictionary<string, int>();
        Remarks = new string[]
        {
            "",                                                                                                   // 0 Unknown
            "You should not have come looking here, and you will not be welcome when you return.",               // 1 Hostile
            "We have seen you, and we have not decided what you are for.",                                       // 2 Suspicious
            "",                                                                                                   // 3 Neutral
            "You have been useful, and we have noted it in ways you will not see.",                              // 4 Friendly
            "Few are shown the lower shelves, and you have been shown them.",                                    // 5 Trusted
            "The older members have begun to ask about you, and the answers have not been given.",              // 6 Honored
            "Something was left to you in the ash, and it will be yours when you are ready.",                    // 7 Exalted
            "Your name is in the ash now, and the ash does not give back what it keeps.",                        // 8 Legendary
        };
        TitleHonored = "Seeker of Ash";
        TitleExalted = "Adept of the Circle";
        TitleLegendary = "Keeper of Ancient Ash";
        WhereFound = "Not met in any place in this first pass; the Circle is hidden.";
    }
}
