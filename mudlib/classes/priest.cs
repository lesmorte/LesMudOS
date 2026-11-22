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
        Description = "The priest wields mace or staff, wears light or medium armor and may carry a shield. Starting Vitality 26, 100 Focus but only 40 Endurance. Spells: Minor Heal, Blessing. Trait: Divine Knowledge.";
        StartReaction = 2.5;
        VitalityBase = 7;
        VitalityPerLevel = 7;
        EnduranceBase = 23;
        EndurancePerLevel = 1;
        FocusBase = 75;
        FocusPerLevel = 4;
        StartAttributes = new() { ["might"] = 5, ["grace"] = 5, ["vigor"] = 6, ["insight"] = 6, ["spirit"] = 9, ["resolve"] = 5 };
        AttackRate = 0.97;
        StartGold = 15;
        Weapons.AddRange(new[] { "mace", "staff" });
        ArmorTypes.AddRange(new[] { "light", "medium" });
        Shield = true;
        Skills.AddRange(new[] { "minor_heal", "blessing" });
        Traits.AddRange(new[] { "divine_knowledge" });
        Kit.AddRange(new[] { "/domains/hollow/obj/novice_mace", "/domains/hollow/obj/healing_potion" });
        Description += " Leans on Spirit, with Vigor and Insight behind it.";
    }
}
