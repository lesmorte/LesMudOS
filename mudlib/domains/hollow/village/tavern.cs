// @inherit /std/room

public class HollowmansRest : Room
{
    public override void Create()
    {
        ShortDesc = "The Hollowman's Rest";
        LongDesc = "Warm firelight illuminates the village tavern. Travelers, adventurers, and locals enjoy food and drink while musicians play near the fireplace. Hanging above the hearth is a weathered portrait of Vincent Holloway himself. Though faded by age, his confident smile remains intact. A sense of comfort fills the room, though whispers occasionally mention strange noises in the cellar below.";
        Exits["north"] = "/domains/hollow/village/square";
        Exits["up"] = "/domains/hollow/village/guest_rooms";
        Exits["down"] = "/domains/hollow/village/cellar";
        Safe = true;

        AddDetail("portrait,vincent,painting,holloway,picture", "A weathered portrait of Vincent Holloway in a dark wooden frame. The paint has faded and cracked, but his confident smile is still there, as if he knows a secret about the room.");
        AddDetail("fireplace,hearth,fire,fire place", "A wide stone fireplace with a crackling log fire. The mantel is blackened with soot and crowded with candle stubs.");
        AddDetail("musicians,musician,band,minstrels", "A fiddler and a piper sit on a low stool near the hearth, playing a lively tune that makes the tables tap their feet.");
        AddDetail("stairs,staircase,steps", "A narrow wooden staircase climbs to the Guest Rooms above, its treads worn hollow by generations of boots.");
        AddDetail("cellar door,trapdoor,cellar,trap door", "A heavy oak trapdoor set into the floor, with an iron ring for a handle. Cold air seeps up around its edges, smelling of damp earth.");

        AddSpawn("/domains/hollow/npc/harland");
        AddSpawn("/domains/hollow/npc/mara");
        AddSpawn("/domains/hollow/npc/prospector");
    }
}
