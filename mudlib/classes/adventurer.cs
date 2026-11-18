// @inherit /std/pclass

/// <summary>Adventurer archetype. Numbers and kit are in docs/CLASS_SYSTEM.md section 7.</summary>
public class AdventurerClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Adventurer";
        Tier = "archetype";
        SortOrder = 5;
        Title = "Adventurer";
        Tagline = "A little of everything and a nose for the road. Hardy, resourceful and well supplied.";
        Description = "The adventurer wields most weapons, wears light armor and may carry a shield. Starting HP 30, 70 Stamina and 70 Mana, and the best gold. Skill: Appraise. Traits: Survival Training, Improvised Combat, Traveler's Instinct.";
        StartHp = 30;
        StartReaction = 2.5;
        MaxStamina = 70;
        MaxMana = 70;
        StaminaRegen = 6;
        ManaRegen = 5;
        AttackRate = 0.98;
        StartGold = 20;
        Weapons.AddRange(new[] { "dagger", "sword", "spear", "axe", "mace", "staff" });
        ArmorTypes.AddRange(new[] { "light" });
        Shield = true;
        Skills.AddRange(new[] { "appraise" });
        Traits.AddRange(new[] { "survival_training", "improvised_combat", "travelers_instinct" });
        Kit.AddRange(new[] { "/domains/hollow/obj/training_spear", "/domains/hollow/obj/trail_ration", "/domains/hollow/obj/trail_ration" });
    }
}
