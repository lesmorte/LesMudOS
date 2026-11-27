/// <summary>
/// The words of a blow (docs/specs/tags-and-saves.md, section 6): which verb a hit takes, by the kind of attack and by how much of the target's
/// maximum Vitality it really took. Four tiers (under 10%, under 25%, under 40%, 40% and over); each cell is "the verb you read | the verb the
/// others read | what follows the target". The line is built by Living.Attack: "You {verb} {the target}{how}." / "{Name} {verbs} you{how}.".
///
/// Which ladder: a creature-only natural kind (bite, claw, gore, kick, slam, lash, touch, spit, thorns) has its own; else a ranged piercing blow
/// is archery (the words a bow always had, unchanged); else a melee or thrown blow takes the ladder of its damage type; else (fists, or an attack
/// with no tags at all) the plain ladder, which is the game's old words. Tuning a word is one line below. Everything here is a pure function of
/// its arguments over frozen tables, so a wizard's workshop may read it. Blueprint only; nothing is saved.
/// </summary>
public class BlowWords : MudObject
{
    private static readonly Dictionary<string, string[]> Rows = new();
    private static readonly HashSet<string> NaturalKinds = new();

    private static void Words(string ladder, string tier0, string tier1, string tier2, string tier3) => Rows[ladder] = new[] { tier0, tier1, tier2, tier3 };

    private static void Natural(string kinds) { foreach (var kind in kinds.Split(' ', StringSplitOptions.RemoveEmptyEntries)) NaturalKinds.Add(kind); }

    private static readonly bool Built = Build();

    private static bool Build()
    {
        Words("plain",    "scratch|scratches|",  "hit|hits|",        "hit|hits| hard",         "smash|smashes| brutally");
        Words("blunt",    "bruise|bruises|",     "thump|thumps|",    "crack|cracks| hard",     "smash|smashes| brutally");
        Words("slashing", "nick|nicks|",         "cut|cuts|",        "slash|slashes| deeply",  "cleave|cleaves| to the bone");
        Words("piercing", "prick|pricks|",       "stab|stabs|",      "pierce|pierces| deeply", "run|runs| through");
        Words("archery",  "graze|grazes|",       "shoot|shoots|",    "shoot|shoots| squarely", "shoot|shoots| clean through");
        Words("bite",     "nip|nips|",           "bite|bites|",      "bite|bites| hard",       "maul|mauls| savagely");
        Words("claw",     "scratch|scratches|",  "claw|claws|",      "rake|rakes| deeply",     "rend|rends| to ribbons");
        Words("gore",     "nudge|nudges|",       "gore|gores|",      "gore|gores| deeply",     "rip|rips| open");
        Words("kick",     "scuff|scuffs|",       "kick|kicks|",      "kick|kicks| hard",       "stamp|stamps| flat");
        Words("slam",     "bump|bumps|",         "slam|slams|",      "slam|slams| hard",       "crush|crushes| brutally");
        Words("lash",     "flick|flicks|",       "lash|lashes|",     "lash|lashes| hard",      "thrash|thrashes| brutally");
        Words("touch",    "brush|brushes|",      "touch|touches|",   "chill|chills| deeply",   "wither|withers| utterly");
        Words("spit",     "spatter|spatters|",   "splash|splashes|", "splash|splashes| thickly", "drench|drenches| completely");
        Words("thorns",   "prick|pricks|",       "pepper|peppers|",  "pepper|peppers| thickly", "riddle|riddles| with thorns");
        Natural("bite claw gore kick slam lash touch spit thorns");
        return true;
    }

    /// <summary>The cell for a ladder and a tier (0 to 3): "you|them|how", for example "cleave|cleaves| to the bone". An unknown ladder is the plain one; the tier is kept in range.</summary>
    public string QueryWords(string ladder, int tier)
    {
        if (!Rows.TryGetValue(ladder ?? "", out var row)) row = Rows["plain"];
        return row[Math.Clamp(tier, 0, 3)];
    }

    /// <summary>The name of the ladder for an attack's kind, reach and damage type (any of them may be empty): see the order in the class summary.</summary>
    public string QueryLadder(string kind, string reach, string damage)
    {
        kind ??= ""; reach ??= ""; damage ??= "";
        if (NaturalKinds.Contains(kind)) return kind;
        if (reach == "ranged" && damage == "piercing") return "archery";
        if ((reach == "" || reach == "melee" || reach == "thrown") && damage is "blunt" or "slashing" or "piercing") return damage;
        return "plain";
    }

    /// <summary>The ladders, in the order of the table.</summary>
    public List<string> QueryLadders() => Rows.Keys.ToList();
}
