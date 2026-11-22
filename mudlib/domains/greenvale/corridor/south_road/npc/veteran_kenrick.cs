// @inherit /std/npc

public class SrVeteranKenrick : Npc
{
    public override void Create()
    {
        Name = "Kenrick";
        Ids.Add("kenrick");
        Ids.Add("veteran");
        Ids.Add("soldier");
        Ids.Add("sellsword");
        Ids.Add("man");
        Ids.Add("old soldier");
        ShortDesc = "Kenrick, a grizzled veteran";
        LongDesc = "A broad, scarred man in a faded leather jerkin with a notched sword propped beside his stool and a cup of small beer in his fist. His left hand is missing two fingers, and he keeps it turned out of sight.";
        Level = 5;
        MaxVitality = 60;

        AddChat("Sit down, lad, lass, whatever you are. I'll not bite. Not before supper.");
        AddChat("The road is fine. The road is always fine. It's the bits either side that bite.");
        AddChat("I've walked it eleven times, and I only went wrong once. It's the once that stays with you.");
        AddTopic("road,north,wood,forest,eldergreen", "The Eldergreen's two kinds of dangerous, the kind you can see and the kind you can't. Wolves and goblin scouts you can see. The rest, you feel.");
        AddTopic("goblins,goblin,scouts,raiders", "Scouts first, just watching. Then raiders in the deep wood, a dozen at a time, with knives. Never fight them in the open. Never fight them at all, if you can walk around.");
        AddTopic("bandits,bandit,lookout,lookouts", "There's a man watching the road from the old sheepfold, west of here. I'd put coin on it. A lookout. Pay him no mind, pay him no coin, and keep the road.");
        AddTopic("heartwood,ruins,old ruins,mossgate,ancient", "Deeper in, past the Green Road, the forest gets old. Not old like a barn: old like a mountain. There are ruins in it, carved stone under moss, older than Greenvale. I did not stay.");
        AddTopic("hand,fingers,scar,scars", "A goblin warrior, in the dark. I lost two fingers, and he lost the rest of himself. Fair trade, the way I tell it.");
        AddTopic("riverwatch,rangers,ranger", "Good folk, the Rangers. They walk the road so we don't have to. If you see one, share your bread.");
        AddTopic("vigor,toughness,tough,hardy,survive,survived,survival,healing,wounds", "Vigor, they call it in the Hall. I call it being hard to kill. I am two fingers short and still here, because I could take a wound that would have put a better swordsman in the ground.");
        AddTopic("resolve,nerve,fear,courage,brave,bravery", "The ones who lasted had Resolve. They felt the same fear as the rest and put one foot forward anyway. I have known big men without it and small men with it, and I buried the big ones.");
        AddEmote("Kenrick turns his cup slowly in his scarred fingers.", 8);
        AddEmote("Kenrick glances up at the road and back down.", 6);
    }
}
