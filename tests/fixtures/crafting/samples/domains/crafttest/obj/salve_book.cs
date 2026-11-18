// @inherit /std/recipe_book
public class CtSalveBook : RecipeBook
{
    public override void Create()
    {
        Name = "Salve Primer";
        Ids.Add("primer");
        Ids.Add("salve primer");
        ShortDesc = "a salve primer";
        LongDesc = "A thin primer on salves.";
        ReadText = "Crush a healing herb with a wildflower, warm it gently and work it into a paste.";
        Teaches.Add("minor_healing_salve");
        Value = 5;
    }
}
