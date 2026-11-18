/// <summary>
/// Base of every faction of Greenvale. A faction is data: the file lives in /factions, inherits this class and sets its
/// fields in Create(); its id is set there too and matches the file name (docs/FACTIONS.md sections 2 and 13). The
/// registry /secure/factions reads these files. Standings are kept there, not here.
/// </summary>
public abstract class Faction : MudObject
{
    /// <summary>The id, the same as the file name: "riverwatch". Named FactionId because MudObject already has Id().</summary>
    protected string FactionId = "";
    /// <summary>The full name: "The Riverwatch Rangers".</summary>
    protected string FactionName = "";
    /// <summary>The name used in speech: "the Rangers".</summary>
    protected string ShortName = "";
    /// <summary>The motto, without quotation marks: "Ever Vigilant".</summary>
    protected string Motto = "";
    /// <summary>Where the faction is based: "Watchfort Crossing".</summary>
    protected string Headquarters = "";
    /// <summary>The design bible's "Type": "Military Frontier Order".</summary>
    protected string Kind = "";
    /// <summary>One paragraph about the faction, in the owner's words.</summary>
    protected string Description = "";
    /// <summary>A hidden faction is not shown anywhere until the character has discovered it. Only the Circle of Ash.</summary>
    protected bool Hidden;
    /// <summary>True if a character starts knowing the faction, at Neutral 0. False if it is met in play.</summary>
    protected bool StartsKnown;
    /// <summary>Faction id to the percent of a gain of 50 or more that spills onto it: positive for an ally, negative for a rival.</summary>
    protected Dictionary<string, int> Relations = new();
    /// <summary>Remark lines by tier number, 0 (Unknown) to 8 (Legendary). Entries 0 and 3 are empty strings.</summary>
    protected string[] Remarks = new string[] { "", "", "", "", "", "", "", "", "" };
    /// <summary>The title granted at Honored.</summary>
    protected string TitleHonored = "";
    /// <summary>The title granted at Exalted.</summary>
    protected string TitleExalted = "";
    /// <summary>The title granted at Legendary.</summary>
    protected string TitleLegendary = "";
    /// <summary>One line on where the faction's people are met.</summary>
    protected string WhereFound = "";

    public string QueryId() => FactionId;
    public string QueryName() => FactionName;
    public string QueryShortName() => ShortName;
    public string QueryMotto() => Motto;
    public string QueryHeadquarters() => Headquarters;
    public string QueryKind() => Kind;
    public string QueryDescription() => Description;
    public bool QueryHidden() => Hidden;
    public bool QueryStartsKnown() => StartsKnown;
    public Dictionary<string, int> QueryRelations() => new(Relations);
    /// <summary>The percent this faction's gains spill onto another faction id; 0 if there is no relation.</summary>
    public int QueryRelation(string otherId) => Relations.TryGetValue(otherId, out var percent) ? percent : 0;
    /// <summary>The remark line for a tier number (0 to 8); empty for Neutral, Unknown or an out-of-range number.</summary>
    public string QueryRemark(int tier) => tier >= 0 && tier < Remarks.Length ? Remarks[tier] : "";
    public string QueryTitleHonored() => TitleHonored;
    public string QueryTitleExalted() => TitleExalted;
    public string QueryTitleLegendary() => TitleLegendary;
    public string QueryWhereFound() => WhereFound;
}
