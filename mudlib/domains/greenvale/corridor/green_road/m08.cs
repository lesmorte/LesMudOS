// @inherit /std/room

public class GrTheLeaningStones : Room
{
    public override void Create()
    {
        ShortDesc = "The Green Road, the Leaning Stones";
        LongDesc = "A ring of tall grey stones stands on a slope beside the road, every one of them leaning outward as though in the grip of a long, slow wind. They are furred with moss on three sides and bare on the fourth, and the bare side faces the road. The trees keep their distance from the stones and the ground between them is thick with ferns. Birds that fly across the gap alter their course for a moment, as if to avoid something.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m07";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m09";
        Safe = false;

        AddDetail("stones,stone,ring,circle,standing stones,leaning stones", "Nine grey stones, taller than a man and each leaning a little outward. The fourth side of each is bare of moss, as though rubbed by a hand, though the stones are too high to rub.");
        AddDetail("moss,lichen,bare side,bare", "Soft green moss thick on three faces and clean grey stone on the fourth. The line between them is as sharp as a cut.");
        AddDetail("ferns,fern,ground,slope", "The ferns grow tall inside the ring and nowhere else, and they are all bent, very slightly, toward the centre.");
        AddDetail("birds,bird,crows,crow,sparrows", "A flock of sparrows rises from the road, bends away round the ring in a wide arc and rejoins the line beyond it. Nobody has ever seen a bird perch on the stones.");
        AddDetail("trees,edge,wood,gap", "The trees come right to the road on both sides but not here. There is an empty space around the stones twenty paces wide, as though the wood would rather not.");
        AddDetail("carving,marks,scratches,scratch,symbols", "Low on one stone there are marks, a few scratches that might be weather and might be an alphabet. They are old and very faint, and you could not copy them if you tried.");
    }
}
