/// <summary>
/// The tuning numbers of the faction system (docs/FACTIONS.md sections 3, 4, 5 and 13). Every number is read from here,
/// so it can be changed without touching the system. Other objects read them with Call("QueryName").
/// </summary>
public class FactionConfig : MudObject
{
    // Tier thresholds: the lowest points in each tier (section 3).

    /// <summary>The lowest points in Hostile. A standing is clamped to this and never falls below it.</summary>
    public int QueryTierMinHostile() => -1000;
    /// <summary>The lowest points in Suspicious.</summary>
    public int QueryTierMinSuspicious() => -500;
    /// <summary>The lowest points in Neutral.</summary>
    public int QueryTierMinNeutral() => 0;
    /// <summary>The lowest points in Friendly.</summary>
    public int QueryTierMinFriendly() => 1000;
    /// <summary>The lowest points in Trusted.</summary>
    public int QueryTierMinTrusted() => 3000;
    /// <summary>The lowest points in Honored.</summary>
    public int QueryTierMinHonored() => 6000;
    /// <summary>The lowest points in Exalted.</summary>
    public int QueryTierMinExalted() => 10000;
    /// <summary>The lowest points in Legendary. There is no ceiling above it.</summary>
    public int QueryTierMinLegendary() => 20000;

    // Donations (section 4 and section 13, item 2).

    /// <summary>Gold a character gives for each point of standing from a donation.</summary>
    public int QueryGoldPerPoint() => 10;
    /// <summary>Points a day, per faction, that a donation gives at full value.</summary>
    public int QueryDonationFullBand() => 50;
    /// <summary>Points a day, per faction, after the full-value band, that a donation gives at half value. Nothing beyond it.</summary>
    public int QueryDonationHalfBand() => 50;
    /// <summary>The tier that donations cannot take a standing above.</summary>
    public string QueryDonationCapTier() => "Friendly";
    /// <summary>The highest points a donation can raise a standing to.</summary>
    public int QueryDonationCapPoints() => 2999;

    // Repeats (section 4): a keyed gain is worth less the more often the same character repeats it in one game day.

    /// <summary>How many repeats of one keyed gain in a day are worth the full amount (1st to 3rd).</summary>
    public int QueryRepeatFullCount() => 3;
    /// <summary>The percent of a gain worth for a full-count repeat.</summary>
    public int QueryRepeatFullPercent() => 100;
    /// <summary>How many repeats after the full-count band are worth the half amount (4th to 6th).</summary>
    public int QueryRepeatHalfCount() => 3;
    /// <summary>The percent of a gain worth for a half-count repeat.</summary>
    public int QueryRepeatHalfPercent() => 50;
    /// <summary>The percent of a gain worth for every repeat after the half-count band (7th onward).</summary>
    public int QueryRepeatTailPercent() => 10;
    /// <summary>The least a repeat of a keyed gain is ever worth, in points.</summary>
    public int QueryRepeatMinimum() => 1;

    // Spillover (section 5 and section 13, item 1).

    /// <summary>The smallest gain that spills onto related factions. Smaller gains do not spill.</summary>
    public int QuerySpilloverThreshold() => 50;
    /// <summary>Spillover stops at this many points and cannot push a standing below it.</summary>
    public int QuerySpilloverFloor() => 0;

    // Starting standing (section 13, item 3).

    /// <summary>The faction a character who starts in Vincent's Hollow is given a bump with.</summary>
    public string QueryStartBumpFaction() => "assembly";
    /// <summary>The points of that starting bump, given once at character creation.</summary>
    public int QueryStartBumpAmount() => 100;

    // Hostile acts (section 4, sizes table).

    /// <summary>Standing lost for attacking a member of a faction, once per fight.</summary>
    public int QueryAttackPenalty() => -100;
    /// <summary>Standing lost for killing a member of a faction.</summary>
    public int QueryKillPenalty() => -500;
}
