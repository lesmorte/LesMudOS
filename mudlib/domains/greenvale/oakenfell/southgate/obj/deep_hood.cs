// @inherit /std/armor

/// <summary>
/// The Deep Traveler's Hood: head slot, armour 0, any calling. Its one property is HidesFromWho: its wearer is left off the who list of
/// everyone who is not a wizard (docs/specs/who-and-visibility.md section 5). Nothing else changes: the wearer is seen in the room and
/// hears tells. Sold by Corwin Dray, the outfitter in Southgate, Oakenfell, for 120 gold.
/// </summary>
public class DeepTravelersHood : Armor
{
    public override void Create()
    {
        Name = "Deep Traveler's Hood";
        Ids.Add("hood");
        Ids.Add("deep hood");
        Ids.Add("traveler's hood");
        Ids.Add("deep traveler's hood");
        ShortDesc = "a deep traveler's hood";
        LongDesc = "A hood of heavy grey wool, cut deep enough to hide a face and long enough to fall over the shoulders. It has seen a good many roads and does not mind who knows it. It is no protection against a blade, but folk who wear it are rarely asked after.";
        ArmorValue = 0;
        Slot = "head";
        ReactionMod = 0.0;
        Category = "light";
        HidesFromWho = true;
        Value = 40;
    }
}
