// @inherit /std/npc

public class FdDallaPryce : Npc
{
    public override void Create()
    {
        Name = "Dalla Pryce";
        Ids.Add("dalla");
        Ids.Add("pryce");
        Ids.Add("cloth seller");
        Ids.Add("woman");
        ShortDesc = "Dalla Pryce, a cloth-seller";
        LongDesc = "A brisk woman of middle years in a patched blue cloak, a bolt of undyed linen under one arm and a measuring cord around her neck. She has the look of someone who knows the price of everything and the weather for tomorrow.";
        Level = 1;
        MaxVitality = 15;

        AddChat("Mind the mosaic, dear, you are walking on the founders. Everyone does it. Everyone says sorry.");
        AddChat("I come to the square every morning for the notices. You learn more in an hour here than in a month at market.");
        AddTopic("notices,board,notice,news", "Read it before you do anything, dear. Roads shut, hounds lost, the Assembly sitting. I have never known the board to be wrong, though it is sometimes late.");
        AddTopic("square,mosaic,oak,founders", "The oak in the stones. They say it was a real tree, the biggest anyone had seen, and that the town is named after the day it fell. I was told as a girl it was the mother of all the trees in Greenvale. I believed it.");
        AddTopic("cloth,linen,wool,market", "Linen from the east, wool from the vale, and silk once a year when the caravans come in. Go to the market for it, dear. I only sell what I carry.");
        AddEmote("Dalla Pryce shakes out a bolt of linen, squints at it against the light, and folds it away again.", 6);
        AddEmote("Dalla Pryce stops to read the notice board, nods to herself, and moves on.", 5);
    }
}
