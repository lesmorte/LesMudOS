// @inherit /std/npc

public class GqHallmasterScout : Npc
{
    public override void Create()
    {
        Name = "Veyla";
        Ids.Add("veyla");
        Ids.Add("hallmaster");
        Ids.Add("scout hallmaster");
        Ids.Add("hallmaster veyla");
        Ids.Add("woman");
        ShortDesc = "Veyla, Hallmaster of the Scouts";
        LongDesc = "A lean, weathered woman in a long grey-green cloak, with a short bow slung across her back and a hawk-feather pinned at her collar. She stands at the bow-and-cloak-pin station so quietly that visitors often walk past her before noticing, and she seems to find this funny.";
        Level = 30;
        MaxVitality = 160;

        AddChat("You walk loudly, but you will learn. I am Veyla, and I speak for the Scouts. Ask what you like.");
        AddChat("This is the whole of it. At the fifth level, a Scout may choose one of three paths: Rogue, Ranger or Bard. The choice is permanent. And nothing you already are is lost.");
        AddChat("Your evasion, your tracking, your precision: they travel with you. A path is a new road from the same pair of feet.");
        AddChat("The rite of pledging is not yet open. Come back when it is, and ask me again.");

        AddTopic("rogue", "The Rogue: stealth and assassination. Backstab costs twenty-five Endurance; it can only be used on a target that is not yet fighting the Rogue, it rolls to hit at two better than usual, and does triple damage if it lands. It starts the fight, so choose your moment. Opportunist is a trait: ten percent more chance of a critical blow. It asks no more experience than the common measure.");
        AddTopic("ranger", "The Ranger: ranged combat and survival. Power Shot costs twenty Endurance and needs a bow: a single attack, two better to hit, for double damage. Hunter's Mark costs ten Endurance; the target takes two more damage from every attack the Ranger makes for sixty seconds. It is a path for those who like to see the road ahead and what stands on it. It asks no more experience than the common measure.");
        AddTopic("bard", "The Bard: support, inspiration and utility. Song of Courage costs twenty Endurance; the Bard and every other player in the room gain one to hit and one to damage for sixty seconds. Discordant Note also costs twenty Endurance, and slows its target by thirty percent for twenty seconds. A Bard fights by making others better at it. It asks no more experience than the common measure.");
        AddTopic("knight,berserker,monk,warrior,warriors", "That is a Warrior's road. Gorrim speaks for them, over by the shield station. He is louder than I am, and you will not miss him.");
        AddTopic("cleric,templar,oracle,priest,priests", "That is a Priest's path. Hallmaster Anselm, at the lamp station, will tell you.");
        AddTopic("mage,druid,necromancer,mages", "A Mage's path. Hallmaster Ilsabet, at the star station. Ask her something specific. She does not like vague.");
        AddTopic("spellblade,beastmaster,artificer,adventurer,adventurers", "The Adventurers' paths. Hallmaster Jory, at the lantern station, speaks for them, and will tell you a story with the answer.");
        AddTopic("paths,path,classes,class,advanced,three", "Three roads for a Scout. Rogue, stealth and assassination. Ranger, ranged combat and survival. Bard, support, inspiration and utility. Ask me about any of them by name.");
        AddTopic("pledge,choose,choice,choosing,rite,permanent,ceremony", "The hall is not yet taking pledges. The rite will be opened soon. Come back once you have reached your fifth level and it is, and ask me then. Remember that the choice is permanent. Look at all three first.");
        AddTopic("level,fifth,five,retain,keep,lose,lost", "At the fifth level a Scout may choose a path. What you were before you keep: Evasion, Tracking, Precision Strike. The path is added to it, not swapped for it.");
        AddTopic("scout,scouts,calling,agility,precision", "Scouts are speed and sight. Agility, precision, the shot or blow that lands where it must. You begin with Evasion, Tracking and Precision Strike. The three paths are where you may take them.");
        AddTopic("hall,hall of legends,banners,chronicle,envoys", "Read the banners on the east wall for a line on every path. The Chronicle Gallery is east of here and the Envoys' Gallery west.");
        AddTopic("attributes,attribute,grace,insight,resolve,quickness,lean,leans,stats,points", "A Scout is Grace: the hand that finds the mark, and the blow that lands where it counts. Insight to read the ground, and Resolve to keep going when others stop. You will hear nothing of my Grace. That is the point of it.");
        AddEmote("Veyla turns a hawk's feather between her fingers and watches the doors.", 8);
        AddEmote("Veyla smiles faintly at a visitor who has just realized she was standing there all along.", 6);
    }
}
