// @inherit /std/room

public class FbInscribedTomb : Room
{
    public override void Create()
    {
        ShortDesc = "The Inscribed Tomb";
        LongDesc = "The west wall of this narrow tomb is given over to one great panel of dark stone, set flush in the wall and cut in rows of interlaced strokes from the height of the knee to the height of a raised hand. In the middle of the panel is a rectangle where the stone is cleaner and paler than the rest, as if a sheet had lain across it for a while. Before it the dust on the floor is stained with a fine black drift, such as a stick of charcoal leaves. A bier stands against the east wall, bare and level and cold.";
        Exits["east"] = "/domains/greenvale/corridor/barrow/long_gallery";

        AddDetail("rectangle,clean,clean stone,pale,sheet", "The rectangle is about as large as a table-top and as sharply edged as if it had been ruled. The dust and the faint bloom that cover the rest of the panel are gone from it, wiped by something laid flat and pressed, over and over. It was not scrubbed. In the deepest grooves a few threads of white paper fibre have caught, and been left. It was copied.");
        AddDetail("dust,drift,black,charcoal,floor,stain,smudge", "The black is charcoal, fine and dry and fresh, and where it has been trodden it has left the print of a boot sole. A few threads of pale paper fibre lie among it, caught on the grain of the dust.");
        AddDetail("bier,slab,rock,east wall", "The bier is cut from the wall rock and is as long as the others, and bare. A sprinkle of charcoal lies across its foot, where someone sat to work.");
        AddDetail("door,doorway,east,threshold", "The doorway out is as tall and narrow as the rest. Its jambs are carved, and not touched.");
        AddReadable("panel,inscription,inscriptions,strokes,rows,writing,carving,carvings,wall,signs", "Three rows of interlaced strokes run the width of the panel, each in seven groups. Above them stand three signs, one over each row: a ring crossed by a bar, a single wavering line like water, and a ring with a gap open to the north. The strokes are cut very deep and are as sharp as on the day they were made. You cannot read them, and it seems that no one could have read them in a very long time. Whoever came before you could not, either, or they would not have needed a copy to carry away.", "A great panel of strokes covers the west wall. You could {cmd}read{/} it.");
        AddSpawn("/domains/greenvale/corridor/mob/ancient_skeleton", 1);
    }
}
