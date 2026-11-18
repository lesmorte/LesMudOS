// @inherit /std/npc

public class RvLennaSorrel : Npc
{
    public override void Create()
    {
        Name = "Lenna Sorrel";
        Ids.Add("lenna");
        Ids.Add("sorrel");
        Ids.Add("clerk");
        Ids.Add("trade clerk");
        ShortDesc = "Lenna Sorrel, a trade clerk";
        LongDesc = "A neat young woman in a brown clerk's coat, ink on her thumb and a pair of spectacles on a cord. She writes figures in a ledger with swift, tiny strokes.";
        Level = 1;
        MaxHp = 15;

        AddChat("Freight, bills of lading, weights and measures. How may I assist?");
        AddChat("The river never stops. Neither do the forms.");
        AddTopic("freight,cargo,rates,barge,barges,shipping", "Freight by barge is charged by weight and distance, at the rates posted on the wall. I stamp the bill of lading while you wait. It is not exciting, but it is correct.");
        AddTopic("passage,downriver,ticket,travel,travelers,sail", "I am sorry, but passage downriver is not yet sold. The Trade Office books only freight for the present. When we begin to take passengers, there will be a notice on the quay.");
        AddTopic("river,mere,chart,shoals", "The chart on the wall is the one the barge masters use. It is out of date in two places, and I have the pencil marks to prove it.");
        AddEmote("Lenna Sorrel dips her pen and writes a column of figures, faster than seems possible.", 6);
        AddEmote("Lenna Sorrel stamps a bill of lading with a satisfied thump.", 5);
    }
}
