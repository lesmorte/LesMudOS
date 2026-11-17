// @inherit /std/room

public class IronoakArms : Room
{
    public override void Create()
    {
        ShortDesc = "Ironoak Arms";
        LongDesc = "The smell of hot metal and burning coal fills this small but impressive smithy. Weapon racks cover the walls while a roaring forge illuminates newly finished blades.";
        Exits["west"] = "/domains/hollow/village/square";
        Safe = true;

        AddDetail("forge,fire,coal", "The forge roars like a caged beast, its coals glowing white at the heart. Torren feeds it with a bellows the size of a small boat.");
        AddDetail("racks,rack,weapon racks,weapons,blades", "Swords, spears, axes and daggers stand in tidy ranks, each one tagged in chalk with its price. Torren will tell you what he has if you ask to browse.");
        AddDetail("anvil", "A scarred anvil, black with the work of decades. A half-finished blade lies across it.");

        AddSpawn("/domains/hollow/npc/torren");
    }
}
