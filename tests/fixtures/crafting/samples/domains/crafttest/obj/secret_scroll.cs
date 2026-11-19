// @inherit /std/recipe_book
public class CtSecretScroll : RecipeBook
{
    public override void Create()
    {
        Name = "Secret Scroll";
        Ids.Add("scroll");
        ShortDesc = "a secret scroll";
        ReadText = "Blueberries and honey, it says, and nothing else.";
        Teaches.Add("stamina_tonic");
        ConsumedOnRead = true;
    }
}
