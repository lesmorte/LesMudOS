// @inherit /std/find

/// <summary>A pale oak root grown through the wall of the Mushroom Cavern: a keepsake. Every character may take one Staff of the Hollow Oak, once.</summary>
public class OakRoot : Find
{
    public override void Create()
    {
        Name = "Oak Root";
        Ids.Add("root");
        Ids.Add("oak root");
        Ids.Add("pale root");
        Ids.Add("oak");
        Ids.Add("wand");
        Ids.Add("heartwood");
        Ids.Add("rod");
        Ids.Add("staff");
        ShortDesc = "a pale root grown through the cavern wall";
        LongDesc = "Between two glowing clusters of mushrooms a root as thick as your arm has forced its way through the stone, pale as bone and wholly out of place: it is oak, and there is no oak for a mile. Here and there it has thrown up a straight rod of heartwood, as long as a staff and clean of bark, as if it grew them for whoever came asking. One would come away in the hand.";
        TakenShort = "a pale root grown through the cavern wall, with a clean-cut stump where you took a rod";
        TakenHere = "A pale root grows through the cavern wall, a clean-cut stump where you took a rod.";
        EmptyHere = "A pale root grows through the cavern wall.";
        TakenLong = "The pale root runs through the wall as before. One of its rods is gone, and a neat pale stump is all that shows where you took it. The others are too green to cut for you.";
        ItemLine = "You close a hand on one of the straight rods and it parts from the root with a soft crack. You hold %item%.";
        TakenLine = "You have already taken a rod from the root; the rest are too green to cut for you.";
        ItemPath = "/domains/hollow/obj/hollow_oak_staff";
    }
}
