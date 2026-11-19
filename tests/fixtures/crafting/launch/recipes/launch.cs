// @inherit /std/recipe_set

/// <summary>The owner's ten launch recipes (docs/specs/crafting-system.md), all Simple or Novice, all near Vincent's Hollow.</summary>
public class HcLaunchRecipes : RecipeSet
{
    public override void Create()
    {
        Recipe("berry_juice", "Berry Juice")
            .Needs("blackberries").Needs("clean_water")
            .At("cooking fire").Skill("cooking", "simple")
            .Makes("berry_juice");
        Recipe("sweet_berry_juice", "Sweet Berry Juice")
            .Needs("berry_juice").Needs("honey")
            .At("cooking fire").Skill("cooking", "novice")
            .Makes("/domains/hollow/obj/craft/sweet_berry_juice");
        Recipe("simple_bandage", "Simple Bandage")
            .Needs("cloth_strip")
            .Skill("herbalism", "simple")
            .Makes("/domains/hollow/obj/craft/simple_bandage");
        Recipe("herbal_bandage", "Herbal Bandage")
            .Needs("cloth_strip").Needs("healing_herb")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/hollow/obj/craft/herbal_bandage");
        Recipe("minor_healing_salve", "Minor Healing Salve")
            .Needs("healing_herb").Needs("wildflower")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/hollow/obj/craft/minor_healing_salve");
        Recipe("minor_stamina_tonic", "Minor Stamina Tonic")
            .Needs("blueberries").Needs("honey")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/hollow/obj/craft/minor_stamina_tonic");
        Recipe("treated_leather", "Treated Leather")
            .Needs("rabbit_skin").Needs("wolf_blood")
            .At("tanning rack").Skill("tanning", "novice")
            .Makes("treated_leather");
        Recipe("travelers_leather_wrap", "Traveler's Leather Wrap")
            .Needs("treated_leather").Needs("cloth_strip")
            .At("workbench").Skill("tinkering", "novice")
            .Makes("/domains/hollow/obj/craft/travelers_leather_wrap");
        Recipe("blood_ink", "Blood Ink")
            .Needs("goblin_blood").Needs("wildflower")
            .At("workbench").Skill("tinkering", "novice")
            .Makes("blood_ink");
        Recipe("hunters_poultice", "Hunter's Poultice")
            .Needs("healing_herb").Needs("goblin_blood").Needs("cloth_strip")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/hollow/obj/craft/hunters_poultice");
    }
}
