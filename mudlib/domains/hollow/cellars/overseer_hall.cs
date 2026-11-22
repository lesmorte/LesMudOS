// @inherit /std/room

/// <summary>The trophy hall before the Overseer's chamber, guarded by two goblin elite guards. A closed door leads east to the chamber.</summary>
public class OverseerHall : Room
{
    public override void Create()
    {
        ShortDesc = "Overseer Hall";
        LongDesc = "Animal skulls and trophies decorate this intimidating chamber.";
        Exits["west"] = "/domains/hollow/cellars/goblin_barracks";
        Exits["east"] = "/domains/hollow/cellars/overseer_chamber";
        Safe = false;
        // The door to the Overseer's chamber: closed and not locked, shut again at every reset. Opening it is loud (docs/BUILDING.md, sneaking):
        // whoever comes through it has to deal with the Elite Guards first. The chamber declares the other half; the two share one state.
        AddDoor("east", "door", description: "A heavy door of black-stained planks bound in iron, a crow scratched across it in ash. It has no lock, only an iron ring to pull.");

        AddDetail("skulls,animal skulls,skull", "Animal skulls, mostly rats and wolves, are nailed to the walls in neat rows, with teeth and tufts of fur still clinging to them.");
        AddDetail("ropes,hide ropes,charms,knuckle bones", "Braided hide ropes hang from the ceiling, strung with knuckle bones and tiny carved charms that knock together in the draught.");
        AddDetail("trophies,trophy,hangings,hide", "Hides have been stretched and pegged to the far wall, each one marked with a crude crow scratched in ash.");

        AddSpawn("/domains/hollow/mob/goblin_elite_guard", 2);
    }
}
