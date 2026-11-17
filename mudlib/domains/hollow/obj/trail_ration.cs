// @inherit /std/consumable

public class TrailRation : Consumable
{
    public override void Create()
    {
        Name = "Trail Ration";
        Ids.Add("ration");
        Ids.Add("food");
        ShortDesc = "a trail ration";
        LongDesc = "A wrapped parcel of hard biscuit, dried apple and salted meat. Filling, if not delicious.";
        HealAmount = 5;
        UseVerb = "eat";
        UseMessage = "You chew through the dry biscuit and salted meat.";
        Value = 4;
    }
}
