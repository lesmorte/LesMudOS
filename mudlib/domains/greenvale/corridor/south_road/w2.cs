// @inherit /std/room

public class SrW2 : Room
{
    public override void Create()
    {
        ShortDesc = "Old Miller's Farm, the Orchard";
        LongDesc = "An old orchard of apple and pear trees spreads in crooked rows behind the farm, the trunks grey and mossed, the boughs bent under green fruit. Bees hum in the long grass, and wasps work the windfalls with grim purpose. A thick bramble hedge borders the north side, heavy with berries in the hottest corners, and a path of bare earth runs through the middle of it all toward the hill. The sails of the windmill show through the branches, slow and white.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/w1";
        Exits["north"] = "/domains/greenvale/corridor/south_road/w3";
        ResetSeconds = 120;

        AddDetail("orchard,trees,apple trees,pear trees,boughs", "Old trees, planted in the miller's grandfather's day, propped with forked poles where the limbs hang low. The fruit is hard and green and smells of promise.");
        AddDetail("bees,hive,hives,skeps", "Straw skeps stand on a shelf under a thatched shed, and the bees come and go in a drowsy stream. They have no interest in you if you are polite.");
        AddDetail("windfalls,wasps,fruit,apples,pears", "Windfalls lie in the grass already browning, loud with wasps. The bees ignore them. The wasps ignore everything.");
        AddDetail("bramble,brambles,hedge,berries,blackberries", "A tall bramble hedge, black with ripening blackberries in the sunniest corners. Birds have already eaten the lowest ones; the best still hang high.");
        AddDetail("path,hill,earth", "A worn path crosses the orchard, bare earth between grass, and climbs away beyond the north hedge. A cartwheel's width of it has been laid with flat stones for wet weather.");

        AddSpawn("/domains/greenvale/corridor/mob/field_rabbit", 2);
    }
}
