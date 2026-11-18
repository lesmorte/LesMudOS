// @inherit /std/recipe_set
public class CtSampleRecipes : RecipeSet
{
    public override void Create()
    {
        // No station, one ingredient.
        Recipe("simple_bandage", "Simple Bandage")
            .Needs("cloth_strip")
            .Skill("herbalism", "simple")
            .Makes("/domains/crafttest/obj/simple_bandage")
            .Description("A strip of clean cloth, folded and tied.");
        // A station, two exact ingredients.
        Recipe("minor_healing_salve", "Minor Healing Salve")
            .Needs("healing_herb").Needs("wildflower")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/crafttest/obj/minor_healing_salve");
        // A category ingredient; the output is itself a material.
        Recipe("berry_juice", "Berry Juice")
            .NeedsAny("berry", 2).Needs("clean_water")
            .At("cooking fire").Skill("cooking", "simple")
            .Makes("berry_juice");
        // Three ingredients, one of them Goblin Blood; found by exploration.
        Recipe("hunters_poultice", "Hunter's Poultice")
            .Needs("healing_herb").Needs("goblin_blood").Needs("cloth_strip")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/crafttest/obj/hunters_poultice")
            .Discovery("rare");
        // A secret: no trainer or book can teach it.
        Recipe("stamina_tonic", "Minor Stamina Tonic")
            .Needs("blueberries").Needs("honey")
            .At("herbalist table").Skill("herbalism", "novice")
            .Makes("/domains/crafttest/obj/stamina_tonic")
            .Discovery("secret");
        // A wearable with a recorded but inert property, from a skin.
        Recipe("leather_wrap", "Leather Wrap")
            .Needs("wolf_skin").Needs("cloth_strip")
            .At("workbench").Skill("tinkering", "novice")
            .Makes("/domains/crafttest/obj/leather_wrap");
    }
}
