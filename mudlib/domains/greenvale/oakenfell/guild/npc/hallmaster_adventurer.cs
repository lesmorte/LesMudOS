// @inherit /std/npc

public class GqHallmasterAdventurer : Npc
{
    public override void Create()
    {
        Name = "Jory";
        Ids.Add("jory");
        Ids.Add("hallmaster");
        Ids.Add("adventurer hallmaster");
        Ids.Add("hallmaster jory");
        Ids.Add("man");
        ShortDesc = "Jory, Hallmaster of the Adventurers";
        LongDesc = "A rangy, sun-browned man of no particular age in a patched travelling coat sewn with a dozen mismatched buttons, each from somewhere different. He leans on the lantern-and-compass station with the ease of a man who has been to most of the places on the map, and he is smiling before he speaks.";
        Level = 30;
        MaxVitality = 180;

        AddChat("Ha! A new face. Come in, sit, stand, whatever suits. I am Jory, and I speak for the Adventurers. Ask me anything. I will probably tell you a story as well.");
        AddChat("Short version. At the fifth level an Adventurer may choose one of three paths: Spellblade, Beastmaster or Artificer. The choice is permanent. And nothing you already are is lost.");
        AddChat("Your Survival Training, your Appraise, your Improvised Combat, your Traveler's Instinct: all yours still. Ours is the calling that keeps everything.");
        AddChat("The rite of pledging is not yet open. Soon, they tell me. Come back and we will see.");

        AddTopic("spellblade", "The Spellblade: hybrid martial and magical combatant. Arcane Strike costs ten Endurance and fifteen Focus: a weapon attack that adds five damage ignoring armor if it hits. Weapon Infusion costs twenty-five Focus: your wielded weapon does two more damage for sixty seconds. One warning, and I give it freely: the Spellblade is a demanding path. It needs a quarter more experience for every level than the common measure. Glory costs, as they say.");
        AddTopic("beastmaster", "The Beastmaster: explorer and companion handler. The Beastmaster is given a bow. Call Companion costs thirty Endurance: a wolf joins you and fights your enemies until it dies or you dismiss it, one at a time, following you from room to room. Pack Tactics is a trait: one more to hit whenever your companion is in the room. It asks no more experience than the common measure. I have had a wolf. Best friend I ever had.");
        AddTopic("artificer", "The Artificer: inventor, gadgeteer and crafter. Deploy Turret costs thirty-five Endurance: a turret stands in the room for sixty seconds and shoots your current enemy every six seconds for three to five damage; only one at a time, and it does not move. Gadget Toss costs fifteen Endurance: four to seven damage, and the target is slowed twenty percent for ten seconds. It asks no more experience than the common measure.");
        AddTopic("knight,berserker,monk,warrior,warriors", "Warriors? Gorrim, at the shield station. Grumpy, honest, dependable. The best sort.");
        AddTopic("rogue,ranger,bard,scout,scouts", "Scouts? Veyla, at the bow station. Lovely woman. She once found me in a forest where I was lost. I had not told her I was there.");
        AddTopic("cleric,templar,oracle,priest,priests", "Priests? Anselm, at the lamp station. He will tell you gently, even if the news is not.");
        AddTopic("mage,druid,necromancer,mages", "Mages? Ilsabet, at the star station. Brilliant, and she will say so only if you ask.");
        AddTopic("paths,path,classes,class,advanced,three", "Three roads for an Adventurer. Spellblade, the hybrid martial and magical combatant. Beastmaster, explorer and companion handler. Artificer, inventor, gadgeteer and crafter. Ask about any of them by name.");
        AddTopic("pledge,choose,choice,choosing,rite,permanent,ceremony", "The hall is not yet taking pledges. The rite will be opened soon. When it is, and you have reached your fifth level, come and tell me which you favor. Remember, friend, the choice is permanent. I would sleep on it, if I were you. I usually sleep on everything.");
        AddTopic("level,fifth,five,retain,keep,lose,lost", "At the fifth level an Adventurer may choose a path. Survival Training, Appraise, Improvised Combat and Traveler's Instinct all stay. The path sits on top, like a good coat over a better shirt.");
        AddTopic("adventurer,adventurers,calling,generalist,versatile,travel", "Versatile, adaptable, curious. We give up some of the sharp edge of the specialists for a lot of flexibility. We use most basic weapons, wear light armor, and see a good deal of the world. The three paths are bridges to more specific things, down the road.");
        AddTopic("hall,hall of legends,banners,chronicle,envoys", "The banners on the east wall have a line for every path. East of here is the Chronicle Gallery, west the Envoys' Gallery.");
        AddEmote("Jory fiddles with one of the buttons on his coat and smiles at a private memory.", 8);
        AddEmote("Jory pulls a worn map halfway out of his pocket, glances at it fondly, and puts it back.", 6);
    }
}
