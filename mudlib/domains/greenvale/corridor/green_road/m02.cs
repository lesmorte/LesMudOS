// @inherit /std/room

public class GrTheCanopy : Room
{
    public override void Create()
    {
        ShortDesc = "The Green Road, the Canopy";
        LongDesc = "The trees meet overhead and the road becomes a long green tunnel, damp and hushed. Drips fall from the leaves though it has not rained, and your footsteps seem to die a few paces away. The ruts hold dark water that never quite dries. Every so often a shaft of light finds a gap and lights a cloud of midges, which is the nearest thing to company the road offers.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m01";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m03";
        Safe = false;
        DangerBand(6, 7);

        AddDetail("tunnel,canopy,roof,leaves,branches", "Beech and oak, all reaching for the same scrap of sky. The roof is so close and so thick that you could take it for a vaulted hall.");
        AddDetail("water,ruts,puddles,puddle,rut", "The standing water is brown and cold, with a skin of old leaves. Hoof prints are pressed into the mud at its edge, and one of them is a good deal larger than a horse's.");
        AddDetail("midges,shaft,light,shafts", "A cloud of midges turns in a slant of light, bright as sparks. Outside the shaft they vanish.");
        AddDetail("drips,drip,moisture,damp", "Water gathers on the leaves and falls in single heavy drops. The forest never quite dries out.");
        AddDetail("mud,hoof prints,prints,hoof,boar", "Among the horse tracks a hoof print is cloven and as wide as your hand. Something heavy comes to drink here when the road is empty.");
        AddDetail("moss,stones,roots,root", "Roots as thick as a man's leg cross the road and are worn smooth by wheels. Moss has covered every stone that is not walked on.");

        AddEncounter("/domains/greenvale/corridor/mob/goblin_raider", 6);
    }
}
