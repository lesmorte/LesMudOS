// @inherit /std/weapon

public class TrainingSpear : Weapon
{
    public override void Create()
    {
        Name = "Training Spear";
        Ids.Add("spear");
        ShortDesc = "a training spear";
        LongDesc = "A long ash spear with a blunted-looking but perfectly serviceable iron head. Good reach and easy to handle.";
        DamageMin = 2;
        DamageMax = 5;
        ToHit = 1;
        Value = 8;
    }
}
