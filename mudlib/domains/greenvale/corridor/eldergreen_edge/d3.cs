// @inherit /std/room

public class EeMarkedOak : Room
{
    public override void Create()
    {
        ShortDesc = "The Marked Oak";
        LongDesc = "A colossal oak stands alone in a ring of bare, swept earth, so wide that a dozen people might not encompass it, and so old that its lower limbs have sunk into the ground and risen again as trunks. Its bark is carved all round at head height with a band of deep marks, and from every low bough hang wreaths of plaited withy, small bones and bright feathers. Nothing grows beneath it. The wood about the ring is silent and the animals keep a distance from it, as if by agreement, though the goblin cords end twenty paces away.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/d2";
        Exits["west"] = "/domains/greenvale/corridor/eldergreen_edge/w5";
        ResetSeconds = 120;

        AddDetail("oak,tree,bark,trunk,limbs,boughs", "The oak is vast beyond reckoning, with a trunk like a tower and a canopy that covers the whole clearing. It looks a thousand years old, and it looks watchful.");
        AddDetail("wreaths,withy,plait,plaits,bones,feathers,hangings,charms", "The wreaths are plaited willow bent into rings, and each ring holds small bones laid in a pattern, a fox's skull, a bird's wing, three teeth. Some are green-fresh, and some have rotted to a thread.");
        AddDetail("ground,earth,ring,swept earth,bare earth", "The earth in the ring is swept bare and packed hard, without leaf or twig, as if brushed each dawn. There are no footprints in it but yours.");
        AddDetail("stones,stone,cairn,stone circle", "Small grey stones are set at the edge of the swept ring at regular intervals, each with a single notch cut in its top. Roots have lifted a few, and someone has patiently set them back.");
        AddDetail("arrow,shaft,fletching,cloth,strip,green cloth", "A single arrow stands upright in the turf a pace outside the swept ground. It was pushed in by hand, not shot. The fletching is grey goose, cut the way the hunters at the camp cut theirs, and a strip of green cloth is tied below the head. The weather has had it for a season. Nothing else of whoever left it is here.");
        AddDetail("animals,silence,cords,cord,goblin cords", "A knotted goblin cord lies in the leaves twenty paces back from the ring, dropped, not tied. Whatever the goblins were counting, they stopped here.");
        AddReadable("markings,marks,carvings,band", "You cannot read the markings; they are not letters of any alphabet you know. Some show rings, some show a branching like thorns or antlers, and one, repeated nine times round the trunk, is a simple spiral. You have the feeling they were made to be understood by something other than people.", "A band of marks has been cut around the trunk, three hands deep: rings within rings, notched lines like thorns, and a repeated shape that might be a crown or a tooth. They are fresh-edged in places and silvered with age in others, as though added to over centuries. You could {cmd}read{/} them.");

        AddSpawn("/domains/greenvale/corridor/mob/wild_boar", 1);
        AddSpawn("/domains/greenvale/corridor/mob/goblin_scout", 1);
    }
}
