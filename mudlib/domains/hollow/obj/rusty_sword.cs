// @inherit /std/weapon

public class RustySword : Weapon
{
    public override void Create()
    {
        Name = "Rusty Sword";
        Ids.Add("sword");
        ShortDesc = "a rusty sword";
        LongDesc = "A short sword, pitted with rust but honed to a decent edge on Torren's wheel.";
        DamageMin = 2;
        DamageMax = 5;
        Value = 10;
    }
}
