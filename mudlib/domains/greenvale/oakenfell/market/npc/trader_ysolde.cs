// @inherit /std/npc

public class MkTraderYsolde : Npc
{
    public override void Create()
    {
        Name = "Ysolde Fenwick";
        Ids.Add("ysolde");
        Ids.Add("ysolde fenwick");
        Ids.Add("trader");
        Ids.Add("dyer");
        Ids.Add("cloth seller");
        Ids.Add("stallholder");
        ShortDesc = "Ysolde Fenwick, a cloth merchant";
        LongDesc = "A graceful, grey-haired woman in a gown of deep blue, her hands stained to the wrist in dye. Bolts of cloth hang from her stall in every colour of the rainbow.";
        Level = 1;
        MaxHp = 15;

        AddChat("Cloth! Wool and linen, silk and dye! Mind the colour, it runs.");
        AddChat("Greenvale blue, Greenvale green: dyed in the Mere, and no finer colours south of the mountains.");
        AddChat("I dress half the Assembly and a quarter of the Hall of Legends. Come, feel the cloth.");
        AddTopic("cloth,wool,linen,silk", "I sell to the tailors and costumers of the city by the bolt. For a traveler's cloak, ask the Outfitter, who sells ready-made. I sell the cloth that goes into the cloaks.");
        AddTopic("river,mere,dye", "The River Mere gives us our blue, and the Elderwood our greens. When the Elderwood road is closed, my greens go up in price.");
        AddTopic("assembly,hall of legends,customers", "Gentlefolk of every faction come to my stall. I keep my tongue still and my scissors sharp.");
        AddEmote("Ysolde Fenwick drapes a bolt of green cloth over her arm and smiles.", 8);
    }
}
