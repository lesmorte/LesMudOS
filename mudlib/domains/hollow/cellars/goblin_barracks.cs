// @inherit /std/room

/// <summary>The goblins' sleeping quarters, crowded with three goblin warriors, between the caverns and Overseer Hall.</summary>
public class GoblinBarracks : Room
{
    public override void Create()
    {
        ShortDesc = "Goblin Barracks";
        LongDesc = "Crude bedding and stolen goods clutter this crowded chamber.";
        Exits["west"] = "/domains/hollow/cellars/mushroom_cavern";
        Exits["east"] = "/domains/hollow/cellars/overseer_hall";
        Safe = false;
        Light = LightDark;
        DarkDesc = "The dark is thick with old straw, smoke and sour meat. The air feels crowded.";

        AddDetail("bedding,straw,beds,bedrolls,nests", "Heaps of filthy straw and stolen blankets are piled in the corners, and each nest has a notched knife tucked beside it.");
        AddDetail("goods,stolen goods,loot,plunder", "Stolen goods of every kind lie heaped together: a cracked lantern, a bolt of mildewed cloth, and a child's wooden horse with one wheel missing.");
        AddDetail("scraps,bones,gnawed bones,flies", "Half-gnawed bones and sour scraps litter the floor, and a cloud of flies rises whenever anything moves.");

        AddSpawn("/domains/hollow/mob/goblin_warrior", 3);
    }
}
