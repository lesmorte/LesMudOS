// @inherit /std/weapon

/// <summary>The fine step of the sword line: a rare drop of the Goblin Elite Guard.</summary>
public class CaptainsSabre : Weapon
{
    public override void Create()
    {
        Name = "Captain's Sabre";
        Ids.Add("sabre");
        Ids.Add("sword");
        Ids.Add("captain's sabre");
        Ids.Add("captains sabre");
        ShortDesc = "a captain's sabre";
        LongDesc = "A curved sabre with a brass guard worn smooth by a long-dead hand. Somebody once carried it at the head of a company, and the edge still has the memory of a drill ground in it.";
        Category = "sword";
        DamageMin = 14;
        DamageMax = 20;
        ReactionMod = 1.0;
        Recovery = 1.0;
        Value = 90;
    }
}
