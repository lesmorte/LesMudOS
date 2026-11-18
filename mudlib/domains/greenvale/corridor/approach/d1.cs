// @inherit /std/room

public class OaD1 : Room
{
    public override void Create()
    {
        ShortDesc = "The Aqueduct Cistern";
        LongDesc = "A flight of broken steps leads down into a vast chamber, a stone cistern with a tall pointed vault held on a double row of square pillars, where the aqueduct's water once gathered. The floor is dry, a litter of leaves and bones, and the walls sweat with a cold seep that tastes of iron. Light falls in grey shafts from holes in the vault, an echoing drip counts the seconds, and in the farthest corner, where the floor tilts down into a sump, the stones are of a different colour.";
        Exits["east"] = "/domains/greenvale/corridor/approach/w3";
        Exits["north"] = "/domains/greenvale/corridor/approach/d2";
        ResetSeconds = 120;

        AddDetail("cistern,chamber,vault,roof,pillars,pillar", "The pillars are square and without ornament, of a dark green-grey stone, and the vault they carry rises to a point along its whole length. It is the work of masons who knew they were building for ever.");
        AddDetail("steps,broken steps,stair,staircase", "The steps are broad, shallow and crumbled at the edges, worn hollow by footsteps older than any record. They have been used recently, by someone with hobnails.");
        AddDetail("floor,leaves,bones,litter,droppings", "Leaves have blown in, and bones too: deer, boar, something that might once have been a dog. Fresh droppings suggest that the cistern is not empty.");
        AddDetail("holes,shafts,light,roof holes,sky", "Round holes in the vault, a stride across, let down shafts of grey daylight where, once, water was drawn through. Ferns grow down toward them on thin roots.");
        AddDetail("seep,walls,water,wall,drip,drips", "A thin film of water slides down the wall and drips into a stone channel, as it has done for longer than anyone can say. It leaves an orange stain.");
        AddDetail("sump,corner,farthest corner,low corner,lower stones,different stones,blocked arch,arch,masonry,draught,gap,culvert", "In the low corner the floor drops to a shallow sump, and beyond it a pointed arch has been filled solid with blocks of a paler, finer-grained stone, fitted so closely that no mortar shows. At the very top of the filling one block has slipped, leaving a black gap a person could wriggle through. A thin cold air breathes out of it, dry, as if from a place that is a long way down, and a careful person might well go down into it.");
        HiddenExit("down", "/domains/greenvale/corridor/approach/s1", "sump,corner,farthest corner,low corner,lower stones,different stones,blocked arch,arch,masonry,gap,culvert", 15,
            "You wade the shallow sump and climb the pale, close-fitted filling to the block that has slipped, and the black gap above it is wide enough to wriggle through. A way leads {cmd}down{/}, into dry cold air.");

        AddSpawn("/domains/greenvale/corridor/mob/rogue_goblin_veteran");
    }
}
