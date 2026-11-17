// @inherit /std/room

/// <summary>The trophy hall before the Overseer's chamber, guarded by two goblin elite guards.</summary>
public class OverseerHall : Room
{
    public override void Create()
    {
        ShortDesc = "Overseer Hall";
        LongDesc = "Animal skulls and trophies decorate this intimidating chamber.";
        Exits["west"] = "/domains/hollow/cellars/goblin_barracks";
        Exits["east"] = "/domains/hollow/cellars/overseer_chamber";
        Safe = false;

        AddDetail("skulls,animal skulls,skull", "Animal skulls, mostly rats and wolves, are nailed to the walls in neat rows, with teeth and tufts of fur still clinging to them.");
        AddDetail("ropes,hide ropes,charms,knuckle bones", "Braided hide ropes hang from the ceiling, strung with knuckle bones and tiny carved charms that knock together in the draught.");
        AddDetail("trophies,trophy,hangings,hide", "Hides have been stretched and pegged to the far wall, each one marked with a crude crow scratched in ash.");

        AddSpawn("/domains/hollow/mob/goblin_elite_guard", 2);
    }
}
