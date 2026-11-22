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
        Description = "The adventurer wields most weapons, wears light armor and may carry a shield. Starting Vitality 30, 70 Endurance and 70 Focus, and the best gold. Skills: Double Strike, Appraise. Traits: Survival Training, Improvised Combat, Traveler's Instinct, Jack of All Trades.";
        StartReaction = 2.5;
        VitalityBase = 10;
        VitalityPerLevel = 8;
        EnduranceBase = 50;
        EndurancePerLevel = 2;
        FocusBase = 50;
        FocusPerLevel = 2;
        StartAttributes = new() { ["might"] = 6, ["grace"] = 6, ["vigor"] = 6, ["insight"] = 6, ["spirit"] = 6, ["resolve"] = 6 };
        AttackRate = 0.98;
        StartGold = 20;
        Weapons.AddRange(new[] { "dagger", "sword", "spear", "axe", "mace", "staff" });
        ArmorTypes.AddRange(new[] { "light" });
        Shield = true;
        Skills.AddRange(new[] { "double_strike", "appraise" });
        Traits.AddRange(new[] { "survival_training", "improvised_combat", "travelers_instinct", "jack_of_all_trades" });
        Kit.AddRange(new[] { "/domains/hollow/obj/training_spear", "/domains/hollow/obj/trail_ration", "/domains/hollow/obj/trail_ration" });
        Description += " Starts even in all six attributes: Might, Grace, Vigor, Insight, Spirit and Resolve.";
    }
}
