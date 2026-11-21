// @inherit /std/npc

/// <summary>
/// Guildmaster Rowan: the quest giver. Offers `accept` and `report` to every
/// player in the room. Quest states are per player: 0 not started, 1 active,
/// 2 completed (see Adventurer.QueryQuest / SetQuest).
/// </summary>
public class Rowan : Npc
{
    private const string Cellar = "clear-the-cellar";
    private const string Orders = "overseers-orders";
    private const string OverseerPath = "/domains/hollow/mob/goblin_overseer";
    private const string OrdersPath = "/domains/hollow/obj/overseers_orders";
    private const string HelmPath = "/domains/hollow/obj/guild_helm";
    private const string PassPath = "/domains/hollow/obj/travel_pass";

    public override void Create()
    {
        Name = "Rowan";
        Ids.Add("guildmaster");
        Ids.Add("guildmaster rowan");
        Ids.Add("master");
        ShortDesc = "Guildmaster Rowan";
        LongDesc = "A lean, grey-bearded man with a scar through one eyebrow and the stillness of someone who has stopped being surprised. The guild's sword-and-banner badge is pinned to his coat.";
        Level = 4;
        MaxVitality = 50;

        AddChat("Every village has its legends. Ours is Vincent himself. Some believe he found something beneath these lands before he vanished.");
        AddChat("The board has work for anyone brave enough. Read it, then tell me which contract you want: {cmd}accept cellar{/}, or {cmd}accept orders{/}.");
        AddChat("When your work is done, come back and {cmd}report{/}. The guild pays its debts.");
        AddTopic("quest,quests,work,job,contract,board", "Read the board. The cellar quest is the one the village needs. When you've done it, say {cmd}report{/}.");
        AddTopic("cellar,goblins,goblin,overseer", "Goblins have taken the old smugglers' tunnels beneath the Hollowman's Rest. Lately they've grown organized. There's an Overseer down there, and he's the one you want.");
        AddTopic("vincent,holloway,founder", "Vincent Holloway built this village. He vanished decades ago, looking for something under the earth. Nobody believed him then.");
        AddTopic("crow", "The Crow? I have never heard the name. If you find it written down somewhere, bring it to me.");
        AddEmote("Rowan studies a map pinned to the wall and frowns.", 5);
    }

    /// <summary>
    /// A player who has completed "Clear the Cellar" and carries no travel pass (an existing character, or one who lost it
    /// with a corpse) is given another. True if one was given.
    /// </summary>
    private bool ReissuePass(ObjectRef who)
    {
        if (who.Call<int>("QueryQuest", Cellar) != 2 || Present("travel pass", who) != null) return false;
        MoveObject(CloneObject(PassPath), who);
        Speak("You have no travel pass on you? Then take another, and keep it safe. The guild vouches for you.");
        Write("Rowan gives you a travel pass.\n");
        return true;
    }

    protected override string? TalkOverride(ObjectRef who)
    {
        ReissuePass(who);
        if (who.Call<int>("QueryQuest", Orders) == 2)
            return "The Crow, the Hollow Below, the sealed entrance... We have learned a great deal, and I like none of it. The guild will have more work for you when you are ready.";
        if (who.Call<int>("QueryQuest", Cellar) == 2)
            return "You have cleared the cellar, and the village sleeps easier for it. If you still carry the Overseer's Orders, bring them to me and say {cmd}report{/}.";
        return null;
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

    private static string Title(string quest) => quest == Cellar ? "Clear the Cellar" : "Recover the Overseer's Orders";

    private bool CmdAccept(string args)
    {
        var player = ThisPlayer();
        if (player == null) return false;
        var what = args.Trim().ToLowerInvariant();
        string? quest = null;
        if (what is "1" or "cellar" or "clear" or "clear the cellar") quest = Cellar;
        else if (what is "2" or "orders" or "order" or "recover" or "the orders" or "overseer's orders" or "overseers orders" or "recover the overseer's orders") quest = Orders;
        if (quest == null)
        {
            Speak("Which contract? Read the board, then say {cmd}accept cellar{/}, or {cmd}accept orders{/}.");
            return true;
        }

        var state = player.Call<int>("QueryQuest", quest);
        if (state == 2) { Speak($"You have already finished \"{Title(quest)}\". Thank you again."); return true; }
        if (state == 1) { Speak($"You have already taken \"{Title(quest)}\". " + Reminder(quest)); return true; }

        player.Call("SetQuest", quest, 1, Title(quest));
        Write($"Quest accepted: {Title(quest)}.\n");
        if (quest == Cellar)
            Speak("Good. Go down through the Hollowman's Rest, past the cellar and into the old smugglers' tunnels. The goblins answer to an Overseer. Kill him, and come back and {cmd}report{/}. Whatever you kill leaves a corpse, so {cmd}loot{/} it for gold and spoils.");
        else
            Speak("The goblins take their orders from somewhere. The Overseer will carry them. Bring me what he carries on him, and we will learn who is behind this.");
        return true;
    }

    private string Reminder(string quest)
        => quest == Cellar
            ? "The Goblin Overseer waits at the end of the Forgotten Cellars. Kill him, then {cmd}report{/}."
            : "Find the Overseer's Orders in the Forgotten Cellars, and bring them to me.";

    private bool CmdReport(string args)
    {
        var player = ThisPlayer();
        if (player == null) return false;
        var reported = ReissuePass(player);

        if (player.Call<int>("QueryQuest", Cellar) < 2 && player.Call<bool>("QueryKilled", OverseerPath))
        {
            reported = true;
            FinishCellar(player);
        }
        if (player.Call<int>("QueryQuest", Orders) < 2 && FindOrders(player) is { } orders)
        {
            reported = true;
            FinishOrders(player, orders);
        }

        if (!reported)
        {
            var cellar = player.Call<int>("QueryQuest", Cellar);
            var ordersState = player.Call<int>("QueryQuest", Orders);
            if (cellar == 1) Speak("The Overseer still lives. " + Reminder(Cellar));
            else if (ordersState == 1) Speak(Reminder(Orders));
            else if (cellar == 2 && ordersState == 2) Speak("You have done everything I asked. Rest, and come back when you are stronger.");
            else Speak("You have nothing to report yet. Take a contract from the board first.");
        }
        return true;
    }

    private ObjectRef? FindOrders(ObjectRef player) => AllInventory(player).FirstOrDefault(o => o.ProgramPath == OrdersPath);

    private void Reward(ObjectRef player, string quest, int xp, int gold, string extra)
    {
        player.Call("SetQuest", quest, 2, Title(quest));
        Write($"Quest complete: {Title(quest)}!\n");
        player.Call("AddXp", xp);
        player.Call("AddGold", gold);
        Write($"Rowan pays you {gold} gold{extra}.\n");
        try
        {
            LoadObject("/secure/events").Call<string>("Report", quest == Orders ? "quest.overseers_orders" : "quest.clear_the_cellar", player,
                new List<ObjectRef> { player }, Environment(), quest, new Dictionary<string, string> { ["name"] = Title(quest) });
        }
        catch (MudRuntimeException) { }
    }

    private void FinishCellar(ObjectRef player)
    {
        Speak("The Overseer is dead? Then the cellar is ours again. Well done. The Hollow owes you.");
        var helm = CloneObject(HelmPath);
        MoveObject(helm, player);
        Reward(player, Cellar, 150, 50, " and gives you a Guild Initiate's Helm");
        MoveObject(CloneObject(PassPath), player);
        Speak("And this is for the road: a travel pass, a pass of safe conduct from the guild. With it the way north out of the Hollow is open to you, and the road leads to Oakenfell.");
        Write("Rowan gives you a travel pass.\n");

        if (player.Call<int>("QueryQuest", Orders) == 0)
        {
            player.Call("SetQuest", Orders, 1, Title(Orders));
            Speak("There is more. Whatever drove those goblins left its orders somewhere down there. Find the Overseer's Orders, if the goblins have not burned them, and bring them to me.");
            Write("New quest: Recover the Overseer's Orders.\n");
        }
    }

    private void FinishOrders(ObjectRef player, ObjectRef orders)
    {
        orders.Call("Vanish");
        if (Environment() is { } room)
            TellRoom(room, "Rowan takes the bundle, unfolds the crude notes and squints at the scrawl. \"Most of this is nonsense,\" he says, \"but a few lines are plain enough.\" He reads them aloud:\n" +
                           "  \"The Hollow Below\"\n" +
                           "  \"The Key of Vincent\"\n" +
                           "  \"Continue excavation.\"\n" +
                           "  \"The old founder was telling the truth.\"\n" +
                           "  \"The entrance remains sealed.\"\n" +
                           "  \"The Crow demands progress.\"\n");
        Speak("The old founder was telling the truth. Vincent did find something beneath this village before he vanished.");
        Speak("There is a place under our feet, a whole complex they call \"The Hollow Below\".");
        Speak("The Overseer was taking his orders from someone called \"The Crow\". The goblins were only hands, and the Crow is the head.");
        Speak("I doubt Vincent's vanishing was an accident. I think it is tied to whatever lies down there.");
        Speak("\"The entrance remains sealed,\" it says. Then there may be other entrances too, perhaps in the northern mountains.");
        Speak("But the cellar is clear, and the village is safe tonight. Come back when you are ready for what comes next.");
        Reward(player, Orders, 200, 75, "");
    }
}
