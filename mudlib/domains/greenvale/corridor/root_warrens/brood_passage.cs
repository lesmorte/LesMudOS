// @inherit /std/room

public class RwBroodPassage : Room
{
    public override void Create()
    {
        ShortDesc = "The Gnaw-Slope";
        LongDesc = "The slope runs down at a steady angle, wide enough for a cart, and its floor is worn glassy and smooth by countless bodies. Goblin steps once cut into it have been ground to nothing, and the walls are gouged by teeth to a height above a man's head. The air is warm and foul and moves in and out like a breath, and from below comes a low, slow, grinding sound that you feel in your teeth. Tufts of fur, droppings and scraps of bone are caught in every crack of the walls. Nothing stirs here at all, and the quiet is the worst of it.";
        Exits["up"] = "/domains/greenvale/corridor/root_warrens/root_hall";
        Exits["down"] = "/domains/greenvale/corridor/root_warrens/nest";
        ResetSeconds = 300;

        AddDetail("slope,floor,steps,step,ramp", "The floor is polished like the inside of a bowl, shallow in the middle and rising at the sides, where something large has dragged itself up and down. The old goblin steps can be found only as faint ridges in the glaze.");
        AddDetail("walls,wall,gouges,gouge,teeth,marks", "The gouges are deep, long and in pairs, the width of two fingers apart. They begin at about the height of your waist and rise to well above your head, the work of something that gnaws as it walks.");
        AddDetail("fur,tufts,bone,bones,scraps,cracks", "The fur is grey, coarse and plentiful. Among the bones are the long bones of deer and the skull of a wolf, cracked open, and the smaller shapes of what might once have been goblin tools.");
        AddDetail("air,breath,smell,sound,grinding,warm", "The air comes up the slope in a slow tide, warm and damp and sour with musk, and it goes down again a long breath later. The grinding underneath is steady and unhurried, like a mill-wheel in the next valley.");
        AddDetail("dung,droppings,dark", "The droppings in the corners are as big as a man's fist, and they are fresh.");
    }
}
