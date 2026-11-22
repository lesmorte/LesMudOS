// @inherit /std/npc

public class EeGwenllian : Npc
{
    public override void Create()
    {
        Name = "Commander Gwenllian";
        Ids.Add("gwenllian");
        Ids.Add("commander");
        Ids.Add("ranger commander");
        ShortDesc = "Commander Gwenllian of the Riverwatch";
        LongDesc = "A tall woman of fifty in a long green cloak, a silver eye-badge at her collar and a plain sword at her hip. Her hair is iron-grey and cropped short, her face is brown and creased from decades outdoors, and she stands the way a woman stands who has stood watch on a great many nights.";
        Level = 9;
        MaxVitality = 110;

        AddChat("Ever Vigilant. It sounds grand. It means we don't get to sit down.");
        AddChat("The Edge Post is the Rangers' main station on this road. We hold the gate, we walk the road, and we send word to Watchfort Crossing.");
        AddChat("You've come a long way from the Hollow. Good. The road north gets harder, and it is better to know it.");
        AddTopic("rangers,riverwatch,riverwatch rangers,order,ever vigilant,motto", "The Riverwatch Rangers keep Greenvale's roads and borders. We patrol, escort, hunt what needs hunting and watch for what does not yet have a name. Our headquarters is Watchfort Crossing, far east. This is the Edge Post, and I answer for the road from the Last Fences to the Merchant Camp.");
        AddTopic("north,green road,road north,merchant camp,oakenfell", "Beyond the Green Gate the road runs under dense canopy, and the light gives out. A merchant camp stands a few miles in, and travelers group there. Further on is the Crossroads Stone, then the Heartwood, which is old, and then the Approach to Oakenfell. I would not go north alone, if you are new to fighting.");
        AddTopic("south,south road,hollow,vincents hollow,farms,traveler's rest", "The South Road is farmland, and safer. Traveler's Rest has an inn and a bridge. You came that way. Be glad of it, and remember it when you are further in.");
        AddTopic("wolves,wolf,forest wolves", "Wolves have been bold this season. Packs along the Fern Banks and in the Narrows. They are cunning, and they strike when you are weak. Bring a friend, or bring a bow.");
        AddTopic("boar,boars,wild boar", "The boar do not trouble the road, but if you walk into their wallow, they will trouble you. A tusker can break a man's leg with one charge. Keep to the road, or take care.");
        AddTopic("goblins,goblin,scouts,goblin scouts", "Goblin scouts are new on this road, or newly bold. They count the traffic and report to something north. They do not seem to want to fight. I do not like that. If you meet them, do not assume they are alone.");
        AddTopic("spiders,spider,thicket", "The Spider Thicket grows every year. We burn the webs when we can, and the spiders come back. Do not venture in without a torch.");
        AddTopic("bounties,bounty,board,notice,work,job", "The board by the gate lists what we want done. Wolves, boar, goblin scouts, spiders. It is plain work. Show the quartermaster proof and she will see you are acknowledged. I make no promises about coin.");
        AddTopic("morwenna,hunter,missing", "Morwenna was a hunter, and a good one, who worked the east side of the wood. She set out in the autumn to look at something in the deep wood and has not been heard of since. If you find her, or what she left, tell me.");
        AddTopic("thorn pact,pact,oak,marked oak,markings,grove", "There are old groves in this wood, and old marks, and the Rangers do not disturb them. I will not say more than that. If you find marks, report them. Do not cut them. Do not move them.");
        AddTopic("patrol,patrols,patrol route", "We walk the Patrol Mile to the Green Gate and back each day, and south to the Broken Waystone twice a week. You may meet a patrol on the road, and if you do, answer their challenge civilly.");
        AddTopic("resolve,duty,endurance,standing,perseverance,steadfast,remembered", "The Rangers are not chosen for strength. We are chosen for Resolve: who is still at the post at the dawn watch after a bad night. Those on the slab at the Heartwood were remembered for it, and I would sooner be remembered for that than for anything I did with a blade.");
        AddTopic("insight,noticing,observe,observing,reading,track reading", "A ranger reads a track by Insight: the broken twig that is wrong, the silence that is wrong. It is not cleverness; it is looking, and remembering what you saw. If you find something that does not fit, tell me, and I would rather be told twice.");
        AddEmote("Commander Gwenllian studies the road north from the gate with narrowed eyes.", 6);
        AddEmote("Commander Gwenllian rolls a map from the table and weights its corners with stones.", 6);
        AddEmote("Commander Gwenllian speaks quietly to a passing ranger, who nods and hurries off.", 6);
    }
}
