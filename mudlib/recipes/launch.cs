// @inherit /std/recipe_set

/// <summary>The owner's ten launch recipes (docs/specs/crafting-system.md), all Simple or Novice, all near Vincent's Hollow.</summary>
public class CrLaunchRecipes : RecipeSet
{
    public override void Create()
    {
        Recipe("berry_juice", "Berry Juice")
            .Needs("blackberries").Needs("clean_water")
            .At("cooking fire").Skill("cooking", "simple")
            .Makes("berry_juice")
            .Description("Press blackberries and thin them with clean water.");
        Recipe("sweet_berry_juice", "Sweet Berry Juice")
            .Needs("berry_juice").Needs("honey")
            .At("cooking fire").Skill("cooking", "novice")
            .Makes("/domains/hollow/obj/craft/sweet_berry_juice")
            .Discovery("discovered")
            .Description("Stir honey into berry juice over the fire until it runs glossy.");
        Recipe("simple_bandage", "Simple Bandage")
            .Needs("cloth_strip")
            .Skill("herbalism", "simple")
            .Makes("/domains/hollow/obj/craft/simple_bandage")
            .Description("Fold, roll and tie a strip of clean cloth. Needs no station.");
        Recipe("herbal_bandage", "Herbal Bandage")
            .Needs("cloth_strip").Needs("healing_herb")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/hollow/obj/craft/herbal_bandage")
            .Description("Soak a cloth strip in crushed healing herb before rolling it.");
        Recipe("minor_healing_salve", "Minor Healing Salve")
            .Needs("healing_herb").Needs("wildflower")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/hollow/obj/craft/minor_healing_salve")
            .Description("Work a healing herb and a wildflower into a paste.");
        // The id and the output file keep their old name (stamina): the id is a key in saved characters (known recipes) and the file is a saved path.
        Recipe("minor_stamina_tonic", "Minor Endurance Tonic")
            .Needs("blueberries").Needs("honey")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/hollow/obj/craft/minor_stamina_tonic")
            .Description("Crush blueberries into honey and warm it gently.");
        Recipe("treated_leather", "Treated Leather")
            .Needs("rabbit_skin").Needs("wolf_blood")
            .At("tanning rack").Skill("tanning", "novice")
            .Makes("treated_leather")
            .Description("Cure a rabbit skin with wolf blood on the rack.");
        Recipe("travelers_leather_wrap", "Traveler's Leather Wrap")
            .Needs("treated_leather").Needs("cloth_strip")
            .At("workbench").Skill("tinkering", "novice")
            .Makes("/domains/hollow/obj/craft/travelers_leather_wrap")
            .Description("Stitch treated leather and a cloth strip into a belt with pouches.");
        Recipe("blood_ink", "Blood Ink")
            .Needs("goblin_blood").Needs("wildflower")
            .At("workbench").Skill("tinkering", "novice")
            .Makes("blood_ink")
            .Discovery("rare")
            .Description("Grind a wildflower into goblin blood until it runs as ink.");
        Recipe("hunters_poultice", "Hunter's Poultice")
            .Needs("healing_herb").Needs("goblin_blood").Needs("cloth_strip")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/hollow/obj/craft/hunters_poultice")
            .Description("Bind healing herb and goblin blood in a cloth strip.");
    }
}