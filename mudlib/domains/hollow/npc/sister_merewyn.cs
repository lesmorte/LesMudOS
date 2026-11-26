// @inherit /std/trainer

/// <summary>
/// Sister Merewyn, keeper of the Shrine of the Quiet Hand. Sells the Quiet Hand's blessing (a full restoration at once, for coin), gives
/// the quest The Quiet Offering and pays it with the Shrine-Blessed Mace. She is also the Priest's rank II trainer (docs/specs/skill-ranks.md).
/// Quest states are per player: 0 not started, 1 active, 2 completed (Adventurer.QueryQuest / SetQuest), as for Guildmaster Rowan.
/// </summary>
public class VhShrineKeeper : Trainer
{
    private const string Quest = "the-quiet-offering";
    private const string QuestTitle = "The Quiet Offering";
    private const string FlowerPath = "/domains/hollow/obj/craft/wildflower";
    private const string WaterPath = "/domains/hollow/obj/craft/clean_water";
    private const string ScavengerPath = "/domains/hollow/mob/goblin_scavenger";
    private const string MacePath = "/domains/hollow/obj/shrine_blessed_mace";
    private const string BlessingPath = "/domains/hollow/obj/shrine_blessing";

    public override void Create()
    {
        Name = "Sister Merewyn";
        Ids.Add("merewyn");
        Ids.Add("sister");
        Ids.Add("sister merewyn");
        Ids.Add("keeper");
        Ids.Add("shrine keeper");
        Ids.Add("woman");
        ShortDesc = "Sister Merewyn the shrine keeper";
        LongDesc = "A slight, grey-eyed woman in a plain undyed habit with the sleeves rolled to the elbow, who moves about the shrine without making a sound. She has a small scar through one palm, and she keeps that hand open when she speaks, as if there were something in it she were about to give you.";
        Level = 3;
        MaxHp = 30;

        AddChat("The Quiet Hand asks for nothing, and so it is never refused. Come in. Sit, if you want to.");
        AddChat("Some people come here to pray and some to be left alone. Both are welcome. I try to tell which, and mostly I am wrong.");
        AddChat("There is a small matter I could use a hand with, if you have a little time: {cmd}ask{/} me about the offering. Or if you are only weary, {cmd}buy blessing{/}.");
        AddChatAfterQuest(Quest, "The basin has never been so still. Thank you. The Quiet Hand does not forget what you did here, and neither do I.");
        AddTopic("shrine,quiet hand,hand,stone hand,faith,creed,prayer,pray,basin,water", "The Quiet Hand is an open hand, palm up, asking nothing. It is older than the village, though nobody can say how much, and Vincent's people built this shrine around it. We keep still water in its palm. A stilled mind, we say, is the best offering. Some come for comfort and some come for strength. I can offer a little of both.");
        AddTopic("blessing,bless,heal,healing,hurt,weary,rest,restore,wounds,tired,cost,price", "A blessing from the Quiet Hand costs six gold into the bowl, and it will leave you rested and whole, body and breath and focus. It will not raise the dead, I should say, and it will not make a coward brave. Say {cmd}buy blessing{/} and kneel. {cmd}browse{/} will show it to you.");
        AddTopic("yourself,myself,self,own hurts,own wounds,heal yourself,healing yourself,heal myself,healing myself,reach,reaching,half,third", "The Quiet Hand gives freely to the one who reaches for another, and sparingly to the one who reaches for himself. Heal a stranger and the light is full; heal yourself and you will find barely a third of it comes. I did not make the rule, and I have stopped arguing with it. A healer's own hurts are the slowest to close. Ask a friend to see to them, if you have one. If you have not, be patient.");
        AddTopic("offering,offerings,errand,quest,work,job,favour,favor,mace,blessed mace,shrine-blessed mace,help", "The pilgrims used to leave silver at the old cellar door, and now the goblin scavengers in the storage chamber carry it off. The basin wants a fresh offering to be reblessed: a wildflower and a flask of clean water, laid at the Hand, and a good reason for the scavengers to stay below. Bring me a wildflower and a flask of clean water (Elspeth sells both for a few coins), and show me you have faced a goblin scavenger in the old smugglers' tunnels. Then I will bless a mace for you from the shrine's own store, washed in the basin and left a night beneath the Hand. Say {cmd}accept offering{/} if you will, and {cmd}report{/} when you have the things.");
        AddTopic("teach,teaching,teacher,trainer,train,rank,ranks,priest,priests,skill,skills,lessons,heal spell,spells", "There are fuller works than the ones you have been taught, and I teach them here, to those who have the calling for them: the second working of the heal and of the blessing. The fifth level, and forty gold into the bowl. Say {cmd}ranks{/} and I will show you, and {cmd}train heal{/} or {cmd}train bless{/} when you are ready. What comes after is taught at the Hall of Legends, in Oakenfell.");
        AddTopic("village,hollow,vincent,lane,fletcher,fletchers,neighbours,neighbors", "The Hollow is a noisy village, for all its small size. We have the fletchers' thud, the smith's hammer, the children and the geese. The Hand likes it that way. It is easier to find quiet in a noisy place, where you have to make it, than in a quiet one, where it merely happens.");
        AddTopic("cellar,cellars,goblin,goblins,tunnels,storage,scavengers,scavenger", "The old cellar door used to be our way of sending pilgrims home: a bowl, a candle, a prayer. Now the goblins come through it. I do not hate them. I think they are frightened, and I think something is giving them orders. A frightened thing with orders is the most dangerous thing in the world, and the saddest.");
        AddTopic("rumour,rumor,rumours,rumors,news,gossip,candles,candle,flames,north", "Look at the candles on the north sill. They lean away from the wall, all of them, whichever way the door is open. In forty years I have seen it only when something is stirring below the village: a goblin camp moving, or the Overseer's drums. They leaned the other way, once, the night Vincent vanished. My grandmother kept the shrine then, and she never told me why.");
        AddTopic("spirit,inner,belief,inner strength,calm,stillness", "Spirit is what the Quiet Hand asks of us: a stillness that holds. Those with much of it heal better, and the Focus comes back to them sooner. It cannot be bought; I have tried to give it away, and people only say thank you.");
        AddTopic("focus,concentration,mind,casting,cast", "Focus is the water in the Hand: the stiller you are, the more it holds. Insight sets how deep the basin is, and Spirit how soon it fills again. A blessing fills it at once, but a quiet hour does the same, more slowly.");
        AddTopic("remembered,names,wall,sill names", "Ysolt Brannock is remembered here for her Spirit. She sat the long nights with the sick, one winter, and never once raised her voice. Her name is scratched under the north sill, if you look.");
        AddEmote("Sister Merewyn straightens a candle on the sill and sets her palm flat beside the flame.", 8);
        AddEmote("Sister Merewyn dips two fingers in the basin and touches them to her brow.", 6);

        AddStock(BlessingPath, 6);
        TeachCalling("priest", 2, 2);
        AddRankOffer("glow", 2, 20);
        AddTopic("glow,light,candle,candles,lamp,dark,darkness", "Glow is the Quiet Hand's one small gift to those who walk in the dark: it asks nothing and shows you the way. {cmd}glow{/} for yourself, {cmd}glow NAME{/} for a friend. The second working is twenty gold here, once you are of the fifth level; the rest is taught at the Hall of Legends.");
        ElsewhereLine = "The third rank and beyond are taught in Oakenfell, at the Hall of Legends.";
    }

    /// <summary>The blessing is not an item: buying it restores the buyer at once, as Harland's bed does.</summary>
    protected override bool OnBuy(ObjectRef buyer, string path)
    {
        if (path != BlessingPath) return base.OnBuy(buyer, path);
        buyer.Call("Heal", 9999);
        buyer.Call("FillResources");
        TellObject(buyer, "Sister Merewyn has you kneel at the stone hand. She dips two fingers in the still water, touches them to your brow and murmurs a few words you do not catch. A great quiet settles through you. You rise rested and whole.\n");
        return true;
    }

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Interactive(other)) return;
        AddAction("accept", CmdAccept);
        AddAction("report", CmdReport);
    }

    private void Speak(string line)
    {
        if (Environment() is { } room) TellRoom(room, $"{Name} says: \"{line}\"\n");
    }

    private int Held(ObjectRef player, string path) => CountIn(player, path).Values.Sum();

    private const string Reminder = "Bring me a wildflower and a flask of clean water (Elspeth sells them), and have faced a goblin scavenger in the old smugglers' tunnels. Then {cmd}report{/}.";

    private bool CmdAccept(string args)
    {
        var player = ThisPlayer();
        if (player == null) return false;
        var what = args.Trim().ToLowerInvariant();
        if (what is not ("offering" or "the offering" or "quiet offering" or "the quiet offering" or "mace" or "blessed mace" or "1"))
        {
            Speak("Which errand? The only one I have is the offering: {cmd}accept offering{/}.");
            return true;
        }
        var state = player.Call<int>("QueryQuest", Quest);
        if (state == 2) { Speak($"You have already finished \"{QuestTitle}\". Thank you again."); return true; }
        if (state == 1) { Speak($"You have already taken \"{QuestTitle}\". " + Reminder); return true; }

        player.Call("SetQuest", Quest, 1, QuestTitle);
        Write($"Quest accepted: {QuestTitle}.\n");
        Speak("Thank you. " + Reminder);
        return true;
    }

    private bool CmdReport(string args)
    {
        var player = ThisPlayer();
        if (player == null) return false;
        var state = player.Call<int>("QueryQuest", Quest);
        if (state == 2) { Speak("You have done everything I asked. Thank you. The basin has never been so still."); return true; }
        if (state == 0) { Speak("You have nothing to report yet. If you would help the shrine, {cmd}ask{/} me about the offering."); return true; }

        var missing = new List<string>();
        if (Held(player, FlowerPath) < 1) missing.Add("a wildflower");
        if (Held(player, WaterPath) < 1) missing.Add("a flask of clean water");
        if (!player.Call<bool>("QueryKilled", ScavengerPath)) missing.Add("proof that you have faced a goblin scavenger in the old tunnels");
        if (missing.Count > 0)
        {
            Speak("Not yet. I still need " + string.Join(", and ", missing) + ".");
            return true;
        }

        if (!TakeFrom(player, 1, o => o.ProgramPath == FlowerPath) || !TakeFrom(player, 1, o => o.ProgramPath == WaterPath))
        {
            Speak("Something is wrong with the offering. Bring it again, and I will take it from your hands.");
            return true;
        }
        TellRoom(Environment()!, "Sister Merewyn takes the wildflower and the flask of water, lays the flower in the palm of the stone hand and pours the water into the basin. It ripples once and goes perfectly still.\n");
        Speak("The basin is quiet. Thank you. Take this: the shrine's own mace, washed in that water and left a night beneath the Hand. It will serve you better than a mace that has not been blessed, and, I hope, a little more kindly.");
        MoveObject(CloneObject(MacePath), player);
        player.Call("SetQuest", Quest, 2, QuestTitle);
        Write($"Quest complete: {QuestTitle}!\n");
        player.Call("AddXp", 100);
        player.Call("AddGold", 25);
        Write("Sister Merewyn gives you 25 gold and a shrine-blessed mace.\n");
        try
        {
            LoadObject("/secure/events").Call<string>("Report", "quest.local", player,
                new List<ObjectRef> { player }, Environment(), Quest, new Dictionary<string, string> { ["name"] = QuestTitle });
        }
        catch (MudRuntimeException) { }
        return true;
    }
}
