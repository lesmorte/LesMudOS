// @inherit /std/npc

public class RvWatchRivergate : Npc
{
    public override void Create()
    {
        Name = "Tessaly Orme";
        Ids.Add("tessaly");
        Ids.Add("orme");
        Ids.Add("watchwoman");
        Ids.Add("watch");
        Ids.Add("guard");
        ShortDesc = "Tessaly Orme, a city watchwoman";
        LongDesc = "A square-built watchwoman in a green surcoat marked with a white oak, a boathook in one hand and a horn at her belt. Her gaze rests on the water the way another guard's rests on a crowd.";
        Level = 3;
        MaxHp = 40;

        AddChat("Keep clear of the edge. The Mere has taken better swimmers than you.");
        AddChat("Barges clear the arch by day only. After dusk, we let down the portcullis, and the river waits for morning.");
        AddTopic("road,river road,downriver,north,beyond,towpath", "The River Road follows the towpath out of the gate and down along the Mere. It is not yet open to travelers. The watch has no orders to let anyone through, on foot or by boat.");
        AddTopic("watch,gate,duty,water gate,rivergate", "We keep the arch, we keep the landing, and we fish out whatever the river leaves us. It is better than it sounds. Mostly it is driftwood.");
        AddTopic("river,mere,current,drowned,water", "Fast under the arch, slow elsewhere. The current has a mind of its own here. I have pulled out three carters and a magistrate's mule, and I remember them all.");
        AddEmote("Tessaly Orme hooks a branch out of the current and flicks it onto the landing.", 6);
        AddEmote("Tessaly Orme studies the downstream bend with the stillness of long habit.", 5);
    }
}
