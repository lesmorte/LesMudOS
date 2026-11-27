// @inherit /std/weapon

/// <summary>The fine step of the mace line: the reward of Sister Merewyn's quest, The Quiet Offering. Adds 1 Spirit while wielded.</summary>
public class ShrineBlessedMace : Weapon
{
    public override void Create()
    {
        Name = "Shrine-Blessed Mace";
        Ids.Add("mace");
        Ids.Add("blessed mace");
        Ids.Add("shrine mace");
        Ids.Add("shrine-blessed mace");
        ShortDesc = "a shrine-blessed mace";
        LongDesc = "A plain mace whose head has been washed in the shrine's still water and left a night in the open hand carved above its door. It looks like any other mace, until it is raised, and then it is very quiet.";
        Category = "mace";
        Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("mace"); Tags.Add("iron");
        DamageMin = 13;
        DamageMax = 18;
        ReactionMod = 0.5;
        Recovery = 1.0;
        AttributeBonus["spirit"] = 1;
        Magic = "blessed at the Shrine of the Quiet Hand: +1 Spirit while wielded";
        Value = 75;
    }
}
