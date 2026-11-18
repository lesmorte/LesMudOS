// @inherit /std/room

public class RvQuayNorth : Room
{
    public override void Create()
    {
        ShortDesc = "The Mere Quay, North Landing";
        LongDesc = "The north end of the quay is quieter, a worn stone landing hung with drying nets and smelling strongly of fish. A fish market opens under a tarred awning to the west, and a wide shipyard gate stands to the east, ringing with the noise of mallets and adzes. A little farther north the quay narrows to a steep stone water-gate, where the Mere passes out through the city wall. Gulls squabble on the roofs, and a heap of lobster pots has gone green with weed.";
        Exits["south"] = "/domains/greenvale/oakenfell/river/quay";
        Exits["west"] = "/domains/greenvale/oakenfell/river/fish_market";
        Exits["east"] = "/domains/greenvale/oakenfell/river/shipwright";
        Exits["north"] = "/domains/greenvale/oakenfell/river/rivergate";

        AddDetail("landing,quay,stone,stones,paving,floor", "A stone landing worn into hollows by feet and spilled water. The edge is lined with iron rings, and the mooring-posts are carved with a hundred small marks that the boatmen understand.");
        AddDetail("nets,net,drying nets,rope,ropes,lines", "Brown nets hang from poles along the landing, drying, mended with patches of newer twine. They smell of tar and salt and a whole season of river.");
        AddDetail("pots,lobster pots,heap,weed,traps", "A heap of wicker traps stacked beside the wall, green with weed and rattling as the wind passes through them.");
        AddDetail("awning,fish market,market,west", "A broad tarred awning shelters the fish market to the west, with slabs, tubs of ice and a hand-painted board over the opening. It is easy to find by the smell.");
        AddDetail("yard,shipyard,gate,east,shipwright", "A wide plank gate stands open on the shipwright's yard to the east. Behind it you can see timber stacks, a steaming box and a great curved hull on the slip.");
        AddDetail("water-gate,water gate,arch,wall,north,rivergate", "To the north the quay narrows to a steep stone arch, with a portcullis raised above it, where the Mere passes out through the city wall. A small landing runs beside it for the watch.");
        AddDetail("gulls,gull,birds,roofs", "Gulls stand on every roof and post, quarrelling over scraps with their wings half-spread.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/sal_cobb");
    }
}
