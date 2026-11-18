// @inherit /std/pclass

/// <summary>Mage archetype. Numbers and kit are in docs/CLASS_SYSTEM.md section 7.</summary>
public class MageClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Mage";
        Tier = "archetype";
        SortOrder = 4;
        Title = "Mage";
        Tagline = "Raw arcane power, and little else. Fragile, and best at a distance.";
        Description = "The mage fights with spells, wields dagger or staff and wears light armor only. Starting HP 24, 100 Mana but only 30 Stamina. Spell: Arcane Bolt. Skill: Magical Lore. Trait: Mana Efficiency.";
        StartHp = 24;
        StartReaction = 2.5;
        MaxStamina = 30;
        MaxMana = 100;
        StaminaRegen = 3;
        ManaRegen = 8;
        AttackRate = 0.96;
        StartGold = 15;
        Weapons.AddRange(new[] { "dagger", "staff" });
        ArmorTypes.AddRange(new[] { "light" });
        Shield = false;
        Skills.AddRange(new[] { "arcane_bolt", "magical_lore" });
        Traits.AddRange(new[] { "mana_efficiency" });
        Kit.AddRange(new[] { "/domains/hollow/obj/apprentice_staff", "/domains/hollow/obj/healing_potion" });
    }
}
