// @inherit /std/npc

public class RvHarbormasterIsolde : Npc
{
    public override void Create()
    {
        Name = "Rosamund Kell";
        Ids.Add("rosamund");
        Ids.Add("kell");
        Ids.Add("harbormaster");
        Ids.Add("harbourmaster");
        Ids.Add("master");
        ShortDesc = "Rosamund Kell, the harbormaster";
        LongDesc = "A tall, grey-haired woman in a long blue coat with a brass whistle on a chain and a slate under one arm. She surveys the quay with the cool eyes of someone who has never once lost track of a barge.";
        Level = 3;
        MaxVitality = 40;

        AddChat("State your business, or stand aside. Both are acceptable.");
        AddChat("Eleven piers, forty barges, one harbormaster. I have never mislaid a ship, and I do not intend to begin.");
        AddTopic("quay,piers,barges,harbor,harbour,harbormaster", "The Mere Quay is the busiest landing between here and the sea. Everything that floats up or down the Mere is mine to account for. If you leave a boat where it should not be, I will know before you have finished tying the knot.");
        AddTopic("passage,downriver,travel,sail,ticket", "Passage downriver is not yet sold. The barge masters carry freight only, and the Trade Office has not opened its books to travelers. Ask again in a season or two.");
        AddTopic("river,mere,river mere,shoals,hag", "The Mere is kind until it is not. Shoals below the north wall, and a stretch we call the Hag where the channel narrows. Barge masters learn those, or they learn them the hard way.");
        AddTopic("riverwatch,rangers,sergeant,stroud", "The Rangers keep an office on Old River Street. They look at the roads, and I look at the water. Between us, nothing gets through that should not.");
        AddEmote("Rosamund Kell blows a sharp blast on her whistle and points a barge master to a different pier.", 6);
        AddEmote("Rosamund Kell consults her slate, makes a mark and says to nobody in particular: \"Late again.\"", 5);
    }
}
