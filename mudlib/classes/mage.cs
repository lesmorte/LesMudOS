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
        Description = "The mage fights with spells, wields dagger or staff and wears light armor only. Starting Vitality 21, 100 Focus but only 30 Endurance. Spell: Arcane Bolt. Skill: Magical Lore. Trait: Focus Efficiency.";
        StartReaction = 2.5;
        VitalityBase = 4;
        VitalityPerLevel = 7;
        EnduranceBase = 14;
        EndurancePerLevel = 1;
        FocusBase = 71;
        FocusPerLevel = 4;
        StartAttributes = new() { ["might"] = 5, ["grace"] = 5, ["vigor"] = 5, ["insight"] = 9, ["spirit"] = 7, ["resolve"] = 5 };
        AttackRate = 0.96;
        StartGold = 15;
        Weapons.AddRange(new[] { "dagger", "staff" });
        ArmorTypes.AddRange(new[] { "light" });
        Shield = false;
        Skills.AddRange(new[] { "arcane_bolt", "magical_lore" });
        Traits.AddRange(new[] { "focus_efficiency" });
        Kit.AddRange(new[] { "/domains/hollow/obj/apprentice_staff", "/domains/hollow/obj/healing_potion" });
    }
}
