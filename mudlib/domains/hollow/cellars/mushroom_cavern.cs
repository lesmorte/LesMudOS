// @inherit /std/room

/// <summary>A chamber lit by blue fungus, with three spore bats and a locked iron door leading north to the hidden cache.</summary>
public class MushroomCavern : Room
{
    public override void Create()
    {
        ShortDesc = "Mushroom Cavern";
        LongDesc = "Hundreds of bioluminescent mushrooms bathe the chamber in eerie blue light. Beyond the farthest cluster the wall is dark and damp, and a cold draught carries a green, sharp smell.";
        Exits["west"] = "/domains/hollow/cellars/flooded_tunnel";
        Exits["north"] = "/domains/hollow/cellars/hidden_cache";
        Exits["east"] = "/domains/hollow/cellars/goblin_barracks";
        Safe = false;
        Light = LightDim; // the fungus is the light, by day and by night
        LockExit("north", "rusty key", "A stout, rust-eaten door bars the way north. It is locked.");

        AddDetail("mushrooms,mushroom,caps,pale caps,bioluminescent mushrooms", "Pale caps the size of dinner plates grow in clusters, each one glowing a steady cold blue that brightens faintly when something moves nearby.");
        AddDetail("spores,spore dust,dust,drifting dust", "Fine dust drifts from the caps whenever a bat flaps past, glittering blue in the air before it settles on your clothes.");
        AddDetail("door,stout door,rust-eaten door,iron door,north door", "A heavy door of rust-eaten iron is set into the north wall. Its hinges are sealed with rust, and a keyhole gapes below the handle.");

        AddDetail("draught,smell,damp wall,wall,crack", "Where the glow gives out the wall runs with seep-water, and a cold draught comes out of a crack at about knee height, carrying a green, sharp smell. It is not the smell of the caps.");
        HideNode("herbs", "/domains/hollow/obj/cave_herbs", SearchModerate, "draught,smell,damp wall,wall,crack",
            "Where the glow gives out, you find a crack at knee height, and in it a small crowd of pale herbs. They smell like healing herb.", byFeel: true);

        AddSpawn("/domains/hollow/mob/spore_bat", 3);
        AddSpawn("/domains/hollow/obj/oak_root");
    }
}
