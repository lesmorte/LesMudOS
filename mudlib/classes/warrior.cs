// @inherit /std/pclass

/// <summary>Warrior archetype. Numbers and kit are in docs/CLASS_SYSTEM.md section 7.</summary>
public class WarriorClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Warrior";
        Tier = "archetype";
        SortOrder = 1;
        Title = "Warrior";
        Tagline = "Steel and stubbornness. The sturdiest of the five, slow to act and hard to put down.";
        Description = "The warrior fights in the front rank with any weapon, in any armor, behind a shield. Starting Vitality 35, slow to react (3.0), 100 Endurance and little Focus. Skill: Shield Bash. Traits: Defensive Training, Weapon Proficiency.";
        StartReaction = 3.0;
        VitalityBase = 13;
        VitalityPerLevel = 8;
        EnduranceBase = 79;
        EndurancePerLevel = 2;
        FocusBase = 4;
        FocusPerLevel = 1;
        StartAttributes = new() { ["might"] = 8, ["grace"] = 5, ["vigor"] = 7, ["insight"] = 5, ["spirit"] = 5, ["resolve"] = 6 };
        AttackRate = 1.00;
        StartGold = 10;
        Weapons.AddRange(new[] { "*" });
        ArmorTypes.AddRange(new[] { "light", "medium", "heavy" });
        Shield = true;
        Skills.AddRange(new[] { "shield_bash" });
        Traits.AddRange(new[] { "defensive_training", "weapon_proficiency" });
        Kit.AddRange(new[] { "/domains/hollow/obj/rusty_sword", "/domains/hollow/obj/small_shield" });
    }
}
