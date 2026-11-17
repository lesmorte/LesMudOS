// @inherit /std/consumable

public class HealingPotion : Consumable
{
    public override void Create()
    {
        Name = "Minor Healing Potion";
        Ids.Add("potion");
        Ids.Add("healing potion");
        ShortDesc = "a minor healing potion";
        LongDesc = "A stoppered glass vial of ruby-red liquid. It smells of cherries and iron. You could drink it.";
        HealAmount = 20;
        UseVerb = "drink";
        UseMessage = "You pull the stopper and drink the ruby-red potion. It tastes of cherries.";
        Value = 15;
    }
}
