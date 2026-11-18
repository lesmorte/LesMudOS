// @inherit /std/room

public class FbBowlTomb : Room
{
    public override void Create()
    {
        ShortDesc = "The Tomb of the Seven Bowls";
        LongDesc = "A long, narrow chamber with a bier cut from the living rock down its middle, and about it, set at equal intervals in a ring, seven shallow bowls of green-grey stone. The bowls hold a brown crust, the dust of what may once have been oil, or grain, or something else. The bier is longer than a man needs, and its surface is smooth as water. The doorway behind you is plain but for a few strokes on its jamb. The carving here is deeper and tighter than in the passage, as though more care had been taken with those who lay nearest the ground.";
        Exits["west"] = "/domains/greenvale/corridor/barrow/long_gallery";

        AddDetail("bier,slab,rock,stone bed", "The bier is the rock itself, left standing when the chamber was cut around it, and its top has been polished level. A shallow trough runs down each side. What lay on it lies there still, or has gone to dust, and it is not for you to say which.");
        AddDetail("bowls,bowl,crust,offerings,ring", "Each bowl is a hand deep and as wide as a helm, hollowed from a single block and set into the floor so that its rim is level. One stands at the head of the bier, and the others are set round it at the same distance, the seventh at the foot. The brown crust in each has cracked into plates like a dry riverbed.");
        AddDetail("pins,pin,row,metal,long pins", "Along the edge of the bier lie seven long pins of dark metal, green with age, laid side by side with their points all one way. They are as long as a forearm. The metal is not bronze and not iron, and it has not rusted through.");
        AddDetail("floor,dust,trough,troughs", "The troughs along the bier lead to a hole in the floor, no bigger than a thumb, and the dust is thick about it. Nothing here was disturbed by the party that came before you. They looked in at the door and went elsewhere.");
        AddDetail("door,doorway,west,threshold,jamb", "The doorway is the same tall, narrow shape as all the others. On the jamb, at the height of a man's shoulder, there are seven short strokes cut in a row. They are the only marks here that are not part of the pattern.");
        AddReadable("walls,wall,carving,carvings,scene,figures,figure,panel,pattern", "The carving is one scene, repeated on both walls. A single figure lies on a bier, and seven others stand about it, each holding a bowl in the hand that is not raised. A branch, bound with a cord, is held at the head. At the feet stands a last figure whose arm is not raised, as it was not in the court in the wood. The faces are as blank as ever. Nothing says who lies on the bier.", "A single scene is carved on the walls. You could {cmd}read{/} it.");
        AddSpawn("/domains/greenvale/corridor/mob/ancient_skeleton", 2);
        AddSpawn("/domains/greenvale/corridor/barrow/obj/grave_token");
    }
}
