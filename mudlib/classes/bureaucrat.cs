// @inherit /std/pclass

/// <summary>
/// The Mage's hidden class: the Clerk of the Chancery (docs/specs/bureaucrat.md, built on docs/specs/hidden-classes.md). Nobody offers it. A Mage
/// of level 5 or more finds a summons once, takes it to City Hall's inner door, does a short case there ("Jury Duty") and is offered the seal
/// at the end. Everything here is data; the register (/secure/hidden) and the Registrar (a /std/hiddengiver) do the work. Nothing in this
/// file, or in the files named in OwnPaths, may be shown to a character who does not hold the class or the summons.
///
/// STAGE 2 STATE: the class, the place, the people and the quest are built; the spells are NOT (another build makes the engine pieces and
/// measures balance first), so Skills is empty and the roll is CLOSED (SummonsOpen = false): nobody is summoned by chance. A summons is served only by
/// the level-10 wizard command `hidden grant NAME bureaucrat`, and the door re-issues a lost paper. Open the roll when the spells exist.
/// The six launch spells and the levels at which the class will give them (docs/specs/bureaucrat.md section 5), for the SkillAt table:
///   level 5 triplicate (Forms in Triplicate), 6 restrain (Restraining Order), 7 desist (Cease and Desist), 8 tape (Red Tape), 9 audit (Audit),
///   10 deny (Application Denied). Later: 12 letter, 14 stamp, 16 number, 18 summons, 20 process.
/// Each is a file in /skills/bureaucrat/ added to Skills with SkillAt(id, level).
/// </summary>
public class BureaucratClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Clerk of the Chancery";
        Title = "Clerk of the Chancery";
        Tier = "advanced";
        Parent = "mage";
        Guild = "chancery";
        Hidden = true;
        ExpModifier = 1.0;
        Tagline = "The Bureaucrat: nothing may happen until the correct form has been lodged.";
        Description = "BUREAUCRAT (the Clerk of the Chancery). A Mage who has taken the Chancery's seal. Control, not damage: slow, bar, interrupt, delay, and mark. " +
                      "Leans on Insight, then Resolve. Weapons: dagger, staff. Armor: light. You keep every Mage skill. The seal is for life.\n\n" +
                      "The Chancery of Oakenfell has never lost a war, because it has never agreed that one was properly declared. Its clerks carry no blade worth the name. " +
                      "They carry seals, ledgers and the absolute conviction that nothing may happen until the correct form has been lodged. Creatures that cannot read are " +
                      "bound all the same; ignorance of the procedure is no defence.";

        Require("level", "5");
        Require("class", "mage");
        Require("hidden", "bureaucrat");

        // The Mage's weapons and armour (a Mage keeps them); the class adds none and never a ranged weapon.
        Weapons.AddRange(new[] { "dagger", "staff" });
        ArmorTypes.AddRange(new[] { "light" });

        // Resources (docs/specs/bureaucrat.md section 3): they replace the Mage's (4 + 7, 14 + 1, 71 + 4).
        VitalityBase = 6; VitalityPerLevel = 7;
        EnduranceBase = 14; EndurancePerLevel = 1;
        FocusBase = 76; FocusPerLevel = 4;

        // The spells are not built yet (see the note above): no Skills, an empty SkillAt table.

        // The hidden-class pattern (hidden-classes.md section 4).
        QuestId = "jury-duty";
        GateKey = "inner-door";
        SerialBase = 4400;
        SummonsItem = "/domains/greenvale/oakenfell/founders/obj/jury_summons";
        SummonsOpen = false; // CLOSED until the spells exist (bureaucrat.md section 16, stage 5)
        SummonsFloor = 10; SummonsBase = 2.0; SummonsStep = 0.5; SummonsGuarantee = 50; SummonsMinLevel = 5;
        string F = "/domains/greenvale/oakenfell/founders/";
        OwnPaths.AddRange(new[]
        {
            F + "chancery", F + "obj/", F + "npc/registrar_ashcombe", F + "npc/clerk_crake", F + "npc/pruett", F + "npc/marrowby", F + "npc/goose",
            "/classes/bureaucrat", "/skills/bureaucrat/",
        });

        SummonsFound = "Among the coins and oddments you take from {corpse}, one thing is not like the others: a stiff buff-coloured paper, folded in three, with your name on it in a clerk's hand. You do not remember picking it up. It is already in your hand.";
        BoundLine = "It is addressed to you. It is not a thing one drops.";
        SummonsText =
            "                      THE CHANCERY OF OAKENFELL\n" +
            "                    SUMMONS TO ATTEND AS JUROR\n" +
            "              Form 27-J (third copy: the bearer's copy)\n" +
            "\n" +
            "  Reference:  CH/J/{serial}-B\n" +
            "  Issued:     {date}, being the date on which you read this\n" +
            "  To:         {Name}, a person of sufficient standing to be inconvenienced\n" +
            "\n" +
            "  You are hereby summoned to attend the Chancery as a juror in a matter\n" +
            "  to be heard. Present this summons at the inner door of City Hall,\n" +
            "  Founder's Square, Oakenfell. Knock.\n" +
            "\n" +
            "  You must attend within fourteen days of the date of issue. The date of\n" +
            "  issue is the date on which you read this. The Chancery does not regard\n" +
            "  this as circular.\n" +
            "\n" +
            "  Bring this summons. Do not bring a weapon, an opinion or a friend. You\n" +
            "  will not be required to remember anything. You will be required to be\n" +
            "  present while it happens.\n" +
            "\n" +
            "  Failure to attend renders you liable to a fine of THIRTY-SEVEN GOLD AND\n" +
            "  ONE THIRD OF A GOLD (37 1/3), together with one eighth of a gold for\n" +
            "  the paper this notice is written on. The Chancery will write to you\n" +
            "  about the fine. It has never been known to finish the letter.\n" +
            "\n" +
            "  Jurors are paid. The rate is one gold. It is entered.\n" +
            "\n" +
            "                                   For the Chancery, M. Ashcombe, Registrar.\n" +
            "                                   Stamped in red. NOTHING IS DONE UNTIL IT\n" +
            "                                   IS ENTERED.";
        GateKnock = "You knock on the inner door, under the gilt letters. A voice on the other side says, without hurry, 'Reference?' You hold up the summons. The door opens, politely, as though it had been about to.";
        GateAgain = "You knock. 'Come in,' says the voice. 'You are expected. You are always expected. It is the Chancery's only courtesy.'";
        GateOpen = "The inner door opens before you knock. It has your file.";
        GateLook = "The door is plain oak under the gilt, with a brass knocker in the shape of a rubber stamp. It looks as though it has been expecting someone with paper.";
        GateReissue = "'Paper,' says the voice, and a duplicate of your summons comes through the hatch.";
    }
}
