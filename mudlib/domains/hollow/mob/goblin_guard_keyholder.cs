// @inherit /std/monster

/// <summary>The goblin guard who keeps the Rusty Key for the locked door in the Mushroom Cavern.</summary>
public class GoblinGuardKeyholder : Monster
{
    public override void Create()
    {
        Name = "guard";
        Ids.Add("goblin");
        Ids.Add("guard");
        Ids.Add("goblin guard");
        ShortDesc = "a goblin guard";
        LongDesc = "A broad, scarred goblin in a dented iron breastplate, with a spiked shield strapped to one arm and a short-handled hammer in the other hand. A heavy ring of keys hangs from its belt, and it keeps one hand on them whenever you come near.";
        Level = 2;
        MaxHp = 16;
        DamageMin = 2;
        DamageMax = 3;
        ToHit = 0;
        Armor = 2;
        XpValue = 32;
        GoldMin = 4;
        GoldMax = 12;
        Aggressive = true;
        AddLoot("/domains/hollow/obj/rusty_key", 100);
    }
}
