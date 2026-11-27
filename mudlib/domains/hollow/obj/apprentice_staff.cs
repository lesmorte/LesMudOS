// @inherit /std/weapon

public class ApprenticesStaff : Weapon
{
    public override void Create()
    {
        Name = "Apprentice's Staff";
        Ids.Add("staff");
        Ids.Add("apprentice's staff");
        ShortDesc = "an apprentice's staff";
        LongDesc = "A walking staff of pale ash, its head wrapped in leather and scratched with the first runes a student learns. It is better at pointing than at hitting, but it does both.";
        Category = "staff";
        Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("staff"); Tags.Add("wood");
        DamageMin = 7;
        DamageMax = 11;
        ToHit = 1;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 10;
    }
}
