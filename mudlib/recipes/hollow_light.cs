// @inherit /std/recipe_set

/// <summary>The Rag Torch (docs/specs/light.md, section 4): one Simple recipe, no station, two torches from a cloth strip and a lump of tallow.</summary>
public class CrLightRecipes : RecipeSet
{
    public override void Create()
    {
        Recipe("rag_torch", "Rag Torch")
            .Needs("cloth_strip").Needs("tallow")
            .Skill("tinkering", "simple")
            .Makes("/domains/hollow/obj/rag_torch", 2)
            .Description("Roll a strip of cloth tight, work tallow into it, and bind it round the handle of a broken tool.");
    }
}
