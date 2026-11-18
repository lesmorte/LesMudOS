// @inherit /std/npc

public class FdTomasWick : Npc
{
    public override void Create()
    {
        Name = "Tomas Linch";
        Ids.Add("tomas");
        Ids.Add("linch");
        Ids.Add("lamplighter");
        Ids.Add("old man");
        ShortDesc = "Tomas Linch, the lamplighter";
        LongDesc = "A stooped old man in a long grey coat, with a pole over his shoulder topped by a little brass hook and a pouch of tapers at his belt. His hands are stained with oil and his eyes are the pale blue of old lamplight.";
        Level = 1;
        MaxHp = 15;

        AddChat("Seventy-one lamps from here to the river. I know each one by its smell.");
        AddChat("Forty years I have lit the square, and it has not once been dark in it that I did not know why.");
        AddTopic("lamps,lamp,lighting,light", "Whale oil in the main streets, tallow in the lanes, nothing at all at the end of Elder Lane. I was never told to light that one. I was told not to ask.");
        AddTopic("mosaic,oak,great oak,tree", "The oak in the stones? It is the right size, I am told. The roots especially. The man who laid it said the roots were the hard part, because there were so many and he had to leave room.");
        AddTopic("square,city,oakenfell", "The square is the oldest thing in the city that is open to everyone. There are older things, but you have to know the right people to see them.");
        AddEmote("Tomas Linch trims the linch of a lamp with a snip of his shears and wipes his fingers on his coat.", 6);
        AddEmote("Tomas Linch leans his pole against a lamp-post and looks up at the sky for a long moment, as if reading it.", 5);
    }
}
