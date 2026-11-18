// @inherit /std/weapon

public class TrainingSpear : Weapon
{
    public override void Create()
    {
        Name = "Training Spear";
        Ids.Add("spear");
        ShortDesc = "a training spear";
        LongDesc = "A long ash spear with a blunted-looking but perfectly serviceable iron head. Good reach and easy to handle.";
        DamageMin = 8;
        DamageMax = 12;
        ReactionMod = 0.5;
        Recovery = 1.0;
        ToHit = 1;
        Category = "spear";
        Value = 8;
    }
}
