// @inherit /std/room

public class SrM03 : Room
{
    public override void Create()
    {
        ShortDesc = "Old Miller's Farm, the Lane End";
        LongDesc = "A cart lane leaves the road to the west between two gateposts, its surface scored deep by years of grain wagons. At the far end you glimpse a farmhouse with a sagging roof and a great barn, and above them all, on the rise beyond, the sails of a windmill turning slowly against the sky. A painted board is nailed to one gatepost, and a pair of hens pick at the gravel as if they own it. The air smells of flour, straw and warm dung.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m02";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m04";
        Exits["west"] = "/domains/greenvale/corridor/south_road/w1";
        ResetSeconds = 120;
        DangerBand(5, 6);

        AddDetail("lane,cart lane,ruts", "The lane is a farmer's road, far more rutted than the highway. A strip of grass down the middle is cropped short by whatever the farm keeps.");
        AddDetail("gateposts,gatepost,posts,gate", "Two oak gateposts, grey and split, with iron hinges rusted to the shape of old hands. The gate itself leans against the hedge, unhung for a generation.");
        AddDetail("windmill,mill,sails,hill", "The windmill stands on a green hill north-west of the farm, four white sails turning. Even from here you can hear the long wooden sigh of it.");
        AddDetail("farmhouse,barn,farm,roof", "A low stone farmhouse with a sagging thatch, and a barn big enough to stable a regiment. Both look better kept than the lane suggests.");
        AddDetail("hens,hen,chickens,gravel", "Brown hens, busy and unafraid. One gives you the look of a creature that has seen a great many travelers.");
        AddReadable("board,painted board,sign", "The board is hand-lettered in white on black:\n\"OLD MILLER'S FARM. GRIST GROUND, FLOUR BY THE SACK. TOLL ONE MEASURE IN SIXTEEN. PLEASE SHUT THE GATE. THERE IS NO GATE. SHUT IT ANYWAY.\"", "A painted board is nailed to the gatepost. You could {cmd}read{/} it.");
    }
}
