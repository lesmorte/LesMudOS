// @inherit /std/npc

public class SgWatchHobb : Npc
{
    public override void Create()
    {
        Name = "Hobb Tarrow";
        Ids.Add("hobb");
        Ids.Add("watchman");
        Ids.Add("watch");
        Ids.Add("hobb tarrow");
        Ids.Add("gate watchman");
        Ids.Add("guard");
        ShortDesc = "Hobb Tarrow, a gate watchman";
        LongDesc = "A heavyset man of fifty in a grey tabard with a green oak badge. He leans on his spear with the comfort of long practice, and he has the face of someone who is always about to say something.";
        Level = 4;
        MaxVitality = 50;

        AddChat("Welcome to Oakenfell! Mind your purse in the Row, mind your head in the Lane, and mind your manners everywhere, and you'll do fine.");
        AddChat("Thirty years on this gate, and I've seen every sort come through. Heroes, mostly, and the rest were liars who wanted to be.");
        AddChat("Don't ask me about the weather. I've had enough of weather.");
        AddTopic("city,oakenfell,districts", "Five districts, friend. Southgate here's the newest, then the Market to the north, the Founder's District beyond it with the old stones and the Hall of Legends west of that. The River District's east, along the Mere. Ask the folk at the Silver Stag if you want the rest.");
        AddTopic("motto,keystone,arch,oak", "From Strong Roots Grow Great Things. They say the Great Oak stood hereabouts, and the city grew round it. Old Tam at the Stag will tell you that story, if you buy him a drink.");
        AddTopic("rules,board,law,laws", "Read the board. I've said it ten thousand times and it never gets shorter. Keep your steel in its sheath, pay your toll, and don't argue with the Consortium's clerks. They will win.");
        AddTopic("inn,stag,silver stag,rooms,bed", "Straight up Wayfarer's Row and left into Stag Lane. The Silver Stag. Best stew south of the river, and the beds are clean.");
        AddTopic("wolf,road,wolves", "Wolves on the Vale Road? There's one that wanders. The Riverwatch hasn't caught it, and I haven't had the nerve to ask.");
        AddEmote("Hobb Tarrow tips his helm to a passing merchant and calls out a greeting.", 10);
        AddEmote("Hobb Tarrow watches the carts roll past and sighs contentedly.", 6);
    }
}
