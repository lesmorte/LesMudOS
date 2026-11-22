// @inherit /std/trainer

/// <summary>A hallmaster of the Hall of Callings, and the Mage's rank trainer for rank III and above (docs/specs/skill-ranks.md).</summary>
public class GqHallmasterMage : Trainer
{
    public override void Create()
    {
        Name = "Ilsabet";
        Ids.Add("ilsabet");
        Ids.Add("hallmaster");
        Ids.Add("mage hallmaster");
        Ids.Add("hallmaster ilsabet");
        Ids.Add("woman");
        ShortDesc = "Ilsabet, Hallmaster of the Mages";
        LongDesc = "A narrow, quick-eyed woman in deep blue robes, with silver spectacles perched on a sharp nose and ink on the side of her right hand. A thick book is open on the lectern at the star-over-book station, and she keeps one finger in it even while she is speaking to you.";
        Level = 30;
        MaxVitality = 130;

        AddChat("Ah, good. Someone with a question. I am Ilsabet, and I speak for the Mages. Ask precisely, and I shall answer precisely.");
        AddChat("The essentials. At the fifth level a Mage may choose one of three paths: Mage, Druid or Necromancer. The choice is permanent. Nothing you already are is lost. Do write that down.");
        AddChat("Your Arcane Bolt, your Focus Efficiency, your Magical Lore: they remain. A path is an extension of the work, not a reset of it.");
        AddChat("The rite of pledging is not yet open. It shall be opened soon. Return then, and bring questions.");

        AddTopic("mage,pure mage,fireball,arcane mastery", "The Mage is both a calling and a path, and I find that rather elegant. As a path it means the pure arcane spellcaster. Fireball costs thirty-five Focus and strikes every enemy of the caster in the room for nine to fourteen damage, ignoring armor. Arcane Mastery is a trait: spell damage is a quarter greater. It asks no more experience than the common measure.");
        AddTopic("druid", "The Druid: nature magic and shapeshifting. The Druid is given a spear to carry. Entangle costs twenty-five Focus, strikes for three to five damage and slows the target by forty percent for twenty seconds. Bear Form costs thirty Focus: for sixty seconds you gain fifteen maximum and current Vitality, your bare hands do five to eight damage in place of any weapon, and you gain one armor, but no spells may be cast while the form lasts. You may end it early. It asks no more experience than the common measure.");
        AddTopic("necromancer", "The Necromancer: undeath, curses and summons. Raise Skeleton costs forty Focus and calls a skeleton to fight for the caster for a hundred and twenty seconds; only one at a time. Drain Life costs twenty Focus, deals five to eight damage ignoring armor, and heals the caster by the damage done. Not a path for the squeamish. It asks no more experience than the common measure.");
        AddTopic("knight,berserker,monk,warrior,warriors", "Warriors. Hallmaster Gorrim, at the shield station. He will tell you in short sentences.");
        AddTopic("rogue,ranger,bard,scout,scouts", "Scouts. Hallmaster Veyla, at the bow station, if you can find her. She is generally closer than she seems.");
        AddTopic("cleric,templar,oracle,priest,priests", "Priests. Hallmaster Anselm, at the lamp station. Do take your time with him; he insists.");
        AddTopic("spellblade,beastmaster,artificer,adventurer,adventurers", "Adventurers. Hallmaster Jory, at the lantern station. Do not ask him for a short answer.");
        AddTopic("paths,path,classes,class,advanced,three", "Three roads for a Mage. Mage, the pure arcane spellcaster. Druid, nature magic and shapeshifting. Necromancer, undeath, curses and summons. Ask about any by name.");
        AddTopic("pledge,choose,choice,choosing,rite,permanent,ceremony", "The hall is not yet taking pledges. The rite will be opened soon. When it is, and you have reached your fifth level, return to me. The choice is permanent, so read the banners and ask your questions beforehand. I shall be here.");
        AddTopic("level,fifth,five,retain,keep,lose,lost", "At the fifth level a Mage may choose a path. Arcane Bolt, Focus Efficiency and Magical Lore all remain. The path is added, not substituted.");
        AddTopic("calling,arcane,magic,spells,spellcasting,lore", "Spellcasting, utility, magical damage: the calling of the Mages. You begin with Arcane Bolt, Focus Efficiency and Magical Lore. The three paths are where that work leads.");
        AddTopic("hall,hall of legends,banners,chronicle,envoys", "The banners on the east wall describe every path in a line. The Chronicle Gallery is east and the Envoys' Gallery west.");
        AddTopic("attributes,attribute,insight,focus,lean,leans,stats,points", "A Mage is Insight first: it is the size of the well and the force of the bolt. Spirit comes second, and fills the well sooner. Vigor a Mage neglects, and the first cold night pays for it. Do not tell Anselm that I said so.");
        AddTopic("ranks,rank,train,training,teach,teaching,trainer,third,third rank", "Ranks, yes. The third working of a Mage's spells and every one after it are taught here, by me. The second you learn in Vincent's Hollow. Say {cmd}ranks{/} and I shall show you, precisely, what you may learn, what it costs and what you lack. Then {cmd}train{/} and the spell's word.");
        TeachCalling("mage", 3, 5);
        ElsewhereLine = "The second rank is taught in Vincent's Hollow.";
        AddEmote("Ilsabet turns a page, nods to herself, and underlines something.", 8);
        AddEmote("Ilsabet pushes her spectacles up her nose and peers over them at a visitor.", 6);
    }
}
