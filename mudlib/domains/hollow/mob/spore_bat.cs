// @inherit /std/monster

/// <summary>A small, fragile bat that feeds on the mushroom caps and darts out of reach between attacks.</summary>
public class SporeBat : Monster
{
    public override void Create()
    {
        Name = "bat";
        Ids.Add("bat");
        Ids.Add("spore bat");
        ShortDesc = "a spore bat";
        LongDesc = "A bat with leathery wings the colour of old mould and a glowing blue dust clinging to its fur. It flits in and out of the light, so that you are never quite sure where its next bite will come from.";
        Level = 2;
        MaxHp = 6;
        DamageMin = 1;
        DamageMax = 3;
        ToHit = 1;
        Armor = 3;
        XpValue = 20;
        GoldMin = 0;
        GoldMax = 0;
        Aggressive = true;
    }
}
