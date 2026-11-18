// @inherit /std/craft_data
public class CtMaterials : CraftData
{
    public override void Create()
    {
        Material("healing_herb", "/domains/crafttest/obj/healing_herb", "herb");
        Material("wildflower", "/domains/crafttest/obj/wildflower", "flower");
        Material("blackberries", "/domains/crafttest/obj/blackberries", "berry, fruit");
        Material("blueberries", "/domains/crafttest/obj/blueberries", "berry, fruit");
        Material("clean_water", "/domains/crafttest/obj/clean_water", "liquid");
        Material("cloth_strip", "/domains/crafttest/obj/cloth_strip", "cloth");
        Material("honey", "/domains/crafttest/obj/honey", "sweetener");
        Material("berry_juice", "/domains/crafttest/obj/berry_juice", "juice, drink");

        Yield("berry bush", "blackberries", 1, 2);
        Yield("berry bush", "blueberries", 1, 2);
        Yield("herb patch", "healing_herb");
    }
}
