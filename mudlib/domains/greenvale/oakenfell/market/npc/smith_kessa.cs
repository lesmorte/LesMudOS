// @inherit /std/npc

public class MkSmithKessa : Npc
{
    public override void Create()
    {
        Name = "Kessa Dunmore";
        Ids.Add("kessa");
        Ids.Add("kessa dunmore");
        Ids.Add("weaponsmith");
        Ids.Add("smith");
        Ids.Add("blacksmith");
        ShortDesc = "Kessa Dunmore, a weaponsmith";
        LongDesc = "A short, strong woman with burn-scarred forearms, a leather apron and a pair of tongs always in her hand. Her black hair is bound under a scarf, and her portable anvil rings with each stroke.";
        Level = 3;
        MaxVitality = 40;

        AddChat("Not much on the rack yet. The new blades are still being unloaded from the cart. Come back soon.");
        AddChat("A good blade is balance first, edge second, and price last.");
        AddChat("Mind the sparks!");
        AddTopic("blade,blades,sword,swords,weapon,weapons", "The new stock is still being unloaded. I'll have a better blade for you soon, if you come back.");
        AddTopic("axe,spear,dagger,bow", "All coming in the same cart. A weaponsmith without stock is a sad creature.");
        AddTopic("smith,smiths,guild,crafters", "The Crafters' Court makes the best steel in Greenvale. I just do the fitting and finishing, and the selling.");
        AddEmote("Kessa Dunmore hammers a half-formed blade on her portable anvil.", 8);
        AddEmote("Kessa Dunmore holds a blade up to the light and squints along it.", 6);
    }
}
