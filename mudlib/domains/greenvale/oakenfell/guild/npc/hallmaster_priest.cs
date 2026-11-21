// @inherit /std/npc

public class GqHallmasterPriest : Npc
{
    public override void Create()
    {
        Name = "Anselm";
        Ids.Add("anselm");
        Ids.Add("hallmaster");
        Ids.Add("priest hallmaster");
        Ids.Add("hallmaster anselm");
        Ids.Add("man");
        ShortDesc = "Anselm, Hallmaster of the Priests";
        LongDesc = "A soft-spoken, heavy-set man in white robes belted with undyed rope, with a round, kind face and deep lines of laughter around his eyes. He stands at the lamp-and-open-hand station with his hands folded, and everyone who talks to him leaves feeling slightly more patient than they arrived.";
        Level = 30;
        MaxVitality = 170;

        AddChat("Peace to you, traveler. I am Anselm, and I speak for the Priests. There is no hurry. Ask what you like.");
        AddChat("Let me tell you how it stands. At the fifth level, a Priest may choose one of three paths: Cleric, Templar or Oracle. The choice is permanent. And nothing of what you already are is lost.");
        AddChat("Your healing, your blessing, your divine knowledge: they stay with you. A path is a deepening, never a departure.");
        AddChat("The rite of pledging is not yet open. It will be soon. Come back, and we shall speak again.");

        AddTopic("cleric", "The Cleric: healing and protection. Greater Heal costs thirty-five Focus and restores thirty-five Vitality to the caster or to someone beside them. Sanctuary costs twenty-five Focus: the one it touches, caster or another, absorbs the next fifteen damage, for up to a minute. It is a gentle path, though it has held many a line. It asks no more experience than the common measure.");
        AddTopic("templar", "The Templar: the holy warrior. The Templar is given a sword and heavy armor to wear. Smite costs twenty Focus: a weapon attack that, if it hits, adds six damage which ignores armor. Holy Armor is a trait: one more point of armor, always. It is a path for those whose faith has a hard edge. It asks no more experience than the common measure.");
        AddTopic("oracle", "The Oracle: divine support and foresight. Foresight costs twenty-five Focus and raises dodge by twenty percent for thirty seconds, on the caster or someone beside them. Read Fate costs ten Focus and shows a creature's exact Vitality, the actions it can take with their timing, and how many seconds until it next acts. The Oracle sees what is coming. It asks no more experience than the common measure.");
        AddTopic("knight,berserker,monk,warrior,warriors", "A Warrior's road. Hallmaster Gorrim, at the shield station, will tell you. He is gruff, but good.");
        AddTopic("rogue,ranger,bard,scout,scouts", "A Scout's road. Hallmaster Veyla, at the bow station, can tell you. Look for the cloak; you will not hear her coming.");
        AddTopic("mage,druid,necromancer,mages", "A Mage's road. Hallmaster Ilsabet, at the star station, speaks for them.");
        AddTopic("spellblade,beastmaster,artificer,adventurer,adventurers", "The Adventurers' road. Hallmaster Jory, at the lantern station, speaks for them. Sit down first.");
        AddTopic("paths,path,classes,class,advanced,three", "Three roads for a Priest. Cleric, healing and protection. Templar, the holy warrior. Oracle, divine support and foresight. Ask me about any by name.");
        AddTopic("pledge,choose,choice,choosing,rite,permanent,ceremony", "The hall is not yet taking pledges, my friend. The rite will be opened soon, and then you may come to me at your fifth level. Do not be afraid of the choice, but do know it is permanent. Pray on it, if you are the praying kind.");
        AddTopic("level,fifth,five,retain,keep,lose,lost", "At the fifth level a Priest may choose a path. Minor Heal, Blessing and Divine Knowledge you keep. The path is built upon them, not in place of them.");
        AddTopic("priest,priests,calling,divine,faith,healing", "Healing, blessings and defensive magic: the work of the Priests. You begin with Minor Heal, Blessing and Divine Knowledge. The three paths show what a Priest may grow into.");
        AddTopic("hall,hall of legends,banners,chronicle,envoys", "The banners on the east wall name every path. The Chronicle Gallery lies east; the Envoys' Gallery lies west.");
        AddEmote("Anselm bows his head briefly over folded hands.", 7);
        AddEmote("Anselm trims the wick of a small lamp at his station.", 6);
    }
}
