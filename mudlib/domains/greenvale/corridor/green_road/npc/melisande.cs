// @inherit /std/npc

public class GrMelisande : Npc
{
    public override void Create()
    {
        Name = "Melisande";
        Ids.Add("melisande");
        Ids.Add("keeper");
        Ids.Add("shrine keeper");
        Ids.Add("woman");
        Ids.Add("old woman");
        ShortDesc = "Melisande, keeper of the Old Shrine";
        LongDesc = "A small, brown, weathered woman of sixty with a birch broom in one hand and a lamp-oil flask at her belt. Her grey braid hangs to her waist, and her hands are those of someone who has done a great deal of sweeping.";
        Level = 3;
        MaxVitality = 40;

        AddChat("Keep the light. Keep the road. That is all there is to it, dear, and it is plenty.");
        AddChat("The lamp has not gone out in my mother's day, nor in her mother's. I only fill it.");
        AddChat("Leave a little something, if you have it. It does not matter what.");
        AddTopic("shrine,old shrine,lamp,light", "It is for the ones who went before and the ones who did not arrive. The lost, if you like. Wagon-folk, pilgrims, a bride once. Nobody knows who built it. Somebody had to, and somebody has to keep it.");
        AddTopic("offerings,ribbons,bread,coins", "I take down the offerings each dawn and bury what will rot. The coins go to the Rangers for the causeway. It is the same every year.");
        AddTopic("acorns,acorn,twig,knot,knotted twig", "The acorns and twigs? I do not put them there. I do not move them either. They come in the night, and I leave them be. Mind you, no harm has come to the lamp in all my years.");
        AddTopic("beech,tree,carvings,initials", "The beech is older than the shrine, I believe. People carve their names in it. I let them. It's a fair way of leaving something behind.");
        AddTopic("road,causeway,goblins,danger", "The causeway is kept by the carters and the Rangers, between them. The goblins do not come to the shrine. I could not tell you why.");
        AddTopic("rangers,riverwatch,watchtower,tower", "The Rangers used to send a man every week for the lamp-oil. They stopped when the post was given up. I get the oil from the caravans now.");
        AddEmote("Melisande sweeps the step with short, patient strokes.", 6);
        AddEmote("Melisande trims the lamp wick with her thumbnail and the flame steadies.", 6);
        AddEmote("Melisande hums something with no tune to it, and the sound seems to belong to the clearing.", 6);
    }
}
