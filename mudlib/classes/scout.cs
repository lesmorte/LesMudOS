// @inherit /std/pclass

/// <summary>Scout archetype. Numbers and kit are in docs/CLASS_SYSTEM.md section 7.</summary>
public class ScoutClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Scout";
        Tier = "archetype";
        SortOrder = 2;
        Title = "Scout";
        Tagline = "Quick hands and quicker feet. Dodges, tracks and strikes where it counts.";
        Description = "The scout is the fastest of the five (Reaction Time 2.0) and wears light armor. Weapons: dagger, sword, spear, bow. Starting Vitality 21, 100 Endurance. Skills: Tracking, Precision Strike. Trait: Evasion.";
        StartReaction = 2.0;
        VitalityBase = 3;
        VitalityPerLevel = 8;
        EnduranceBase = 81;
        EndurancePerLevel = 2;
        FocusBase = 2;
        FocusPerLevel = 1;
        StartAttributes = new() { ["might"] = 5, ["grace"] = 9, ["vigor"] = 5, ["insight"] = 6, ["spirit"] = 5, ["resolve"] = 6 };
        AttackRate = 1.00;
        StartGold = 10;
        Weapons.AddRange(new[] { "dagger", "sword", "spear", "bow" });
        ArmorTypes.AddRange(new[] { "light" });
        Shield = false;
        Skills.AddRange(new[] { "tracking", "precision_strike" });
        Traits.AddRange(new[] { "evasion" });
        Kit.AddRange(new[] { "/domains/hollow/obj/hunting_bow", "/domains/hollow/obj/iron_dagger" });
        Description += " Leans on Grace, with Insight and Resolve behind it.";
    }
}
