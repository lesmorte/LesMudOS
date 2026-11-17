// @inherit /std/weapon

public class RustySword : Weapon
{
    public override void Create()
    {
        Name = "Rusty Sword";
        Ids.Add("sword");
        ShortDesc = "a rusty sword";
        LongDesc = "A short sword, pitted with rust but honed to a decent edge on Torren's wheel.";
        DamageMin = 9;
        DamageMax = 14;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 10;
    }
}
