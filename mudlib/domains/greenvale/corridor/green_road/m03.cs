// @inherit /std/room

public class GrMerchantCamp : Room
{
    public override void Create()
    {
        ShortDesc = "The Merchant Camp";
        LongDesc = "A caravan has halted for the night in a wide place where the road bows out and the trees have been cut back, and its wagons stand in a rough ring with the oxen tethered inside. A cookfire burns low in the middle, throwing a red light that stops at the first tree trunks and leaves the wood beyond it very dark. Lanterns hang from the wagon hoops, and the camp smells of woodsmoke, stew, wet canvas and dung. A narrow path leads away west between the trunks. You could {cmd}talk{/} to anyone who looks up from the fire.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m02";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m04";
        Exits["west"] = "/domains/greenvale/corridor/green_road/w1";
        Safe = true;
        DangerBand(6, 7);

        AddDetail("wagons,wagon,ring,hoops,canvas", "Heavy four-wheeled wagons chained wheel to wheel, with canvas tilts pegged tight against the damp. Each carries a painted scale on the tailboard, the mark of a Consortium house, but the paint is flaking from hard travel.");
        AddDetail("oxen,ox,beasts,animals,tethers", "Sixteen patient oxen stand in a huddle with their breath smoking, and one of them looks at you with the weary interest of an animal that has done this journey many times.");
        AddDetail("fire,cookfire,campfire,embers,stew,pot", "A black pot hangs on a tripod over the coals and the stew in it has been added to, by the smell, every night since Eldergreen. A ring of flat stones keeps the sparks off the grass.");
        AddDetail("lanterns,lantern,lamps,lights", "Horn lanterns swing from the wagon hoops. Whoever hung them did it with care: not one is near the canvas.");
        AddDetail("path,west path,pedlars,trail", "A narrow beaten path leaves the camp between two beeches. Pedlars use it to avoid paying camp-fee, and goblins, it is said, use it for less honest purposes.");
        AddDetail("trees,wood,forest,dark,darkness", "Beyond the firelight the trunks stand black and close. The drovers have cut every bush back twenty paces and a watchman stands at each corner of the ring all night.");
        AddDetail("cut,clearing,stumps,stump", "The clearing is old. Stumps ringed by coals and cart-ruts show that caravans have used the same place for years, and the camp-fee is the one tax on the Green Road that everyone pays.");

        AddSpawn("/domains/greenvale/corridor/green_road/npc/lambert");
        AddSpawn("/domains/greenvale/corridor/green_road/npc/hereward");
        AddSpawn("/domains/greenvale/corridor/green_road/npc/drogo");
        AddSpawn("/domains/greenvale/corridor/green_road/npc/wystan");
    }
}
