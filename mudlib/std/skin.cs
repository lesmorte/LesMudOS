// @inherit /std/material

/// <summary>
/// The skin of any creature: one object for every beast, its kind held as the item property "kind" ("wolf"), which is saved
/// with it like its quality and quantity ("/std/skin?p.kind=wolf&amp;q=2&amp;n=3"). Its name, value and material id
/// ("wolf_skin") come from the beast table under /crafting: adding a beast is one line there, with no file per creature.
/// Skins of two kinds never stack together, because their properties differ. A skin with no kind, or a kind the table does not
/// know, is a worthless scrap that no recipe accepts. Shops can stock one by record: AddStock("/std/skin?p.kind=rabbit", 4).
/// </summary>
public class Skin : Material
{
    /// <summary>"skin" here, "blood" in /std/blood: which column of the beast table this object reads.</summary>
    protected virtual string Part => "skin";

    public override void Create()
    {
        Describe();
    }

    protected override void StateChanged() => Describe();

    /// <summary>Set the names and value from the beast table for the current kind.</summary>
    protected void Describe()
    {
        var kind = QueryProperty("kind") ?? "";
        List<string>? info = null;
        if (kind != "")
        {
            try { info = LoadObject("/secure/crafting").Call<List<string>>("QueryBeast", kind); }
            catch (MudRuntimeException) { info = null; }
        }
        Ids.Clear();
        Ids.Add(Part);
        if (info == null || info.Count < 4)
        {
            Name = "scrap of " + Part;
            ShortDesc = Part == "skin" ? "a scrap of skin" : "a smear of blood";
            Plural = Part == "skin" ? "scraps of skin" : "smears of blood";
            LongDesc = "Whatever creature this came from, nothing can be made of it.";
            Value = 0;
            return;
        }
        var display = info[0];             // "Wolf"
        var lower = display.ToLowerInvariant();
        Value = int.TryParse(Part == "skin" ? info[1] : info[2], out var v) ? v : 0;
        Name = display + (Part == "skin" ? " Skin" : " Blood");
        Ids.Add(lower + " " + Part);
        Ids.Add(Part + " of " + lower);
        Ids.Add(kind + "_" + Part);
        if (Part == "skin")
        {
            ShortDesc = (StartsWithVowel(lower) ? "an " : "a ") + lower + " skin";
            Plural = lower + " skins";
            Ids.Add("hide");
            LongDesc = $"The skin of {Article(lower)}, scraped and rolled for carrying.";
        }
        else
        {
            ShortDesc = "a vial of " + lower + " blood";
            Plural = "vials of " + lower + " blood";
            Ids.Add("vial");
            Ids.Add("vial of " + lower + " blood");
            LongDesc = $"A stoppered glass vial of {lower} blood, dark and still.";
        }
    }

    private static bool StartsWithVowel(string word) => word.Length > 0 && "aeiou".IndexOf(word[0]) >= 0;

    private static string Article(string word) => (StartsWithVowel(word) ? "an " : "a ") + word;
}
