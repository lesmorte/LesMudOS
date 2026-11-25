// @inherit /std/adventurer

/// <summary>
/// The player body. Its own commands are ordinary AddAction bindings, added
/// once at login. Everything else a player can do is offered by the room and
/// the objects around them.
/// </summary>
public class Player : Adventurer
{
    // Where a new character, or one who did not quit in a logout room, enters the game.
    private const string StartRoom = "/domains/hollow/village/square";
    private const string WizList = "/secure/wizlist";

    // Wizard level needed for each wizard command. Level 0 has none of them.
    private const int LevelFiles = 1; // pwd, cd, list, cat; and every changing command inside the wizard's own home
    private const int LevelEdit = 5; // mkdir, edit, copy, move, rename, delete, rmdir, update outside the wizard's own home (and in other wizards' homes)
    private const int LevelSystemFiles = 10; // the same under /secure and /std, and for /wiz itself and the home directories in it
    private const string WizRoot = "/wiz"; // every wizard has a home directory /wiz/<name>
    private const int MaxEditLines = 5000;
    private const int LevelMudstat = 1;
    private const int LevelClone = 3;
    private const int LevelUpdate = 5;
    private const int LevelShutdown = 10;

    private const int MinPasswordLength = 8;
    private const int MaxPasswordChecks = 3;
    private const string LoginProgram = "/secure/login";

    [Saved] private string title = "the newcomer";
    [Saved] private int logins;
    [Saved] private long lastLogin;
    [Saved] private string cwd = "/"; // the mudlib directory a wizard is "in"
    // Made by the HashPassword efun. Empty for accounts saved before passwords
    // existed. No command may ever show it.
    [Saved] private string passwordHash = "";
    // ANSI colour. On by default, also for characters saved before this setting existed.
    [Saved] private bool colour = true;
    // The player's own time zone for `time` (an IANA name, "UTC" or "UTC-5"); "" until set. Only this player ever sees it.
    [Saved] private string timeZone = "";
    // `charset ascii`: every message to this player is turned into 7-bit ASCII (see Asciify). Off (UTF-8) by default, also for older saves.
    [Saved] private bool asciiOnly;

    private bool ready;
    // True only while WriteRaw is sending file text: CatchTell then passes it on exactly as stored.
    private bool rawOutput;

    // Login hand-shake with /secure/login, used only until the player is ready.
    private ObjectRef? loginObject; // the login object that called BeginLogin
    private string accountState = ""; // "new", "password" or "unset" after BeginLogin
    private bool authenticated;
    private bool returning; // a save file was restored
    private int failedChecks;

    // Editor session; edLines is null when the player is not in the editor.
    private List<string>? edLines;
    private string edPath = "";
    private bool edDirty;
    private int edInsertAt = -1; // >= 0 while typed lines are being inserted at that index

    // Counts every message sent to this player. delete compares it before and
    // after its question, so a "y" typed after anything else was shown (which
    // may have been another object's question) does not delete.
    private int messagesShown;

    public override void CatchTell(string message)
    {
        messagesShown++;
        base.CatchTell(asciiOnly ? Asciify(rawOutput ? message : Render(message)) : rawOutput ? message : Render(message));
    }

    // ---- Colour ----
    // Every message to a player passes CatchTell, which turns the {tags} of
    // the colour markup into ANSI sequences, or removes them with colour off.
    // See "Colour and command highlighting" in docs/BUILDING.md.

    /// <summary>The message as this player's connection should get it.</summary>
    private string Render(string text) => Markup(text, colour);

    /// <summary>
    /// Send text exactly as it is, with no rendering: for the contents of
    /// files, which a builder needs to see as stored.
    /// </summary>
    private void WriteRaw(string text)
    {
        rawOutput = true;
        try { Write(text); }
        finally { rawOutput = false; }
    }

    private string SaveFile => "/players/" + Name.ToLower();

    /// <summary>Record the carried items and the logout room, then write the save file.</summary>
    protected override void PersistNow() => Save();

    /// <summary>Called on every online player by the shutdown command, so that a planned stop loses nothing.</summary>
    public void SaveForShutdown()
    {
        if (ready && PreviousObject() is { ProgramPath: "/std/player" }) Save();
    }

    /// <summary>
    /// Called by another player's body right after it gave this player something (the give command), so that both save
    /// files agree. Only a player body acting for its own command may ask.
    /// </summary>
    public void SaveAfterTrade()
    {
        var caller = PreviousObject();
        if (ready && caller is { IsClone: true, ProgramPath: "/std/player" } && caller != Self && caller == ThisPlayer()) Save();
    }

    /// <summary>
    /// Called by a find right after it paid this character (gold, and perhaps an item), so that the character reaches disk
    /// straight after the register of finds did. The register is written first on purpose: a driver killed between the two
    /// writes then loses the reward of one character rather than letting it be taken twice. Saving is harmless, so any object
    /// may ask, but only from the character's own command and from the room the character stands in.
    /// </summary>
    public void SaveAfterFind()
    {
        var caller = PreviousObject();
        if (ready && caller != null && Self == ThisPlayer() && Environment(caller) == Environment()) Save();
    }

    private void Save()
    {
        PrepareSave();
        SaveObject(SaveFile);
    }

    public override string? Short() => $"{Name} {Esc(title)}";

    public override string Long() => $"{Name} {Esc(title)}, who has visited {logins} time(s).\n" + HealthLine() + (linkDead ? $"{Name} stares at nothing, and does not answer.\n" : "");

    // ---- Login ----
    // These public methods decide who gets into an account, so each one checks
    // that it is called by the login object that started this login (or, for
    // BeginLogin, by a connected login object), and each one does nothing once
    // the player is set up.

    private static bool IsLoginObject(ObjectRef? ob) => ob != null && ob.IsClone && ob.ProgramPath == LoginProgram;

    private bool CalledByOurLogin() => !ready && loginObject != null && loginObject.IsAlive && PreviousObject() == loginObject;

    /// <summary>
    /// Step 1, called by /secure/login on a freshly cloned body: take the name
    /// and restore its save file. Returns "new" (no save file), "password"
    /// (the account has a password) or "unset" (an account saved before
    /// passwords existed). Returns "" if the call is refused. Works once.
    /// </summary>
    public string BeginLogin(string name)
    {
        var caller = PreviousObject();
        if (ready || loginObject != null || !IsLoginObject(caller) || !Interactive(caller!)) return "";
        if (name.Length < 2 || name.Length > 16 || !name.All(char.IsLetter)) return "";
        loginObject = caller;
        Name = name;
        returning = RestoreObject(SaveFile);
        accountState = !returning ? "new" : string.IsNullOrEmpty(passwordHash) ? "unset" : "password";
        return accountState;
    }

    /// <summary>
    /// Step 2 for an account with a password. True if it matches. After
    /// three wrong passwords every later check fails.
    /// </summary>
    public bool CheckPassword(string password)
    {
        if (!CalledByOurLogin() || accountState != "password" || authenticated || failedChecks >= MaxPasswordChecks) return false;
        if (VerifyPassword(password, passwordHash))
        {
            authenticated = true;
            return true;
        }
        failedChecks++;
        return false;
    }

    /// <summary>
    /// Step 2 for a new account or one without a password: store the password
    /// and save at once, so a new name is reserved with its password. False if
    /// refused, including when the password breaks the rules in CheckNewPassword.
    /// </summary>
    public bool SetPassword(string password)
    {
        if (!CalledByOurLogin() || authenticated || accountState is not ("new" or "unset")) return false;
        if (CheckNewPassword(password) != null) return false;
        passwordHash = HashPassword(password);
        authenticated = true;
        Save();
        return true;
    }

    /// <summary>Why a password may not be used for this player, or null if it may. Reveals nothing about the account.</summary>
    public string? CheckNewPassword(string password)
    {
        if (password.Length < MinPasswordLength) return $"A password needs at least {MinPasswordLength} characters.";
        if (string.Equals(password, Name, StringComparison.OrdinalIgnoreCase)) return "Your password cannot be your name.";
        return null;
    }

    /// <summary>
    /// Step 3, called by /secure/login once the connection has been handed
    /// over to this body: enter the game. Needs a successful step 2.
    /// </summary>
    public void SetupPlayer(string name)
    {
        if (ready || !authenticated || !CalledByOurLogin() || name != Name || !Interactive(Self)) return;
        ready = true;
        loginObject = null;

        logins++;
        lastLogin = Time();

        EnableCommands();
        BindChannelVerbs(); // first, so that no channel name can take over a command the game already has
        BeginAdventure();
        AddAction("say", CmdSay);
        AddAction("'", CmdSay);
        AddAction("tell", a => ToComm("DoTell", a));
        AddAction("reply", a => ToComm("DoReply", a));
        AddAction("whisper", a => ToComm("DoWhisper", a));
        AddAction("shout", CmdShout);
        AddAction("ignore", CmdIgnore);
        AddAction("unignore", CmdUnignore);
        AddAction("channels", a => ToChannels("Listing", ""));
        AddAction("channel", a => ToChannels("Switch", a));
        AddAction("who", CmdWho);
        AddAction("title", CmdTitle);
        AddAction("renown", a => { Write(LoadObject("/secure/renown").Call<string>("RenownText", Name) ?? ""); return true; });
        AddAction("date", CmdTime);
        AddAction("time", CmdTime); // the same as date
        AddAction("password", CmdPassword);
        AddAction("save", CmdSave);
        AddAction("quit", CmdQuit);
        AddAction("help", CmdHelp);
        AddAction("charset", CmdCharset);
        AddAction("ascii", CmdCharset);
        AddAction("ansi", CmdAnsi);
        AddAction("color", CmdAnsi);
        AddAction("colour", CmdAnsi);
        AddAction("sync", CmdSync);
        // Wizard commands. Each handler checks the caller's level on every use
        // and declines below it, so to a non-wizard these verbs do not exist.
        AddAction("promote", CmdPromote);
        AddAction("wizards", CmdWizards);
        AddAction("pwd", CmdPwd);
        AddAction("cd", CmdCd);
        AddAction("list", CmdList);
        AddAction("ls", CmdList);
        AddAction("cat", CmdCat);
        AddAction("mkdir", CmdMkdir);
        AddAction("edit", CmdEdit);
        AddAction("copy", CmdCopy);
        AddAction("move", CmdMove);
        AddAction("rename", CmdRename);
        AddAction("delete", CmdDelete);
        AddAction("rmdir", CmdRmdir);
        AddAction("update", CmdUpdate);
        AddAction("clone", CmdClone);
        AddAction("mudstat", CmdMudstat);
        AddAction("shutdown", CmdShutdown);

        Write(returning ? $"\nWelcome back, {Name}. This is visit number {logins}.\n\n" : $"\nWelcome, {Name}. Type '{{cmd}}help{{/}}' for commands. If the boxes and bullets of {{cmd}}score{{/}} look wrong, type {{cmd}}charset ascii{{/}}.\n\n");
        EnsureHome(); // a wizard whose home is missing gets it back
        EnterWorld(StartRoom);
        ClaimLostProperty();
        Say($"{Name} enters the game.\n");
        Look();
    }

    /// <summary>This player's wizard level, 0 to 10, as held by the wizard registry.</summary>
    public override int QueryWizLevel() => LoadObject(WizList).Call<int>("QueryLevel", Name);

    // ---- Player commands ----

    private bool CmdSay(string args) => ToComm("DoSay", args);

    /// <summary>A client aid: "sync TOKEN" answers with one line "[sync TOKEN]" (letters and digits only, 16 at most), so a client can tell where the reply to its earlier commands ends. It takes no game time.</summary>
    private bool CmdSync(string args)
    {
        var token = new string(args.Where(char.IsLetterOrDigit).Take(16).ToArray());
        Write($"[sync {token}]\n");
        return true;
    }

    // ---- Idle and timeout (server settings: see docs/DEPLOY.md) ----

    private bool idleWarned;

    /// <summary>True once the player has typed nothing for the server's idle time. Empty lines and telnet keep-alives are not typing.</summary>
    public override bool QueryIdle() => IdleSeconds(Self) >= IdleLimitSeconds();

    public int QueryIdleSeconds() => IdleSeconds(Self);

    /// <summary>How long before the disconnect the warning comes: two minutes, or half the timeout if that is shorter.</summary>
    private static int WarningLead(int timeout) => Math.Min(120, timeout / 2);

    protected override void IdleCheck()
    {
        var idle = IdleSeconds(Self);
        var timeout = TimeoutSeconds();
        var warnAt = timeout - WarningLead(timeout);
        if (idle < warnAt) { idleWarned = false; return; }
        if (QueryWizLevel() > 0) return; // wizards stay connected
        if (idle >= timeout)
        {
            Write("You have been idle for too long. Your character is saved and you are disconnected.\n");
            if (!DropConnection()) CmdQuit(""); // the connection ends like a dropped one: NetDead follows (the body lingers a minute)
            return;
        }
        if (idleWarned) return;
        idleWarned = true;
        var left = timeout - idle;
        Write($"You have been idle for a while. Type something in the next {(left >= 90 ? $"{(left + 30) / 60} minutes" : $"{left} seconds")} or you will be saved and disconnected.\n");
    }

    private static string IdleSuffix(int seconds) => seconds >= 120 ? $" (idle {seconds / 60}m)" : $" (idle {seconds}s)";

    private bool CmdWho(string args)
    {
        var users = Users()
            .Where(u => u.Call<string>("Short") != null)
            .Select(u => u.Call<string>("Short") + (u.Call<int>("QueryWizLevel") > 0 ? $" [wizard {u.Call<int>("QueryWizLevel")}]" : "") + (u.Call<bool>("QueryIdle") ? IdleSuffix(u.Call<int>("QueryIdleSeconds")) : ""))
            .ToList();
        foreach (var dropped in LoadObject("/secure/linkdead").Call<List<ObjectRef>>("All") ?? new List<ObjectRef>()) users.Add(dropped.Call<string>("Short") + " (link-dead)");
        Write($"{users.Count} player(s) online:\n  " + string.Join("\n  ", users) + "\n");
        return true;
    }

    private bool CmdTitle(string args)
    {
        if (args == "" || args.Length > 40) { NotifyFail("Usage: {cmd}title{/} <up to 40 characters>"); return false; }
        title = args;
        Write($"You are now {Short()}.\n");
        return true;
    }

    /// <summary>
    /// "password": change the password. Asks for the current one, then the new
    /// one twice, with echo off. Nothing typed here is ever written back.
    /// </summary>
    private bool CmdPassword(string args)
    {
        Write("Current password: ");
        InputTo(PasswordCurrent, true);
        return true;
    }

    private void PasswordCurrent(string current)
    {
        if (!VerifyPassword(current, passwordHash))
        {
            Write("Wrong password. Your password is unchanged.\n");
            return;
        }
        Write($"New password (at least {MinPasswordLength} characters): ");
        InputTo(PasswordNew, true);
    }

    private void PasswordNew(string password)
    {
        var problem = CheckNewPassword(password);
        if (problem != null)
        {
            Write(problem + " Your password is unchanged.\n");
            return;
        }
        Write("Type it again: ");
        InputTo(again => PasswordConfirm(password, again), true);
    }

    private void PasswordConfirm(string password, string again)
    {
        if (again != password)
        {
            Write("The passwords do not match. Your password is unchanged.\n");
            return;
        }
        passwordHash = HashPassword(password);
        Save();
        Write("Password changed.\n");
    }

    private bool CmdSave(string args)
    {
        Save();
        Write("Saved.\n");
        return true;
    }

    private bool CmdQuit(string args)
    {
        // Quitting is not an escape: a fight is left by fleeing (the body of a dropped connection stays for a minute, so quit must not be cheaper).
        if (QueryInCombat()) { Write("You cannot leave in the middle of a fight. Flee first.\n"); return true; }
        Save();
        Write("Goodbye.\n");
        Say($"{Name} leaves the game.\n");
        ReleaseInventory(); // the carried items are saved as records (path and state) and come back at the next login
        Destruct(); // the connection closes
        return true;
    }

    private bool CmdHelp(string args)
    {
        // Crafting and gathering topics ("help crafting", "help skin"): answered by the crafting daemon.
        if (args.Trim() != "" && CraftHelp(args) is { } craftTopic) { Write(craftTopic); return true; }
        if (args.Trim().ToLowerInvariant() is "ranks" or "rank" or "train" or "training")
        {
            Write("Ranks: every skill and spell with a command word has ranks, I, II, III and on. A higher rank is stronger and costs a\n" +
                  "little more; the command word stays the same. Rank II needs level 5, rank III level 10, IV level 15, V level 20.\n" +
                  "A rank is bought with gold from a trainer of your calling: in Vincent's Hollow for rank II (the loft, the shrine, the\n" +
                  "Strung Yew and the Practice Yard), at the Hall of Legends in Oakenfell for rank III and above. At a trainer,\n" +
                  "{cmd}ranks{/} shows what you can learn, the price and anything else a rank asks (Arcane Bolt II asks for ten goblins slain);\n" +
                  "{cmd}train{/} SKILL buys the next rank. Ranks cannot be skipped. {cmd}skills{/} shows your rank in each and the next rank's level.\n");
            return true;
        }
        Write(
            "Commands: {cmd}look{/} [at THING], {cmd}say{/} TEXT, {cmd}get{/} [N|all] THING, {cmd}drop{/} [N|all] THING, {cmd}inventory{/} ({cmd}i{/}),\n" +
            "          {cmd}who{/} (idle players are marked), {cmd}title{/} TEXT, {cmd}password{/}, {cmd}save{/}, {cmd}quit{/}, {cmd}ansi{/} [on|off], {cmd}time{/} [-timezone [ZONE|REGION|none]] (your own time zone), {cmd}charset{/} [ascii|utf8] (or {cmd}ascii{/} [on|off]: plain 7-bit characters if the boxes of {cmd}score{/} look wrong), {cmd}verbose{/} [on|off], {cmd}prompt{/} [TEXT|default|off], {cmd}sync{/} TOKEN (a client aid: answers [sync TOKEN]), and the exits of the room you are in.\n" +
            "          Objects nearby add their own verbs (try the lantern and the cat).\n" +
            "Fighting:  {cmd}kill{/} (or {cmd}attack{/}) TARGET, {cmd}flee{/} [DIRECTION], {cmd}consider{/} TARGET, {cmd}vitality{/}, {cmd}score{/} ({cmd}sc{/}), {cmd}improve{/} [ATTRIBUTE] (place attribute points),\n" +
            "           {cmd}skills{/} (your skills, spells and traits; each skill has its own command word),\n" +
            "           {cmd}stop{/} (cancel a potion, meal or skill you queued while recovering)\n" +
            "           A bow in your hands gives you a head start when you choose the fight: the target's first action waits two seconds more (never when a creature attacks you first).\n" +
            "           A Scout's {cmd}aim{/} (Aimed Shot, bow only) hits hardest as the shot that starts a fight (never more than a share of the target's full Vitality, unless you are three levels above it), and is spoiled if a blow lands while you aim. A Priest cannot cast {cmd}heal{/} again for 15 seconds after one, and what it heals in the Priest who casts it is a third of what it gives another.\n" +
            "Sneaking:  a Scout's {cmd}sneak{/} [on|off] (turn it on before you enter a room): each room costs Endurance, and the creatures there may not notice you. Attacking, any skill but Tracking, picking things up, opening a door or chest, {cmd}say{/} and {cmd}shout{/} end it and wake the room;\n" +
            "           {cmd}look{/}, {cmd}rest{/}, {cmd}tell{/} and {cmd}track{/} do not. A creature that has seen you keeps you from sneaking until it dies or you leave. The {cmd}prompt{/} token for it is sneak.\n" +
            "Resting:   {cmd}rest{/} (or {cmd}sit{/}) sits you down: Vitality, Endurance and Focus return five times as fast, until you {cmd}stand{/}, move, fight, pick up, drop, wear, buy, sell or craft;\n" +
            "           a creature that finds you resting gets the first blow in (you lose two seconds more). Not while fighting. The {cmd}prompt{/} token for it is rest.\n" +
            "Names:     where a command takes a name you can say which one: {cmd}rat 2{/}, {cmd}2.rat{/}, {cmd}second rat{/}\n" +
            "           (while fighting you cannot get, loot or walk away: {cmd}flee{/})\n" +
            "Corpses:   {cmd}loot{/} [CORPSE], {cmd}get{/} THING (or all) from CORPSE, {cmd}look in{/} CORPSE\n" +
            "Gear:      {cmd}wield{/} THING, {cmd}unwield{/}, {cmd}wear{/} THING, {cmd}remove{/} THING, {cmd}eat{/}/{cmd}drink{/} THING\n" +
            "Speaking:  {cmd}say{/} TEXT (or {cmd}'{/} TEXT), {cmd}tell{/} PLAYER TEXT, {cmd}reply{/} TEXT, {cmd}whisper{/} TEXT {cmd}to{/} PLAYER, {cmd}shout{/} TEXT\n" +
            "           {cmd}shout off{/} / {cmd}shout on{/} (stop or hear shouts), {cmd}ignore{/} [PLAYER], {cmd}unignore{/} PLAYER,\n" +
            "           {cmd}channels{/} (your channels), {cmd}channel{/} NAME {cmd}on{/}|{cmd}off{/}, {cmd}shout{/} NAME TEXT or NAME TEXT to speak on one\n" +
            "People:    {cmd}talk{/} (or {cmd}greet{/}) NPC, {cmd}ask{/} NPC {cmd}about{/} TOPIC, {cmd}read{/} THING,\n" +
            "           {cmd}give{/} [N|all] THING {cmd}to{/} PLAYER, {cmd}search{/} (for hidden ways)\n" +
            "Crafting:  {cmd}skin{/}, {cmd}bleed{/}, {cmd}harvest{/}, {cmd}craft{/}, {cmd}combine{/}, {cmd}recipes{/}, {cmd}recipe{/}, {cmd}proficiencies{/},\n" +
            "           {cmd}apply{/} THING; with a trainer: {cmd}lessons{/}, {cmd}learn{/} NAME ({cmd}help crafting{/}, or help with one of those words)\n" +
            "Ranks:     with a trainer of your calling: {cmd}ranks{/} (what you can learn and what it asks), {cmd}train{/} SKILL (buy the next rank; {cmd}help ranks{/})\n" +
            "Town:      {cmd}browse{/}, {cmd}buy{/} THING, {cmd}sell{/} THING, {cmd}value{/} THING (in a shop), {cmd}quests{/},\n" +
            "           {cmd}accept{/} QUEST and {cmd}report{/} (in the Adventurers Guild)\n" +
            "Colour:    command names like these are highlighted; {cmd}ansi off{/} turns colour off, {cmd}ansi on{/} turns it on.\n");

        var level = QueryWizLevel();
        if (level < 1) return true;
        Write($"Wizard commands at your level ({level}):\n" +
              "          {cmd}promote{/} <player> <level>   set a lower wizard's level, up to your own\n" +
              "          {cmd}wizards{/}                    list all wizards\n" +
              "          {cmd}pwd{/}                        show the mudlib directory you are in\n" +
              "          {cmd}cd{/} [<dir>]                 change directory (.. goes up, no argument goes to your home)\n" +
              "          {cmd}list{/} [<dir>]               list a directory (also: ls)\n" +
              "          {cmd}cat{/} <file>                 show a file with line numbers\n" +
              "          {cmd}goto{/} <room path or player> move yourself to a room or to a player\n" +
              "          {cmd}stat{/} <thing|me|here>       show the numbers of a creature, item or room\n" +
              $"          Your home directory is {{cmd}}{HomePath}{{/}}; ~ in a path means it. In your home you may use every file\n" +
              "          command below, plus update and clone, at any wizard level.\n");
        if (level >= LevelMudstat) Write("          {cmd}mudstat{/}                    driver statistics\n");
        Write("          {cmd}clone{/} <path>               clone an object into your inventory or room (level 3 outside your home)\n" +
              "          {cmd}update{/} <path>              recompile a file and replace the loaded object (level 5 outside your home)\n" +
              "          {cmd}mkdir{/} <dir>                create a directory\n" +
              "          {cmd}edit{/} <file>                create or change a file in the line editor\n" +
              "          {cmd}copy{/} <from> <to>           copy a file (into a directory: same name)\n" +
              "          {cmd}move{/} <from> <to>           move a file to a new name or directory\n" +
              "          {cmd}rename{/} <file> <newname>    rename a file within its directory\n" +
              "          {cmd}delete{/} <file>              delete a file, after you confirm (cannot be undone)\n" +
              "          {cmd}rmdir{/} <dir>                remove an empty directory\n" +
              "                                     (those seven need level 5 outside your home, and in other wizards' homes;\n" +
              "                                     level 10 for /secure, /std, /wiz itself and the home directories)\n");
        if (level >= 5) Write("          {cmd}restore{/} [<target>]         restore a creature (or yourself) to full Vitality, Endurance and Focus\n");
        if (level >= 5) Write("          {cmd}speed{/} <target> <percent> [<seconds>]  haste (positive) or slow (negative) a creature's Reaction Time; 0 clears\n");
        if (level >= 10) Write("          {cmd}slay{/} <target>              kill a creature at once, with kill credit\n");
        if (level >= 10) Write("          {cmd}setlevel{/} <1-100>           set your level (experience, Vitality and resources follow)\n");
        if (level >= LevelUpdate) Write("          {cmd}update{/} <path>              recompile a file and replace the loaded object\n");
        if (level >= LevelShutdown) Write("          {cmd}shutdown{/}                   stop the driver\n");
        return true;
    }

    /// <summary>"ansi" shows the colour setting; "ansi on" and "ansi off" change it. Also answers to "color" and "colour".</summary>
    private bool CmdAnsi(string args)
    {
        var word = args.Trim().ToLowerInvariant();
        if (word == "")
        {
            Write(colour ? "ANSI colour is on. Type {cmd}ansi off{/} to turn it off.\n" : "ANSI colour is off. Type {cmd}ansi on{/} to turn it on.\n");
            return true;
        }
        if (word != "on" && word != "off") { NotifyFail("Usage: {cmd}ansi{/} [{cmd}on{/}|{cmd}off{/}]"); return false; }
        colour = word == "on";
        Write(colour ? "ANSI colour is now on. Commands you can type, like {cmd}help{/}, are highlighted.\n" : "ANSI colour is now off.\n");
        return true;
    }

    /// <summary>`time` and `date`: the time in the player's zone. `time -timezone` lists zones, `time -timezone ZONE` sets one (see /secure/clock).</summary>
    private bool CmdTime(string args)
    {
        var word = args.Trim();
        var lower = word.ToLowerInvariant();
        var clock = LoadObject("/secure/clock");
        string rest;
        if (lower is "-timezone" or "-tz" or "timezone" or "zones" or "-zones" or "tz") rest = "";
        else if (lower.StartsWith("-timezone ") || lower.StartsWith("-tz ") || lower.StartsWith("timezone ") || lower.StartsWith("zones ") || lower.StartsWith("tz ")) rest = word.Substring(word.IndexOf(' ') + 1).Trim();
        else if (word == "") { Write(clock.Call<string>("Show", timeZone, LoadObject("/secure/chronicle").Call<string>("DateCommand") ?? "") ?? ""); return true; }
        else { NotifyFail("Usage: {cmd}time{/} [{cmd}-timezone{/} [ZONE|REGION|none]]"); return false; }
        if (rest == "") { Write(clock.Call<string>("Zones") ?? ""); return true; }
        var region = clock.Call<string>("Region", rest) ?? "";
        if (region != "") { Write(region); return true; }
        var answer = clock.Call<string>("Resolve", rest) ?? "MSG:Nothing comes of it.\n";
        if (answer.StartsWith("MSG:")) { Write(answer.Substring(4)); return true; }
        timeZone = answer.StartsWith("NONE:") ? "" : answer.Substring(5);
        Write(timeZone == "" ? "Your time zone is cleared: {cmd}time{/} shows UTC again.\n" : "Your time zone is set.\n");
        Write(clock.Call<string>("Show", timeZone, "") ?? "");
        return true;
    }

    /// <summary>"charset" shows the setting; "charset ascii" and "charset utf8" change it; "ascii on" and "ascii off" do the same.</summary>
    private bool CmdCharset(string args)
    {
        var word = args.Trim().ToLowerInvariant();
        var alias = QueryVerb() == "ascii";
        if (word == "")
        {
            Write(asciiOnly ? "Characters: plain ASCII (boxes drawn with + - |). Type {cmd}charset utf8{/} to use the full character set.\n"
                : "Characters: UTF-8 (boxes, bullets and dashes). If they look like garbage in your client, type {cmd}charset ascii{/}.\n");
            return true;
        }
        bool? want = alias ? (word == "on" ? true : word == "off" ? false : null)
            : word is "ascii" ? true : word is "utf8" or "utf-8" or "unicode" ? false : null;
        if (want == null) { NotifyFail(alias ? "Usage: {cmd}ascii{/} [{cmd}on{/}|{cmd}off{/}]" : "Usage: {cmd}charset{/} [{cmd}ascii{/}|{cmd}utf8{/}]"); return false; }
        asciiOnly = want.Value;
        Write(asciiOnly ? "Characters: plain ASCII from now on. Boxes are drawn with + - | and everything you see is 7-bit.\n"
            : "Characters: UTF-8 from now on.\n");
        return true;
    }

    // ---- Wizard commands ----

    /// <summary>
    /// "promote <player> <level>" sets another player's wizard level; the
    /// registry enforces who may set what. "promote <secret>" claims level 10
    /// with the driver's admin secret, which only works while no level-10
    /// wizard exists. To anyone who is not a wizard and does not know the
    /// secret, the command behaves as if it did not exist.
    /// </summary>
    private bool CmdPromote(string args)
    {
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var registry = LoadObject(WizList);
        var level = 0;
        var isPromotion = words.Length == 2 && int.TryParse(words[1], out level);

        // Anything that is not "<player> <number>" is treated as the secret, spaces and all.
        if (!isPromotion && args != "" && registry.Call<bool>("Bootstrap", args))
        {
            Write("The secret is accepted. You are now a level 10 wizard. Type '{cmd}help{/}'.\n");
            EnsureHome();
            return true;
        }
        if (!IsWizard(1)) return false;
        if (!isPromotion)
        {
            NotifyFail("Usage: {cmd}promote{/} <player> <level>");
            return false;
        }
        var refusal = registry.Call<string>("Promote", words[0], level);
        Write(refusal == null ? $"{Esc(words[0])} is now wizard level {level}.\n" : Esc(refusal) + "\n");
        if (refusal == null && level >= LevelFiles)
        {
            // The new wizard's own body makes the home, from its own name.
            var promoted = Users().FirstOrDefault(u => string.Equals(u.Call<string>("QueryName"), words[0], StringComparison.OrdinalIgnoreCase));
            promoted?.Call("PrepareHome");
        }
        return true;
    }

    /// <summary>
    /// Turn what a wizard typed into an absolute mudlib path: relative paths
    /// start from the current directory, and "." and ".." are resolved here so
    /// that the driver only ever sees a clean absolute path.
    /// </summary>
    protected override string ResolvePath(string path)
    {
        var parts = new List<string>();
        var start = path.StartsWith("/") ? "" : cwd;
        // "~" or "~/..." at the start is this player's own home, never anyone else's ("~bob" is just an odd relative name).
        if (path == "~" || path.StartsWith("~/")) { start = ""; path = HomePath + path.Substring(1); }
        foreach (var part in (start + "/" + path).Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".") continue;
            if (part == "..") { if (parts.Count > 0) parts.RemoveAt(parts.Count - 1); continue; }
            parts.Add(part);
        }
        return "/" + string.Join("/", parts);
    }

    private bool CmdPwd(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        Write(Esc(cwd) + "\n");
        return true;
    }

    private bool CmdCd(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        var target = ResolvePath(args == "" ? "~" : args);
        if (GetDir(target) == null) { Write($"{Esc(target)}: no such directory.\n"); return true; }
        cwd = target;
        Write(Esc(cwd) + "\n");
        return true;
    }

    private bool CmdList(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        var target = ResolvePath(args);
        var entries = GetDir(target);
        if (entries == null) { Write($"{Esc(target)}: no such directory.\n"); return true; }

        var text = new StringBuilder($"{Esc(target)}:\n");
        foreach (var entry in entries)
            text.Append(entry.IsDirectory ? $"  {Esc(entry.Name)}/\n" : $"  {Esc(entry.Name),-28} {entry.Size,8}\n");
        if (entries.Count == 0) text.Append("  (empty)\n");
        Write(text.ToString());
        return true;
    }

    // ---- Wizard commands: files ----

    /// <summary>A file name as typed, as an absolute mudlib path. ".cs" is assumed if no extension is given.</summary>
    private string ResolveFile(string name)
    {
        var path = ResolvePath(name);
        var last = path.Substring(path.LastIndexOf('/') + 1);
        return last == "" || last.Contains('.') ? path : path + ".cs";
    }

    // ---- Wizard homes and the one rule about changing files ----

    /// <summary>This player's home directory, /wiz/&lt;name&gt;. The name is the player's own, in lower case; it is never taken from typed text.</summary>
    private string HomePath => WizRoot + "/" + Name.ToLowerInvariant();

    /// <summary>
    /// Make sure /wiz and this wizard's home exist; tell the wizard in one
    /// line when the home is created. Does nothing below wizard level 1.
    /// The player body makes the directory because the master lets only
    /// player bodies call MakeDir.
    /// </summary>
    private void EnsureHome()
    {
        if (!ready || QueryWizLevel() < LevelFiles) return;
        try
        {
            MakeDir(WizRoot);
            if (MakeDir(HomePath))
                TellObject(Self, $"Your wizard home directory, {{cmd}}{Esc(HomePath)}{{/}}, has been created. Type {{cmd}}cd ~{{/}} to go there; ~ in any path means your home.\n");
        }
        catch (MudRuntimeException) { /* the home is retried at the next login or promotion */ }
    }

    /// <summary>
    /// Called by another player's body (the promoter's) after this player was
    /// made a wizard: create the home from this body's own name. Harmless to
    /// call at any time: it only ever makes this player's own home.
    /// </summary>
    public void PrepareHome()
    {
        var caller = PreviousObject();
        if (caller == null || !caller.IsClone || caller.ProgramPath != "/std/player") return;
        EnsureHome();
    }

    /// <summary>
    /// THE rule: why this wizard may not change this path, or null if they may.
    /// Every command that creates, writes, copies to, moves, renames or removes
    /// a file or directory asks here, for each path it touches, and the editor
    /// and delete ask again when they save or confirm.
    ///
    ///   /secure, /std                      level 10
    ///   /wiz, /wiz/&lt;x&gt; (a home itself, or a file directly in /wiz)   level 10 (own home: mkdir only)
    ///   /wiz/&lt;me&gt;/...                      any wizard (level 1 up)
    ///   /wiz/&lt;other&gt;/...                   level 5
    ///   anywhere else                      level 5
    ///
    /// Names are compared whole-segment and without case (on a case-insensitive
    /// file system /WIZ/Bob is /wiz/bob), so bob does not reach bobby's home.
    /// Paths come from ResolvePath, which has already removed "." and "..".
    /// </summary>
    private string? ChangeRefusal(string path, bool makeDir = false)
    {
        var level = QueryWizLevel();
        if (level < LevelFiles) return "You are not a wizard.";
        var raw = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var segs = raw.Select(x => x.Trim()).ToArray();
        if (segs.Any(x => x == "." || x == "..")) return "Illegal path.";
        bool Is(int i, string name) => segs.Length > i && string.Equals(segs[i], name, StringComparison.OrdinalIgnoreCase);

        // The channel files decide who belongs to which channel, so they are as protected as the registry itself.
        if (Is(0, "channels"))
            return level >= LevelSystemFiles ? null : $"You need wizard level {LevelSystemFiles} to change /channels.";

        if (Is(0, "secure") || Is(0, "std"))
            return level >= LevelSystemFiles ? null : $"You need wizard level {LevelSystemFiles} to change /secure and /std.";

        if (Is(0, "wiz"))
        {
            if (level >= LevelSystemFiles) return null;
            if (segs.Length <= 2)
            {
                // The one thing allowed here: creating your own home (it normally exists already). Exact name, not trimmed.
                if (makeDir && segs.Length == 2 && string.Equals(raw[1], Name, StringComparison.OrdinalIgnoreCase)) return null;
                return $"You need wizard level {LevelSystemFiles} to change /wiz itself, a home directory (/wiz/<name>) or a file directly in /wiz. Everything under your own home, {Esc(HomePath)}, is yours.";
            }
            if (Is(1, Name)) return null;
            if (level >= LevelEdit) return null;
            return $"/wiz/{Esc(segs[1])} is another wizard's home. Below wizard level {LevelEdit} you may read it ({{cmd}}list{{/}}, {{cmd}}cd{{/}}, {{cmd}}cat{{/}}) but not change it.";
        }

        return level >= LevelEdit ? null : $"You need wizard level {LevelEdit} to change anything outside your own home directory, {Esc(HomePath)}.";
    }

    /// <summary>True if the path is a file or directory inside this wizard's own home (not the home directory itself).</summary>
    private bool InOwnHome(string path)
    {
        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        return segs.Length >= 3 && segs.All(x => x != "." && x != "..")
            && string.Equals(segs[0], "wiz", StringComparison.OrdinalIgnoreCase) && string.Equals(segs[1], Name, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> SplitLines(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[lines.Count - 1] == "") lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    private static string Numbered(List<string> lines, int first, int last)
    {
        var text = new StringBuilder();
        for (var i = first; i <= last; i++) text.Append($"{i,4}  {lines[i - 1]}\n");
        return text.ToString();
    }

    private bool CmdCat(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        if (args == "") { NotifyFail("Usage: {cmd}cat{/} <file>"); return false; }
        var file = ResolveFile(args);
        var text = ReadFile(file);
        if (text == null) { Write($"{Esc(file)}: no such file.\n"); return true; }
        var lines = SplitLines(text);
        WriteRaw($"{file} ({lines.Count} lines):\n" + Numbered(lines, 1, lines.Count));
        return true;
    }

    private bool CmdMkdir(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        if (args == "") { NotifyFail("Usage: {cmd}mkdir{/} <dir>"); return false; }
        var target = ResolvePath(args);
        if (ChangeRefusal(target, makeDir: true) is { } refused) { Write(refused + "\n"); return true; }
        Write(MakeDir(target) ? $"Created {Esc(target)}/\n" : $"{Esc(target)} already exists.\n");
        return true;
    }

    private bool CmdCopy(string args) => TransferFile(args, false);

    private bool CmdMove(string args) => TransferFile(args, true);

    /// <summary>
    /// "copy <from> <to>" and "move <from> <to>". If the destination is an
    /// existing directory the file keeps its name inside it. The driver never
    /// overwrites a file. Copying needs the level to write the destination;
    /// moving changes both places, so it needs the level to write both.
    /// </summary>
    private bool TransferFile(string args, bool move)
    {
        if (!IsWizard(LevelFiles)) return false;
        var verb = move ? "move" : "copy";
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2) { NotifyFail($"Usage: {{cmd}}{verb}{{/}} <from> <to>"); return false; }
        try
        {
            var from = ResolveFile(words[0]);
            var to = ResolvePath(words[1]);
            to = GetDir(to) != null ? (to == "/" ? "" : to) + from.Substring(from.LastIndexOf('/')) : ResolveFile(words[1]);
            var refused = (move ? ChangeRefusal(from) : null) ?? ChangeRefusal(to);
            if (refused != null) { Write(refused + "\n"); return true; }
            var done = move ? RenameFile(from, to) : CopyFile(from, to);
            Write(done ? $"{(move ? "Moved" : "Copied")} {Esc(from)} to {Esc(to)}.\n" : $"{Esc(from)}: no such file.\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(Esc(refusal.Message) + "\n");
        }
        return true;
    }

    /// <summary>
    /// "rename <file> <newname>": a new name in the same directory. ".cs" is
    /// assumed for a new name with no extension, as for every file name.
    /// </summary>
    private bool CmdRename(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2) { NotifyFail("Usage: {cmd}rename{/} <file> <newname>"); return false; }
        if (words[1].Contains('/')) { Write("The new name cannot contain '/'. Use {cmd}move{/} to change directory.\n"); return true; }
        try
        {
            var from = ResolveFile(words[0]);
            // Built directly rather than with ResolvePath, so ".." cannot turn
            // into a parent directory; the driver refuses dot names itself.
            var to = from.Substring(0, from.LastIndexOf('/') + 1) + (words[1].Contains('.') ? words[1] : words[1] + ".cs");
            if ((ChangeRefusal(from) ?? ChangeRefusal(to)) is { } refused) { Write(refused + "\n"); return true; }
            Write(RenameFile(from, to) ? $"Renamed {Esc(from)} to {Esc(to)}.\n" : $"{Esc(from)}: no such file.\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(Esc(refusal.Message) + "\n");
        }
        return true;
    }

    /// <summary>
    /// "delete <file>": delete one file. It cannot be undone, so the wizard
    /// must answer y or yes first; anything else keeps the file. Directories
    /// are removed with rmdir instead.
    /// </summary>
    private bool CmdDelete(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        args = args.Trim();
        if (args == "") { NotifyFail("Usage: {cmd}delete{/} <file>"); return false; }
        try
        {
            var file = ResolveFile(args);
            if (ChangeRefusal(file) is { } refused) { Write(refused + "\n"); return true; }
            if (!IsFile(file))
            {
                // "delete sub" looks for sub.cs, but the wizard may have meant the directory sub.
                var directory = GetDir(file) != null ? file : GetDir(ResolvePath(args)) != null ? ResolvePath(args) : null;
                Write(directory != null ? $"{Esc(directory)} is a directory; use {{cmd}}rmdir{{/}} to remove an empty directory.\n" : $"{Esc(file)}: no such file.\n");
                return true;
            }
            Write($"Delete {Esc(file)}? This cannot be undone. ({{cmd}}y{{/}}/{{cmd}}n{{/}}) ");
            var asked = messagesShown;
            InputTo(answer => DeleteConfirmed(file, answer, asked));
        }
        catch (MudRuntimeException refusal)
        {
            Write(Esc(refusal.Message) + "\n");
        }
        return true;
    }

    private void DeleteConfirmed(string file, string answer, int asked)
    {
        var reply = answer.Trim().ToLowerInvariant();
        if (reply != "y" && reply != "yes") { Write("Not deleted.\n"); return; }
        // Checked again here: the level may have changed while the question was open.
        if (ChangeRefusal(file) != null) { Write("You may no longer delete that file. Not deleted.\n"); return; }
        if (messagesShown != asked) { Write($"Other messages arrived after the question, so {Esc(file)} was not deleted. Type {{cmd}}delete{{/}} again.\n"); return; }
        try
        {
            if (!RemoveFile(file)) { Write($"{Esc(file)}: no such file.\n"); return; }
            Write($"Deleted {Esc(file)}.\n");
            var objectPath = file.EndsWith(".cs") ? file.Substring(0, file.Length - 3) : null;
            if (objectPath != null && FindObject(objectPath) != null)
                Write($"{Esc(objectPath)} is still loaded and will stay until it is destructed or the driver restarts.\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(Esc(refusal.Message) + "\n");
        }
    }

    /// <summary>
    /// True if the path names a file. Looked up in the directory listing, so
    /// files too large for ReadFile are found as well.
    /// </summary>
    private bool IsFile(string path)
    {
        var slash = path.LastIndexOf('/');
        var name = path.Substring(slash + 1);
        var entries = GetDir(slash == 0 ? "/" : path.Substring(0, slash));
        return name != "" && entries != null && entries.Any(e => !e.IsDirectory && e.Name == name);
    }

    /// <summary>
    /// "rmdir <dir>": remove one empty directory. The driver refuses a
    /// directory with anything in it, hidden files included, and is never
    /// recursive.
    /// </summary>
    private bool CmdRmdir(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        args = args.Trim();
        if (args == "") { NotifyFail("Usage: {cmd}rmdir{/} <dir>"); return false; }
        var target = ResolvePath(args);
        if (ChangeRefusal(target) is { } refused) { Write(refused + "\n"); return true; }
        // The root is left to the driver, which refuses it with its own message.
        if (target == cwd && target != "/") { Write("You are in that directory.\n"); return true; }
        if (target != "/" && cwd.StartsWith(target + "/")) { Write("You are inside that directory.\n"); return true; }
        try
        {
            Write(RemoveDir(target) ? $"Removed {Esc(target)}/\n" : $"{Esc(target)}: no such directory.\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(Esc(refusal.Message) + "\n");
        }
        return true;
    }

    /// <summary>
    /// A small line editor. While it is active every line the player types
    /// goes to EdInput instead of the command parser.
    /// </summary>
    private bool CmdEdit(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        if (args == "") { NotifyFail("Usage: {cmd}edit{/} <file>"); return false; }
        var file = ResolveFile(args);
        if (ChangeRefusal(file) is { } refused) { Write(refused + "\n"); return true; }

        var text = ReadFile(file);
        var slash = file.LastIndexOf('/');
        if (text == null && GetDir(slash == 0 ? "/" : file.Substring(0, slash)) == null)
        {
            Write($"{Esc(file)}: no such directory. Create it with {{cmd}}mkdir{{/}} first.\n");
            return true;
        }

        edLines = text == null ? new List<string>() : SplitLines(text);
        edPath = file;
        edDirty = false;
        edInsertAt = -1;
        Write(text == null ? $"New file {Esc(file)}.\n" : $"Editing {Esc(file)}, {edLines.Count} lines.\n");
        Write("You are in the editor. {cmd}h{/} for help, {cmd}a{/} to add text, {cmd}x{/} to save and leave.\n");
        EdPrompt();
        return true;
    }

    private const string EdHelp =
        "Editor commands (n and m are line numbers):\n" +
        "  {cmd}p{/} [n [m]]   print the file, one line, or lines n to m\n" +
        "  {cmd}a{/} [n]       add lines after line n (default: at the end)\n" +
        "  {cmd}i{/} [n]       insert lines before line n (default: at the top)\n" +
        "  {cmd}c{/} n [m]     replace line n, or lines n to m, with new lines\n" +
        "  {cmd}d{/} n [m]     delete line n, or lines n to m\n" +
        "  {cmd}w{/}           write the file\n" +
        "  {cmd}x{/}           write the file and leave\n" +
        "  {cmd}q{/}           leave (q! to discard unsaved changes)\n" +
        "While adding lines, everything you type or paste goes into the file.\n" +
        "A line containing only a full stop ({cmd}.{/}) ends the input.\n";

    private void EdPrompt()
    {
        if (edInsertAt < 0) Write(":");
        InputTo(EdInput);
    }

    private void EdInput(string line)
    {
        if (edLines == null) return;

        if (edInsertAt >= 0)
        {
            if (line.Trim() == ".") edInsertAt = -1;
            else if (edLines.Count >= MaxEditLines) { Write($"The editor holds at most {MaxEditLines} lines.\n"); edInsertAt = -1; }
            else { edLines.Insert(edInsertAt++, line.TrimEnd()); edDirty = true; }
            EdPrompt();
            return;
        }

        var words = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = words.Length == 0 ? "" : words[0];
        var count = edLines.Count;
        var first = 0;
        var last = 0;
        var hasFirst = words.Length > 1 && int.TryParse(words[1], out first);
        if (!(words.Length > 2 && int.TryParse(words[2], out last))) last = first;
        var validRange = hasFirst && first >= 1 && last >= first && last <= count;
        const string BadRange = "No such line. {cmd}p{/} shows the line numbers.\n";

        switch (command)
        {
            case "":
                break;
            case "h":
                Write(EdHelp);
                break;
            case "p":
                // File text is shown as stored, with no rendering of its tags.
                if (!hasFirst) WriteRaw(count == 0 ? "(empty)\n" : Numbered(edLines, 1, count));
                else if (validRange) WriteRaw(Numbered(edLines, first, last));
                else Write(BadRange);
                break;
            case "a":
                if (!hasFirst) first = count;
                if (first < 0 || first > count) { Write(BadRange); break; }
                edInsertAt = first;
                Write("Adding lines. End with a line containing only {cmd}.{/}\n");
                break;
            case "i":
                if (!hasFirst) first = 1;
                if (first < 1 || first > count + 1) { Write(BadRange); break; }
                edInsertAt = first - 1;
                Write("Inserting lines. End with a line containing only {cmd}.{/}\n");
                break;
            case "c":
                if (!validRange) { Write(BadRange); break; }
                edLines.RemoveRange(first - 1, last - first + 1);
                edDirty = true;
                edInsertAt = first - 1;
                Write("Replacing. End with a line containing only {cmd}.{/}\n");
                break;
            case "d":
                if (!validRange) { Write(BadRange); break; }
                edLines.RemoveRange(first - 1, last - first + 1);
                edDirty = true;
                Write($"Deleted {last - first + 1} line(s).\n");
                break;
            case "w":
                EdSave();
                break;
            case "x":
                if (EdSave()) { EdQuit(); return; }
                break;
            case "q":
                if (!edDirty) { EdQuit(); return; }
                Write("There are unsaved changes: {cmd}w{/} to save, {cmd}q!{/} to discard them.\n");
                break;
            case "q!":
                EdQuit();
                return;
            default:
                Write("Unknown editor command. {cmd}h{/} for help.\n");
                break;
        }
        EdPrompt();
    }

    private bool EdSave()
    {
        if (edLines == null) return false;
        // Checked again here: the level may have changed since the editor was opened.
        if (ChangeRefusal(edPath) is { } refused) { Write("You may no longer write that file. " + refused + "\n"); return false; }
        try
        {
            WriteFile(edPath, string.Join("\n", edLines) + "\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(Esc(refusal.Message) + "\n");
            return false;
        }
        edDirty = false;
        Write($"Wrote {Esc(edPath)}, {edLines.Count} lines.\n");
        if (edPath.EndsWith(".cs")) Write($"Load it with: {{cmd}}update {Esc(edPath.Substring(0, edPath.Length - 3))}{{/}}\n");
        return true;
    }

    private void EdQuit()
    {
        edLines = null;
        edInsertAt = -1;
        Write("Left the editor.\n");
    }

    private bool CmdWizards(string args)
    {
        if (!IsWizard(1)) return false;
        var wizards = LoadObject(WizList).Call<List<string>>("QueryWizards") ?? new List<string>();
        Write("Wizards: " + (wizards.Count == 0 ? "none" : Esc(string.Join(", ", wizards))) + "\n");
        return true;
    }

    /// <summary>
    /// The LPmud "update": recompile a source file. If its blueprint object is
    /// loaded, move the living things out, destruct the old object together
    /// with its other contents, load the new one (whose Create() restocks it),
    /// and move the living things back in.
    /// </summary>
    private bool CmdUpdate(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        if (args == "") { NotifyFail("Usage: {cmd}update{/} <path>"); return false; }

        args = ResolvePath(args); // relative to the current directory
        if (!IsWizard(LevelUpdate) && !InOwnHome(args)) { Write($"You need wizard level {LevelUpdate} to update anything outside your own home directory, {Esc(HomePath)}.\n"); return true; }
        var old = FindObject(args);
        var stale = UpdateProgram(args); // throws with compiler errors; nothing has been touched yet
        Write($"Recompiled {Esc(args)}; {stale.Count} program(s) invalidated: {Esc(string.Join(", ", stale))}\n");
        if (old == null) return true;

        // Only the people go through the Void; creatures and objects are rebuilt with the room.
        var occupants = AllInventory(old).Where(Interactive).ToList();
        var limbo = LoadObject("/std/void");
        foreach (var ob in occupants) MoveObject(ob, limbo);
        Destruct(old);

        var fresh = LoadObject(args);
        foreach (var ob in occupants)
            if (ob.IsAlive) MoveObject(ob, fresh);
        Write($"Replaced {Esc(fresh.Name)}; moved {occupants.Count} occupant(s) back in.\n");
        return true;
    }

    private bool CmdClone(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        if (args == "") { NotifyFail("Usage: {cmd}clone{/} <path>"); return false; }
        var path = ResolvePath(args);
        if (!IsWizard(LevelClone) && !InOwnHome(path)) { Write($"You need wizard level {LevelClone} to clone anything outside your own home directory, {Esc(HomePath)}.\n"); return true; }
        var ob = CloneObject(path);
        var room = Environment();
        MoveObject(ob, ob.Call<bool>("CanGet") || room == null ? Self : room);
        Write($"Cloned {Esc(ob.Name)}.\n");
        return true;
    }

    private bool CmdMudstat(string args)
    {
        if (!IsWizard(LevelMudstat)) return false;
        Write(DriverStatus());
        return true;
    }

    private bool CmdShutdown(string args)
    {
        if (!IsWizard(LevelShutdown)) return false;
        foreach (var user in Users())
        {
            TellObject(user, $"{Name} shuts the game down.\n");
            user.Call("SaveForShutdown");
        }
        foreach (var dropped in LoadObject("/secure/linkdead").Call<List<ObjectRef>>("All") ?? new List<ObjectRef>()) dropped.Call("SaveForShutdown"); // bodies of dropped connections too
        Shutdown();
        return true;
    }

    public override void NetDead()
    {
        if (!ready) return;
        if (QueryLingerSeconds() > 0) { StartLinger(); return; }
        Save();
        Say($"{Name} disconnects.\n");
        ReleaseInventory();
        Destruct();
    }

    // ---- A dropped connection: the body lingers (docs/specs/play-feel.md section 14) ----
    // Without `quit`, the body stays where it stands for LingerSeconds() (a minute), link-dead: creatures fight it, it fights back with ordinary
    // attacks only, and it can be killed. The character is saved at the drop and again when the body goes or dies. /secure/linkdead lists the
    // bodies, so that the login object can give a returning connection back to the same one.

    private int lingerCall;

    /// <summary>How long a body of this player would linger if its connection ended now: 0 for a wizard, a body already lingering, a dead one or one not yet in the game. Read by the driver for the log.</summary>
    public int QueryLingerSeconds() => ready && !linkDead && !Dead && QueryWizLevel() < 1 ? LingerSeconds() : 0;

    private void StartLinger()
    {
        Save(); // a driver stopped in the minute loses nothing
        linkDead = true;
        LeaveRest();
        EndSneakLoud();
        AbandonActions();
        LoadObject("/secure/linkdead").Call("Register", Self);
        if (Environment() is { } room) TellRoom(room, $"{Name}'s eyes go blank.\n", Self);
        lingerCall = CallOut(EndLinger, LingerSeconds());
    }

    private void EndLinger()
    {
        if (linkDead && Self.IsAlive) RemoveDroppedBody();
    }

    protected override void LinkDeadDied() => RemoveDroppedBody();

    private void RemoveDroppedBody()
    {
        linkDead = false;
        Save();
        if (Environment() is { } room) TellRoom(room, $"{Name} fades from sight.\n", Self);
        LoadObject("/secure/linkdead").Call("Unregister", Self);
        ReleaseInventory();
        Destruct();
    }

    /// <summary>The login object gives a returning player's connection to the body they left standing. Only the login object, and only to a body that lingers.</summary>
    public void Reconnected()
    {
        var caller = PreviousObject();
        if (!linkDead || caller == null || !caller.IsClone || caller.ProgramPath != "/secure/login" || !Interactive(Self)) return;
        linkDead = false;
        RemoveCallOut(lingerCall);
        LoadObject("/secure/linkdead").Call("Unregister", Self);
        Write("\nYou shake off the daze and find yourself where you stood.\n\n");
        if (Environment() is { } room) TellRoom(room, $"{Name}'s eyes regain their focus.\n", Self);
        Look();
    }

    // ---- Communication (docs/specs/communication.md) ----
    // A self-contained block. The commands are run by /secure/comm (say, tell, reply, whisper, shout) and /secure/channels
    // (channels); this body binds the verbs and keeps what the character chose: who is ignored, whether shouts are heard, which
    // channels are switched off or joined. Only those two daemons may read or change it, which they do for the player whose
    // command is running. Which channels a character belongs to is not kept here: the channel service decides it from the
    // wizard level at every message.

    private const string CommDaemon = "/secure/comm";
    private const string ChannelDaemon = "/secure/channels";

    /// <summary>Lower-case names of the characters this one ignores.</summary>
    [Saved] private List<string> ignored = new();
    [Saved] private bool shoutOn = true;
    [Saved] private List<string> channelsOff = new();
    [Saved] private List<string> channelsJoined = new();
    private string lastTeller = "";

    private static bool IsCommDaemon(ObjectRef? ob) => ob != null && !ob.IsClone && (ob.ProgramPath == CommDaemon || ob.ProgramPath == ChannelDaemon);

    private bool FromCommDaemon() => IsCommDaemon(PreviousObject());

    public bool QueryIgnores(string name) => FromCommDaemon() && ignored.Contains(name);

    public bool QueryShoutOn() => FromCommDaemon() && shoutOn;

    public bool QueryChannelOff(string id) => FromCommDaemon() && channelsOff.Contains(id);

    public bool QueryChannelJoined(string id) => FromCommDaemon() && channelsJoined.Contains(id);

    public string QueryLastTeller() => FromCommDaemon() ? lastTeller : "";

    public void SetLastTeller(string name)
    {
        if (FromCommDaemon()) lastTeller = name;
    }

    public void SetShoutOn(bool on)
    {
        if (!FromCommDaemon()) return;
        shoutOn = on;
        Save();
    }

    public void SetChannelOff(string id, bool off)
    {
        if (!FromCommDaemon()) return;
        channelsOff.Remove(id);
        if (off) channelsOff.Add(id);
        Save();
    }

    public void SetChannelJoined(string id, bool joined)
    {
        if (!FromCommDaemon()) return;
        channelsJoined.Remove(id);
        if (joined) channelsJoined.Add(id);
        Save();
    }

    private bool ToComm(string function, string args)
    {
        var text = LoadObject(CommDaemon).Call<string>(function, args);
        if (!string.IsNullOrEmpty(text)) Write(text);
        else if (function is "DoSay" or "DoShout") EndSneakLoud(); // a voice raised in the room (the daemon answers "" when it was spoken)
        return true;
    }

    private bool ToChannels(string function, string args)
    {
        var text = LoadObject(ChannelDaemon).Call<string>(function, args);
        if (!string.IsNullOrEmpty(text)) Write(text);
        return true;
    }

    /// <summary>The name of every channel is a command, for those who belong to it (the others get "What?", as for any unknown word).</summary>
    private void BindChannelVerbs()
    {
        foreach (var id in LoadObject(ChannelDaemon).Call<List<string>>("QueryVerbs") ?? new List<string>())
        {
            var verb = id;
            AddAction(verb, a => SpeakOnChannel(verb, a));
        }
    }

    private bool SpeakOnChannel(string channel, string text)
    {
        var reply = LoadObject(ChannelDaemon).Call<string>("Send", channel, text);
        if (reply == null) return false; // not a member, or no such channel
        if (reply != "") Write(reply);
        return true;
    }

    /// <summary>"shout text"; "shout channel text" if the first word is a channel the character belongs to; "shout off" and "shout on".</summary>
    private bool CmdShout(string args)
    {
        args = args.Trim();
        var space = args.IndexOf(' ');
        var first = space < 0 ? args : args.Substring(0, space);
        if (first != "" && LoadObject(ChannelDaemon).Call<bool>("IsMember", first))
            return SpeakOnChannel(first, space < 0 ? "" : args.Substring(space + 1));
        return ToComm("DoShout", args);
    }

    private bool CmdIgnore(string args)
    {
        var name = args.Trim().ToLowerInvariant();
        if (name == "")
        {
            Write(ignored.Count == 0 ? "You are not ignoring anyone.\n" : "You are ignoring: " + string.Join(", ", ignored.Select(Cap)) + "\n");
            return true;
        }
        if (name.Length < 2 || name.Length > 16 || !name.All(char.IsLetter)) { Write("Usage: {cmd}ignore{/} [PLAYER]\n"); return true; }
        if (name == Name.ToLowerInvariant()) { Write("You cannot ignore yourself.\n"); return true; }
        if (ignored.Contains(name)) { Write($"You are already ignoring {Cap(name)}.\n"); return true; }
        if (ignored.Count >= (LoadObject(CommDaemon).Call<int>("QueryMaxIgnored"))) { Write("Your ignore list is full.\n"); return true; }
        ignored.Add(name);
        Save();
        Write($"You are now ignoring {Cap(name)}: their tells and whispers are not delivered and you do not hear their shouts or channel messages.\n");
        return true;
    }

    private bool CmdUnignore(string args)
    {
        var name = args.Trim().ToLowerInvariant();
        if (name == "" || name.Contains(' ')) { Write("Usage: {cmd}unignore{/} PLAYER\n"); return true; }
        if (!ignored.Remove(name)) { Write($"You are not ignoring {Esc(Cap(name))}.\n"); return true; }
        Save();
        Write($"You no longer ignore {Esc(Cap(name))}.\n");
        return true;
    }
}
