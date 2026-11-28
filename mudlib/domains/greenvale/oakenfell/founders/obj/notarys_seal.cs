// @inherit /std/armor

/// <summary>The Notary's Seal (docs/specs/bureaucrat.md section 4): a trinket on a cord, +2 Authority (the derived statistic: Living.StatRaw adds an item's StatBonus for any key). The first is a gift at the end of Jury Duty.</summary>
public class NotarysSeal : Armor
{
    public override void Create()
    {
        Name = "Notary's Seal";
        Ids.Add("seal");
        Ids.Add("notary's seal");
        Ids.Add("trinket");
        ShortDesc = "a Notary's seal";
        LongDesc = "A brass seal-die with a plain wooden grip, hung on a cord. What it presses becomes official, and a good deal of what it presses becomes binding.";
        Slot = "trinket";
        Tags.Add("trinket"); Tags.Add("steel");
        ReactionMod = 0.0;
        ArmorValue = 0;
        StatBonus["authority"] = 2;
        Magic = "the Chancery's mark: orders served by its wearer carry more weight";
        QuestItem = true;
        Bound = true;
        BoundLine = "It was issued to you. It is not to be sold.";
        Value = 0;
    }
}
