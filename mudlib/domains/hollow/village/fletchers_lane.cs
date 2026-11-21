// @inherit /std/room

/// <summary>Fletcher's Lane: a short street off the square that joins the new shops, the shrine and the way to the Practice Yard.</summary>
public class VhFletchersLane : Room
{
    public override void Create()
    {
        ShortDesc = "Fletcher's Lane";
        LongDesc = "A lane of beaten earth, hardly a cart wide, leaves the corner of the square between rows of low timber buildings. Feathers, curled shavings and the odd cut-off arrow shaft lie in the gutters, and the whole street smells of sawn yew and hot glue. A painted bow hangs over one door, an outside stair climbs to a loft under the eaves, and a small stone shrine stands at the end of the lane, a carved hand above its open door. Beyond a postern door in the palisade to the east, arrows thud steadily into straw.";
        Exits["square"] = "/domains/hollow/village/square";
        Exits["yew"] = "/domains/hollow/village/strung_yew";
        Exits["loft"] = "/domains/hollow/village/hedge_scholars_loft";
        Exits["shrine"] = "/domains/hollow/village/shrine";
        Exits["yard"] = "/domains/hollow/village/practice_yard";
        Safe = true;

        AddDetail("lane,street,ground,earth", "Packed earth, swept now and then by someone who gave up. Every few yards a cut-off arrow shaft or a curl of yew shaving has been trodden into it.");
        AddDetail("feathers,shavings,shafts,shaft,gutters,gutter,arrow shafts", "Goose feathers, grey and white, drift in the gutters among yew shavings and shafts that someone has cut too short. A boy is collecting the feathers into a sack and pretending he is not.");
        AddDetail("buildings,building,timber,row,rows", "Low timber buildings with deep eaves and shutters propped open, each one a small trade: a bowyer's, a scholar's loft over a net-maker's, and, at the end, the shrine. They lean companionably.");
        AddDetail("sign,painted bow,bow,hanging sign", "A bow is painted on a board in black on yellow, with a taut string drawn across it in real gut. A line of lettering underneath reads \"THE STRUNG YEW\". The door beneath it is open.");
        AddDetail("stair,stairs,outside stair,steps,eaves", "A rough outside stair of weathered boards climbs the side of the building to a door under the eaves. A hand-lettered board nailed to the rail says \"Loft. Knock. Do not touch the books.\"");
        AddDetail("shrine,stone shrine,door", "A small shrine of grey fieldstone at the end of the lane, scarcely larger than a cottage. Over its open door a stone hand has been carved, palm up and fingers loose, as if it were asking for nothing at all.");
        AddDetail("hand,stone hand,carving", "A hand carved in the lintel, palm up and fingers loose, worn almost smooth. Someone has set a small cup of water on the sill below it.");
        AddDetail("postern,postern door,palisade,east,straw,arrows", "A small postern door stands propped open in the palisade at the east side of the lane. Beyond it you can see the straw butts of a practice yard, and hear the thud of arrows going in.");
    }
}
