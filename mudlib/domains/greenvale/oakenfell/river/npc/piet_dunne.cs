// @inherit /std/npc

public class RvPietDunne : Npc
{
    public override void Create()
    {
        Name = "Piet Dunne";
        Ids.Add("piet");
        Ids.Add("dunne");
        Ids.Add("warehouseman");
        Ids.Add("tallyman");
        ShortDesc = "Piet Dunne, a warehouseman";
        LongDesc = "A square, ruddy man with a pencil behind his ear and chalk on his sleeves, holding a slate with a list of figures that he glances at constantly. He counts sacks as they pass, whispering to himself.";
        Level = 1;
        MaxHp = 20;

        AddChat("Forty, forty-one... Do not talk to me. I will lose count.");
        AddChat("Everything that comes into Oakenfell by water passes through here. Everything. Even the things nobody asked for.");
        AddTopic("warehouse,warehouses,cargo,goods,storage", "These are leased by the merchant houses. The Consortium has the best, nearest the market. The rest of us take what is left, and the rats take what is left of that.");
        AddTopic("consortium,merchants,trade", "Merchant Consortium owns most of what is stored here, one way or another. I do not complain. Their bills are paid on time, which is more than I can say for the Assembly's.");
        AddTopic("rats,rat,pests", "Rats are the real shareholders. I count the grain in, I count the grain out, and I do not count the difference.");
        AddEmote("Piet Dunne ticks a mark on his slate and frowns at the next row of sacks.", 6);
        AddEmote("Piet Dunne counts under his breath: \"Thirty-eight, thirty-nine...\" and loses it. He swears quietly and starts again.", 5);
    }
}
