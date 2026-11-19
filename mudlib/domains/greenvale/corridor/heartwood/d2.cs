// @inherit /std/room

public class HwD2 : Room
{
    public override void Create()
    {
        ShortDesc = "The Thorn Thicket";
        LongDesc = "Thorn has taken this part of the wood: brambles as thick as a man's wrist, hawthorn grown to the height of a tree, and sloe and briar knotted among them into a wall on every side. The trail forces its way through in a tunnel barely wide enough for one, hooked and scratched on both flanks. The thorns are long and black and wet, and they catch at cloaks and pull. Strange things are tangled in them: tufts of fur, a scrap of cloth, the white curve of a rib. On the east side of the trail a stretch of the thicket has died back, grey and brittle among the green.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/d1";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/d3";
        DangerBand(9, 10);

        AddDetail("thorn,thorns,brambles,bramble,briar,hawthorn,sloe,thicket,wall", "The thorns are black and as long as a finger, and they grow in pairs. The thicket is so dense that the light comes through only as a dim brown glow, and the leaves rustle without any wind.");
        AddDetail("tunnel,trail,path,way,passage", "The trail is a low tunnel through the thorn, worn by animals and the odd hunter. Bent stems and snapped twigs show where others have come this way, and gone back, and not stopped.");
        AddDetail("tangles,fur,cloth,rib,scrap,scraps,tufts", "The tufts of fur are grey, russet and black. The cloth is green wool, rotten with weather, such as a ranger wears. The rib is deer, picked white, and hangs from a thorn at the height of an eye.");
        AddDetail("east,dead,dead thorn,brittle,died back,grey,grey thorn,east side,gap,stems,cut,cuts", "On the east side, a stretch of thicket the width of a doorway has died, and the stems are grey and brittle. Many have been cut, once, a long time ago, with a blade, and the thicket has grown back over the cuts and then died again. The pattern of the dead stems is a rough arch. Behind it, the dark is a little deeper than it should be.");
        AddDetail("ground,leaf,earth,floor", "The ground under the thorn is black, soft and bare, strewn with the hulls of sloes. Mice have made runs in it.");
        AddDetail("light,glow,brown,sky", "The sky is not to be seen. The thorns above make a roof so dense that the light is a dim brown stain, as in a cellar with a grating.");

        HiddenExit("east", "/domains/greenvale/corridor/heartwood/s1", "east,dead,dead thorn,brittle,died back,grey,grey thorn,east side,gap,stems,cut,cuts", 15,
            "You lay a hand on the grey stems where they make their rough arch, and they come away in brittle handfuls, old cuts and all. Behind them a low way opens in the thicket {cmd}east{/}.");
        AddSpawn("/domains/greenvale/corridor/mob/thorn_woodling", 2);
        AddSpawn("/domains/greenvale/corridor/mob/forest_stalker", 1);
    }
}
