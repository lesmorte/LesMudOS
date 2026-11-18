// @inherit /std/pclass

/// <summary>Priest archetype. Numbers and kit are in docs/CLASS_SYSTEM.md section 7.</summary>
public class PriestClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Priest";
        Tier = "archetype";
        SortOrder = 3;
        Title = "Priest";
        Tagline = "Healing hands and a hard mace. Keeps itself and its friends on their feet.";
        Description = "The priest wields mace or staff, wears light or medium armor and may carry a shield. Starting HP 26, 100 Mana but only 40 Stamina. Spells: Minor Heal, Blessing. Trait: Divine Knowledge.";
        StartHp = 26;
        StartReaction = 2.5;
        MaxStamina = 40;
        MaxMana = 100;
        StaminaRegen = 4;
        ManaRegen = 4;
        AttackRate = 0.97;
        StartGold = 15;
        Weapons.AddRange(new[] { "mace", "staff" });
        ArmorTypes.AddRange(new[] { "light", "medium" });
        Shield = true;
        Skills.AddRange(new[] { "minor_heal", "blessing" });
        Traits.AddRange(new[] { "divine_knowledge" });
        Kit.AddRange(new[] { "/domains/hollow/obj/novice_mace", "/domains/hollow/obj/healing_potion" });
    }
}
