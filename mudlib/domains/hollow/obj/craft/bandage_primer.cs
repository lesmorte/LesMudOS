// @inherit /std/recipe_book
public class CrBandagePrimer : RecipeBook
{
    public override void Create()
    {
        Name = "Tobin's Bandage Primer";
        Ids.Add("primer");
        Ids.Add("book");
        Ids.Add("bandage primer");
        Ids.Add("tobin's primer");
        ShortDesc = "a thin primer on bandages";
        LongDesc = "A few stitched sheets in a card cover, written in a blocky carpenter's hand and smudged with glue. You could {cmd}read{/} it.";
        ReadText = "TOBIN'S PRIMER ON BANDAGES\n  Take a clean strip of linen. Fold the raw edge in on itself, so it will not fray.\n  Roll it tight from one end, and tie it off with a half-hitch. Never mind the knots; it only has to hold.\n  Needs no bench and no fire: you can do it by the roadside.\n  (Written for Benson, who kept asking.)";
        Teaches.Add("simple_bandage");
        Value = 3;
    }
}
