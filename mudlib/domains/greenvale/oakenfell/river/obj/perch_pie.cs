// @inherit /std/consumable

public class RvPerchPie : Consumable
{
    public override void Create()
    {
        Name = "Perch Pie";
        Ids.Add("pie");
        Ids.Add("perch pie");
        Ids.Add("fish pie");
        ShortDesc = "a perch pie";
        LongDesc = "A hand-sized pie of golden pastry, packed with flaked river perch, leeks and cream, still warm from the oven. You could {cmd}eat{/} it.";
        HealAmount = 12;
        UseVerb = "eat";
        UseMessage = "You bite through the crisp pastry into hot, creamy perch and leeks. It is gone far too quickly.";
        Value = 5;
    }
}
