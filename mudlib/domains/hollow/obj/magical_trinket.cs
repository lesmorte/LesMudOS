// @inherit /std/armor

/// <summary>A worn charm from the hidden cache. Its only numbers are the extra hit points it gives its wearer.</summary>
public class MagicalTrinket : Armor
{
    public override void Create()
    {
        Name = "Magical Trinket";
        Ids.Add("trinket");
        ShortDesc = "a magical trinket";
        LongDesc = "A small silver disc on a cord, warm to the touch and engraved with a crow that seems to shift its wings when you look away. It makes you feel steadier on your feet.";
        Slot = "trinket";
        ReactionMod = 0.0;
        ArmorValue = 0;
        MaxHpBonus = 5;
        Value = 40;
    }
}
