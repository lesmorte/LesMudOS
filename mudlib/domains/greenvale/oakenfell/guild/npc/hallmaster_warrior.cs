// @inherit /std/trainer

/// <summary>A hallmaster of the Hall of Callings, and the Warrior's rank trainer for rank III and above (docs/specs/skill-ranks.md).</summary>
public class GqHallmasterWarrior : Trainer
{
    public override void Create()
    {
        Name = "Gorrim";
        Ids.Add("gorrim");
        Ids.Add("hallmaster");
        Ids.Add("warrior hallmaster");
        Ids.Add("hallmaster gorrim");
        Ids.Add("man");
        ShortDesc = "Gorrim, Hallmaster of the Warriors";
        LongDesc = "A square, grey-bearded man with a boxer's nose and a rack of old scars across both forearms. He wears plain mail under a red tabard and stands at the shield-and-sword station with his weight settled, as though the floor were a rampart. He looks at newcomers the way a good sergeant looks at new recruits: carefully, and without contempt.";
        Level = 30;
        MaxVitality = 200;

        AddChat("Welcome to the Hall of Callings. I am Gorrim, and I speak for the Warriors. Look about you. Ask what you like.");
        AddChat("Here is how it goes, plain as I can put it. When a Warrior reaches the fifth level, they may choose one of three paths: Knight, Berserker or Monk. The choice is permanent. And nothing you already are is lost.");
        AddChat("The shield, the training, the sword arm: all of it comes with you. A path adds to a Warrior. It never takes away.");
        AddChat("The rite of pledging is not open yet. Come back in a while and ask me again.");

        AddTopic("knight", "The Knight: the heavy armor defender. Two gifts come with the path. Shield Wall is a skill that costs twenty Endurance and needs a shield in hand; for twenty seconds, the Knight's chance to block rises by forty in the hundred. Guardian's Resolve is a trait: two points of armor while wearing heavy armor, one otherwise. The Knight is the one who does not move. It asks no more experience than the common measure.");
        AddTopic("berserker", "The Berserker: the aggressive melee damage dealer. Reckless Blow costs twenty-five Endurance and is an attack for double damage, though the Berserker's armor is two points lower for ten seconds after. Battle Fury is a trait: fifteen percent haste while below half your Vitality. It is a path for those who would sooner give a blow than take one. It asks no more experience than the common measure.");
        AddTopic("monk", "The Monk: the disciplined martial artist. Flurry costs twenty-five Endurance and works unarmed only: three unarmed attacks in one stroke, each rolled separately. Iron Body is a trait: unarmed blows do three to six damage, and the Monk gains two points of armor while wearing no body armor and no shield. A quiet road, and a hard one. It asks no more experience than the common measure.");
        AddTopic("rogue,ranger,bard,scout,scouts", "That is a Scout's path, and not mine to describe. Hallmaster Veyla speaks for the Scouts, and she stands in this hall, at the bow station.");
        AddTopic("cleric,templar,oracle,priest,priests", "That is a Priest's path. Hallmaster Anselm speaks for them, at the lamp station, and he will tell you more kindly than I could.");
        AddTopic("mage,druid,necromancer,mages", "That is a Mage's path. Hallmaster Ilsabet speaks for them, at the star station. Do not interrupt her. She enjoys it too much.");
        AddTopic("spellblade,beastmaster,artificer,adventurer,adventurers", "That belongs to the Adventurers. Hallmaster Jory speaks for them, at the lantern station, and has more stories than answers.");
        AddTopic("paths,path,classes,class,advanced,three", "Three roads for a Warrior. Knight, the heavy armor defender. Berserker, the aggressive melee damage dealer. Monk, the disciplined martial artist. Ask me about any of them by name.");
        AddTopic("pledge,choose,choice,choosing,rite,permanent,ceremony", "The hall is not yet taking pledges. The rite will be opened soon, and then you may come to me when you have reached your fifth level. When it is open, remember: the choice is permanent. Think on it while you wait.");
        AddTopic("level,fifth,five,retain,keep,lose,lost", "At the fifth level a Warrior may choose a path. What you were before, you keep: Shield Bash, Defensive Training, Weapon Proficiency, all of it. The path is added on top.");
        AddTopic("warrior,warriors,calling,front line,fight", "Warriors hold the line. Defense, melee, mastery of the weapon: that is the whole of the calling. Shield Bash, Defensive Training and Weapon Proficiency are what you begin with. The three paths are what you may grow into.");
        AddTopic("hall,hall of legends,banners,chronicle,envoys", "The banners on the east wall name every path. The Chronicle Gallery lies east of this chamber, the Envoys' Gallery west. Go and look.");
        AddTopic("attributes,attribute,might,vigor,resolve,strength,lean,leans,stats,points", "A Warrior leans on Might and Vigor, and holds on by Resolve. The Knight takes Resolve furthest, the Berserker takes Might, and the Monk takes the stillness between. Place your points where your path leans, and not where your pride does.");
        AddTopic("ranks,rank,train,training,teach,teaching,trainer,third,third rank", "Rank three and up. Here, from me. Rank two is the Hollow's yard. Say {cmd}ranks{/}. Then {cmd}train bash{/}. Bring gold.");
        TeachCalling("warrior", 3, 5);
        ElsewhereLine = "The second rank is taught in Vincent's Hollow.";
        AddEmote("Gorrim rolls his shoulders and surveys the hall like a man counting the exits.", 8);
        AddEmote("Gorrim straightens a rack of practice shields by an inch.", 6);
    }
}
