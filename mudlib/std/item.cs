// @inherit /std/thing

/// <summary>
/// A portable object. Set Value (gold), QuestItem and ReadText in Create() as needed.
///
/// Every item also carries instance state, saved with it wherever it is kept (a character's inventory, the lost-property
/// store): a quality (0 Poor to 5 Legendary, default Common), a quantity (default 1; only stackable items have more) and a
/// few named string properties for later systems. Set Stackable = true in Create() for a material that stacks; Plural
/// overrides the plural name. See docs/BUILDING.md, "Quality and stacks".
/// </summary>
public class Item : Thing
{
    /// <summary>Worth in gold of one item of Common quality. Quality multiplies it (QueryValue). Shopkeepers pay half; 0 means they will not buy it.</summary>
    protected int Value;
    /// <summary>Quest items cannot be sold or given away.</summary>
    protected bool QuestItem;
    /// <summary>If set, "read <thing>" shows this text.</summary>
    protected string? ReadText;
    /// <summary>A magical property, in words, that Magical Lore and Appraise reveal ("raises maximum Vitality by 5"). Empty for none.</summary>
    protected string Magic = "";
    /// <summary>A stackable item is one object with a quantity. Two stacks of the same file and quality merge when they come to rest together.</summary>
    protected bool Stackable;
    /// <summary>The plural name, used for a quantity above one ("3 wolf skins"). Default: the short description without its article, plus "s".</summary>
    protected string? Plural;
    /// <summary>What the item adds to its wearer's attributes while equipped ("grace" 2 is +2 Grace). No existing item has any.</summary>
    protected Dictionary<string, int> AttributeBonus = new();
    /// <summary>
    /// What the item adds to a derived statistic while equipped, in the statistic's own unit ("crit" 5 is +5% Critical Chance).
    /// Keys as Living.QueryStat: physical_power, precision, evasion, crit, spell_power, healing_power, mental_resistance,
    /// physical_resistance, carry_capacity, gathering, crafting.
    /// </summary>
    protected Dictionary<string, double> StatBonus = new();

    /// <summary>The item's bonus to an attribute while equipped.</summary>
    public int QueryAttributeBonus(string name) => AttributeBonus.TryGetValue(name ?? "", out var value) ? value : 0;

    /// <summary>The item's bonus to a derived statistic while equipped.</summary>
    public double QueryStatBonus(string key) => StatBonus.TryGetValue(key ?? "", out var value) ? value : 0;

    // ---- Quality: the one table ----

    public const int Poor = 0, Common = 1, Fine = 2, Excellent = 3, Masterwork = 4, Legendary = 5;

    /// <summary>The quality tiers in order, as the words players see and type.</summary>
    public static readonly string[] QualityNames = { "poor", "common", "fine", "excellent", "masterwork", "legendary" };

    /// <summary>What each quality multiplies the base value by.</summary>
    public static readonly double[] QualityMultipliers = { 0.5, 1.0, 1.5, 2.5, 4.0, 8.0 };

    /// <summary>The largest quantity one stack may hold.</summary>
    public const int MaxStack = 1000000;

    /// <summary>The value of one item of this base value at this quality: rounded, at least 1 (a base value of 0 stays 0).</summary>
    public static int ValueFor(int baseValue, int quality)
    {
        if (baseValue <= 0) return 0;
        var q = Math.Clamp(quality, Poor, Legendary);
        return Math.Max(1, (int)Math.Round(baseValue * QualityMultipliers[q], MidpointRounding.AwayFromZero));
    }

    /// <summary>The quality tier for a word ("fine"), or -1.</summary>
    public static int QualityOf(string word) => Array.IndexOf(QualityNames, word.ToLowerInvariant());

    // Code from these trees may change an item's state. Sandbox code (/wiz) may change only sandbox items.
    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private const int MaxProperties = 16;
    private const int MaxPropertyLength = 256;

    private int quality = Common;
    private int quantity = 1;
    private readonly Dictionary<string, string> properties = new();

    public override bool CanGet() => true;

    public int QueryBaseValue() => Value;

    /// <summary>The value of one of these, quality included.</summary>
    public int QueryValue() => ValueFor(Value, quality);

    /// <summary>The value of the whole stack.</summary>
    public long QueryTotalValue() => (long)QueryValue() * quantity;

    public bool QueryQuestItem() => QuestItem;

    public string? QueryReadText() => ReadText;

    public bool QueryStackable() => Stackable;

    public int QueryQuality() => quality;

    public string QueryQualityName() => QualityNames[quality];

    public int QueryQuantity() => quantity;

    public string? QueryProperty(string key) => properties.GetValueOrDefault(key);

    public Dictionary<string, string> QueryProperties() => new(properties);

    // ---- Changing the state ----

    private static bool Trusted(ObjectRef? ob)
    {
        if (ob == null || ob.ProgramPath.Contains("..")) return false;
        foreach (var tree in TrustedTrees) if (ob.ProgramPath.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>
    /// May the calling object change this item's state? Trusted code may; sandbox code only on a sandbox item. The caller is
    /// taken from PreviousObject(), never from an argument.
    /// </summary>
    private bool MayChange() => !Trusted(Self) || Trusted(PreviousObject());

    /// <summary>Set the quality (0 to 5). False if refused.</summary>
    public bool SetQuality(int q)
    {
        if (!MayChange() || q < Poor || q > Legendary) return false;
        quality = q;
        return true;
    }

    /// <summary>Set the quantity of a stackable item (1 to MaxStack). False if refused or not stackable.</summary>
    public bool SetQuantity(int n)
    {
        if (!MayChange() || n < 1 || n > MaxStack || (!Stackable && n != 1)) return false;
        quantity = n;
        return true;
    }

    /// <summary>Set a named property (null or "" removes it). At most 16, of up to 256 characters each. False if refused.</summary>
    public bool SetProperty(string key, string? value)
    {
        if (!MayChange() || string.IsNullOrEmpty(key) || key.Length > 64) return false;
        if (!PropertyAllowed(key, value ?? "")) return false;
        if (string.IsNullOrEmpty(value)) { properties.Remove(key); StateChanged(); return true; }
        if (value.Length > MaxPropertyLength || (!properties.ContainsKey(key) && properties.Count >= MaxProperties)) return false;
        properties[key] = value;
        StateChanged();
        return true;
    }

    /// <summary>
    /// May this property be given this value now? A type whose identity is a property (the generic skin and blood of /std/skin and
    /// /std/blood) overrides it to refuse some values to some callers; PreviousObject() is still the caller of SetProperty or
    /// RestoreState. Default: allowed.
    /// </summary>
    protected virtual bool PropertyAllowed(string key, string value) => true;

    /// <summary>Called after the quality, the quantity or a property changed through SetProperty, RestoreState or a split. Default: nothing.</summary>
    protected virtual void StateChanged() { }

    // ---- Saving the state ----

    /// <summary>
    /// The instance state as text, for save records: "" for the default state, otherwise "q=2&amp;n=5&amp;p.key=value" with
    /// '%', '&amp;', '=' and '?' in keys and values written as %XX. A record is the program path, then "?" and this.
    /// </summary>
    public string QueryState()
    {
        var parts = new List<string>();
        if (quality != Common) parts.Add("q=" + quality);
        if (quantity != 1) parts.Add("n=" + quantity);
        foreach (var key in properties.Keys.OrderBy(k => k, StringComparer.Ordinal)) parts.Add("p." + Encode(key) + "=" + Encode(properties[key]));
        return string.Join("&", parts);
    }

    /// <summary>Restore state written by QueryState. Unknown or malformed parts are ignored. False if refused, or if a property was refused (PropertyAllowed).</summary>
    public bool RestoreState(string state)
    {
        if (!MayChange()) return false;
        var refused = false;
        foreach (var part in (state ?? "").Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            var key = part.Substring(0, eq);
            var value = part.Substring(eq + 1);
            if (key == "q" && int.TryParse(value, out var q) && q >= Poor && q <= Legendary) quality = q;
            else if (key == "n" && int.TryParse(value, out var n) && n >= 1 && n <= MaxStack && Stackable) quantity = n;
            else if (key.StartsWith("p.") && key.Length > 2)
            {
                var name = Decode(key.Substring(2));
                var text = Decode(value);
                if (!PropertyAllowed(name, text)) refused = true;
                else if (name.Length <= 64 && text.Length <= MaxPropertyLength && (properties.ContainsKey(name) || properties.Count < MaxProperties)) properties[name] = text;
            }
        }
        StateChanged();
        return !refused; // a property this type refuses to this caller (Goblin Blood from a shop) fails the whole restore
    }

    private static string Encode(string text)
    {
        var result = new StringBuilder();
        foreach (var c in text)
        {
            if (c is '%' or '&' or '=' or '?' || c < ' ') result.Append('%').Append(((int)c).ToString("X2"));
            else result.Append(c);
        }
        return result.ToString();
    }

    private static string Decode(string text)
    {
        if (text.IndexOf('%') < 0) return text;
        var result = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '%' && i + 2 < text.Length && IsHex(text[i + 1]) && IsHex(text[i + 2]))
            {
                result.Append((char)Convert.ToInt32(text.Substring(i + 1, 2), 16));
                i += 2;
            }
            else result.Append(text[i]);
        }
        return result.ToString();
    }

    private static bool IsHex(char c) => c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    // ---- Stacks ----

    /// <summary>Would this item and another merge into one stack: both stackable, same file, quality and properties?</summary>
    public bool CanStackWith(ObjectRef other)
    {
        if (!Stackable || other == Self || !other.IsAlive || other.ProgramPath != Self.ProgramPath) return false;
        if (!other.Call<bool>("QueryStackable") || other.Call<int>("QueryQuality") != quality) return false;
        var theirs = other.Call<Dictionary<string, string>>("QueryProperties") ?? new Dictionary<string, string>();
        return theirs.Count == properties.Count && theirs.All(kv => properties.TryGetValue(kv.Key, out var v) && v == kv.Value);
    }

    private static bool Equipped(ObjectRef? holder, ObjectRef item) => holder != null && holder.Call<bool>("IsEquipped", item);

    /// <summary>
    /// Merge this stack into a matching stack in the same container, if there is one. Returns the stack that holds the
    /// items afterwards: the other stack (this object is then destructed), or this one. Call it after moving a stackable
    /// item; the standard commands and the helpers in /std/thing do. Equipped items never merge.
    /// </summary>
    public ObjectRef Settle()
    {
        var env = Environment();
        if (!Stackable || env == null || Equipped(env, Self)) return Self;
        foreach (var other in AllInventory(env))
        {
            if (!CanStackWith(other) || Equipped(env, other)) continue;
            var theirs = other.Call<int>("QueryQuantity");
            if ((long)theirs + quantity > MaxStack) continue;
            var n = quantity;
            if (other.Call<bool>("ReceiveStack", n) != true) continue;
            quantity = 0;
            Destruct();
            return other;
        }
        return Self;
    }

    /// <summary>Part of Settle: another stack of the same file, in the same container, gives this one its items. Only that stack may call it.</summary>
    public bool ReceiveStack(int n)
    {
        var giver = PreviousObject();
        if (giver == null || giver == Self || !giver.IsAlive || giver.ProgramPath != Self.ProgramPath) return false;
        if (Environment(giver) != Environment() || giver.Call<int>("QueryQuantity") != n || !CanStackWith(giver)) return false;
        if (n < 1 || (long)quantity + n > MaxStack) return false;
        quantity += n;
        return true;
    }

    /// <summary>
    /// Split n items off this stack into a new object of the same file and state, put beside it in the same container, and
    /// return the new object. With n at or above the quantity, returns this object. Null if refused (no container, n below
    /// 1, or a caller that may not change this item).
    /// </summary>
    public ObjectRef? Split(int n)
    {
        if (n < 1) return null;
        if (n >= quantity) return Self;
        var env = Environment();
        if (env == null || !MayChange()) return null;
        var part = CloneObject(Self.ProgramPath);
        if (part.Call<bool>("TakeSplit", quality, n, QueryState()) != true)
        {
            part.Call("Vanish");
            return null;
        }
        quantity -= n;
        MoveObject(part, env);
        return part;
    }

    /// <summary>Part of Split: a new object takes the state of the stack it is split from. Only an object of the same file may call it, once, at once.</summary>
    public bool TakeSplit(int q, int n, string state)
    {
        var from = PreviousObject();
        if (from == null || from == Self || from.ProgramPath != Self.ProgramPath || Environment() != null || !Stackable) return false;
        if (n < 1 || n >= from.Call<int>("QueryQuantity") || q != from.Call<int>("QueryQuality")) return false;
        properties.Clear();
        foreach (var part in state.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq > 2 && part.StartsWith("p.")) properties[Decode(part.Substring(2, eq - 2))] = Decode(part.Substring(eq + 1));
        }
        quality = q;
        quantity = n;
        StateChanged();
        return true;
    }

    /// <summary>Use up one item of the stack (eaten, drunk, spent); the last one destructs the object.</summary>
    protected void UseOne()
    {
        if (quantity > 1) quantity--;
        else Destruct();
    }

    // ---- Names ----

    private static string StripArticle(string text, out string article)
    {
        foreach (var a in new[] { "a ", "an ", "the " })
            if (text.StartsWith(a, StringComparison.OrdinalIgnoreCase)) { article = text.Substring(0, a.Length - 1); return text.Substring(a.Length); }
        article = "";
        return text;
    }

    /// <summary>The plural name: Plural, or the short description without its article plus "s" ("es" after s, x, z, ch, sh).</summary>
    public string QueryPlural()
    {
        if (Plural != null) return Plural;
        var single = StripArticle(ShortDesc ?? Name, out _);
        return single + (single.EndsWith("s") || single.EndsWith("x") || single.EndsWith("z") || single.EndsWith("ch") || single.EndsWith("sh") ? "es" : "s");
    }

    /// <summary>How n of these are described: "a fine wolf skin", "3 fine wolf skins". Common has no prefix.</summary>
    public string DescribeCount(int n)
    {
        if (n > 1) return $"{n} {(quality == Common ? "" : QualityNames[quality] + " ")}{QueryPlural()}";
        var single = ShortDesc ?? Name;
        if (quality == Common) return single;
        var rest = StripArticle(single, out var article);
        var word = QualityNames[quality];
        if (article == "") return word + " " + rest;
        if (article.Equals("the", StringComparison.OrdinalIgnoreCase)) return article + " " + word + " " + rest;
        return ("aeiou".IndexOf(word[0]) >= 0 ? "an " : "a ") + word + " " + rest;
    }

    public override string? Short() => DescribeCount(quantity);

    /// <summary>
    /// Answers to its names, to their plurals ("skins", Plural) and to either with its own quality word in front ("fine
    /// wolf skin"). Every name it answered to before quality existed it still answers to.
    /// </summary>
    public override bool Id(string id)
    {
        if (base.Id(id) || IsNoun(id)) return true;
        var space = id.IndexOf(' ');
        if (space <= 0) return false;
        var q = QualityOf(id.Substring(0, space));
        return q == quality && IsNoun(id.Substring(space + 1));
    }

    private bool IsNoun(string word)
    {
        if (base.Id(word)) return true;
        if (word == QueryPlural().ToLowerInvariant()) return true;
        if (word.EndsWith("es") && word.Length > 3 && base.Id(word.Substring(0, word.Length - 2))) return true;
        return word.EndsWith("s") && word.Length > 2 && base.Id(word.Substring(0, word.Length - 1));
    }

    /// <summary>The quality and count, in words, for look and appraise. Empty for one Common item that does not stack.</summary>
    protected string QualityNote()
    {
        if (!Stackable && quality == Common && quantity == 1) return "";
        return quantity > 1 ? $"There are {quantity} of them, of {QualityNames[quality]} quality." : $"It is of {QualityNames[quality]} quality.";
    }

    public override string Long()
    {
        var note = QualityNote();
        return base.Long() + (note == "" ? "" : note + "\n");
    }

    /// <summary>Skills this item grants while it is equipped (ids in /skills). None by default; a builder overrides it.</summary>
    public virtual List<string> QueryGrantedSkills() => new();

    /// <summary>The exact numbers, for Magical Lore and Appraise. Weapons and armor add their own lines.</summary>
    public virtual string Appraisal() => $"{Cap(Short() ?? Name)}. Worth {QueryValue()} gold{(quantity > 1 ? " each" : "")}." + (Magic != "" ? $" Magical property: {Magic}." : " It holds no magic.") + AppraisalQuality() + "\n";

    /// <summary>" Quality: fine." for Appraisal, or "" for an ordinary Common item.</summary>
    protected string AppraisalQuality() => !Stackable && quality == Common ? "" : $" Quality: {QualityNames[quality]}.";

    /// <summary>The equipment slot this item goes in ("weapon", "head", ...), or "" if it cannot be worn or wielded.</summary>
    public virtual string QuerySlot() => "";

    /// <summary>What this item adds to the Reaction Time of whoever has it equipped. Weapons and armor override it.</summary>
    public virtual double QueryReactionMod() => 0;

    /// <summary>Called after a player has picked this item up. ThisPlayer() is the taker.</summary>
    protected virtual void OnGet(ObjectRef taker) { }

    /// <summary>Called by the "get" command after the item has been moved to the taker.</summary>
    public void Taken(ObjectRef taker)
    {
        if (Environment() == taker) OnGet(taker);
    }

    /// <summary>Remove this item (the whole stack) from the game (sold, eaten, handed in).</summary>
    public void Vanish() => Destruct();

    public override string Stat() => base.Stat() + $"  Value {Value} gold, quest item {QuestItem}\n" +
        $"  Quality {QualityNames[quality]}, quantity {quantity}, stackable {Stackable}, unit value {QueryValue()}" +
        (properties.Count > 0 ? ", properties " + string.Join(", ", properties.Select(kv => kv.Key + "=" + kv.Value)) : "") + "\n" +
        (AttributeBonus.Count + StatBonus.Count > 0 ? "  Bonuses " + string.Join(", ", AttributeBonus.Select(kv => kv.Key + " " + (kv.Value >= 0 ? "+" : "") + kv.Value).Concat(StatBonus.Select(kv => $"{kv.Key} {Num(kv.Value)}"))) + "\n" : "");
}
