// @inherit /std/craft_data

/// <summary>The materials of Vincent's Hollow (the six skins and bloods come from /crafting/beasts.cs) and what its nodes yield.</summary>
public class HcLaunchMaterials : CraftData
{
    public override void Create()
    {
        Material("blackberries", "/domains/hollow/obj/craft/blackberries", "berry, fruit");
        Material("blueberries", "/domains/hollow/obj/craft/blueberries", "berry, fruit");
        Material("healing_herb", "/domains/hollow/obj/craft/healing_herb", "herb");
        Material("wildflower", "/domains/hollow/obj/craft/wildflower", "flower");
        Material("thistle", "/domains/hollow/obj/craft/thistle", "plant");
        Material("honey", "/domains/hollow/obj/craft/honey", "sweetener");
        Material("flour", "/domains/hollow/obj/craft/flour", "grain");
        Material("clean_water", "/domains/hollow/obj/craft/clean_water", "liquid");
        Material("cloth_strip", "/domains/hollow/obj/craft/cloth_strip", "cloth");
        Material("empty_bottle", "/domains/hollow/obj/craft/empty_bottle", "container");
        // Crafted things that are ingredients of other recipes.
        Material("berry_juice", "/domains/hollow/obj/craft/berry_juice", "juice, drink");
        Material("treated_leather", "/domains/hollow/obj/craft/treated_leather", "leather");
        Material("blood_ink", "/domains/hollow/obj/craft/blood_ink", "ink");

        Yield("berry bush", "blackberries", 1, 2);
        Yield("berry bush", "blueberries", 1, 2);
        Yield("herb patch", "healing_herb");
        Yield("wildflowers", "wildflower", 1, 2);
        Yield("thistle patch", "thistle");
    }
}
