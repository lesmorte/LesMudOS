// @inherit /std/room

/// <summary>A goblin defensive position at the western end of Smuggler Hall, held by two guards.</summary>
public class GuardPost : Room
{
    public override void Create()
    {
        ShortDesc = "Guard Post";
        LongDesc = "Crude goblin barricades create a defensive position.";
        Exits["east"] = "/domains/hollow/cellars/smuggler_hall";
        Safe = false;
        Light = LightDim; // dim: the goblins' fire pit is cold, and the glow of the Smuggler Hall spills in
        LongDesc += " The fire pit is cold, and only the glow of the hall spills in.";

        AddDetail("barricade,barricades,crude barricades,wall", "Broken cart planks, fence stakes and fallen rubble are piled into a wall, lashed together with rope and something that looks like coarse goblin hair.");
        AddDetail("fire,fire pit,ashes,brazier", "A cold fire pit sits behind the barricade, ringed with gnawed bones and a few blackened stones.");
        AddDetail("stakes,sharpened stakes,spikes", "Sharpened stakes point outward through gaps in the barricade, each one darkened at the tip.");

        AddSpawn("/domains/hollow/mob/goblin_guard", 1);
        AddSpawn("/domains/hollow/mob/goblin_guard_keyholder", 1);
    }
}
