// @inherit /std/npc

public class SrFarmwifeEdda : Npc
{
    public override void Create()
    {
        Name = "Edda";
        Ids.Add("edda");
        Ids.Add("farmwife");
        Ids.Add("woman");
        Ids.Add("miller's daughter");
        Ids.Add("daughter");
        ShortDesc = "Edda, the miller's daughter";
        LongDesc = "A sturdy woman of thirty with her sleeves rolled to the elbow, a flour-dusted apron, and a basket of eggs on her hip. She has her father's pale eyes and a good deal more patience.";
        Level = 2;
        MaxVitality = 30;

        AddChat("Eggs, if you want eggs. Not for sale; I'd sooner give you a dozen than haggle over them.");
        AddChat("Father says you can read the weather in the sails. I read it in his knees.");
        AddChat("If you're going on to the Rest, tell Aldith I'll send the flour the day after tomorrow.");
        AddTopic("farm,eggs,hens,hen", "We keep forty hens and a bad-tempered goose. The goose guards the gate better than the dog ever did.");
        AddTopic("father,leofric,miller", "He's eighty if he's a day and won't be told. He still climbs to the mill each morning, and I follow with a rope, in case.");
        AddTopic("wolf,wolves,fox,danger", "I'll not let the children past the orchard these days. A grey wolf came to the hen-house in Highsun and took the dog's bowl for good measure. The rangers say they will send someone.");
        AddTopic("travelers,north,road,oakenfell", "They come through all summer, drovers and pedlars and the odd boy with a sword and big ideas. The ones who leave the road, well, we see them again in the Hollow, mostly. Walking slow.");
        AddTopic("ranger,rangers,riverwatch", "Mildred, at the bridge. She's the only one who ever comes to the farm, and only for the cider.");
        AddEmote("Edda scatters grain and the hens swarm her ankles.", 8);
        AddEmote("Edda shades her eyes and looks up at the mill.", 6);
    }
}
