// @inherit /std/weapon

/// <summary>The improved step of the dagger line: sold by Torren.</summary>
public class SteelDirk : Weapon
{
    public override void Create()
    {
        Name = "Steel Dirk";
        Ids.Add("dagger");
        Ids.Add("dirk");
        Ids.Add("steel dirk");
        ShortDesc = "a steel dirk";
        LongDesc = "A narrow, triple-edged dirk of good steel with a plain wire-bound grip. It is quick, it is quiet, and it is not meant for anything you would say out loud.";
        Category = "dagger";
        Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("dagger"); Tags.Add("steel");
        DamageMin = 8;
        DamageMax = 12;
        ToHit = 1;
        ReactionMod = 0.0;
        Recovery = 0.5;
        Value = 45;
    }
}
