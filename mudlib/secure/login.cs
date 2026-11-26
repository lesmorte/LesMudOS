/// <summary>
/// Owns a new connection until the visitor has given a name and proved (or,
/// for a new account, chosen) the password, then hands the connection over to
/// a player body.
///
/// The body is cloned as soon as the name is known, because only a player
/// body may read player save files: the login object asks the body whether
/// the account exists and whether a password matches, and never sees the
/// hash itself. Until the hand-over the body has no connection; if the login
/// fails or the connection drops, the body is destructed here.
/// </summary>
public class Login : MudObject
{
    private const int MaxPasswordAttempts = 3;
    private const int MinPasswordLength = 8; // for the prompt only; /std/player enforces the rules

    private ObjectRef? body;
    private string name = "";
    private string accountState = "";
    private bool claiming; // choosing a password for a new or password-less account
    private int attempts;

    // The login throttle (docs/specs/login-throttle.md): the connection's true address as the driver counts it (null if it has none), its keyed
    // hash, whether this login runs under the stricter rules for an address the character has used before, and whether an answer is being delayed.
    private string? address;
    private string tag = "";
    private bool knownMode;
    private bool waiting;

    private ObjectRef Throttle() => LoadObject("/secure/throttle");

    public void Logon()
    {
        if (!Interactive(Self)) return; // only a login object the driver gave a connection to
        address = ConnectionAddress();
        Write("\nWelcome to Legends of Hollowmere.\n\nBy what name are you known? ");
        InputTo(GotName);
    }

    /// <summary>
    /// Every answer that says "no" (a wrong password, a refusal) comes after the same short pause, made with a call-out so the game goes on
    /// meanwhile, so that how long an answer took tells nothing about why it was no. What is typed meanwhile is swallowed unseen.
    /// </summary>
    private void Later(Action next)
    {
        var ms = Throttle().Call<int>("QueryDelayMs");
        if (ms <= 0) { next(); return; }
        waiting = true;
        InputTo(Swallow, true);
        CallOut(() => { waiting = false; next(); }, ms / 1000.0);
    }

    private void Swallow(string ignored)
    {
        if (waiting) InputTo(Swallow, true);
    }

    /// <summary>The one refusal for every cause (a locked name, a locked address): the same words whether or not the name exists.</summary>
    private void Refuse()
    {
        Discard();
        var text = Throttle().Call<string>("QueryRefusal") + "\n";
        Later(() => Fail(text));
    }

    /// <summary>
    /// The name this connection is choosing a password for, if any. Lets two
    /// connections see that they are trying to create the same account.
    /// </summary>
    public string? QueryClaim() => claiming ? name : null;

    private bool IsOnline(string who) => Users().Any(u => u.Call<string>("QueryName") == who);

    private void GotName(string input)
    {
        input = input.Trim();
        if (input.Length < 2 || input.Length > 16 || !input.All(char.IsLetter))
        {
            Write("Names are 2 to 16 letters. Try again: ");
            InputTo(GotName);
            return;
        }
        name = char.ToUpper(input[0]) + input.Substring(1).ToLower();

        // A locked address is not turned away at connect: a character's owner who shares it with someone who guessed too often may still try that
        // character (below, "known" mode). Everyone else, whatever the name, hears the same refusal, and learns nothing about the name.
        address = ConnectionAddress();
        var addressLocked = Throttle().Call<bool>("AddressBlocked", address);
        if (addressLocked && IsOnline(name)) { Refuse(); return; }
        if (IsOnline(name))
        {
            Write($"{name} is already here. Another name? ");
            InputTo(GotName);
            return;
        }

        body = CloneObject("/std/player");
        accountState = body.Call<string>("BeginLogin", name) ?? "";
        attempts = 0;
        knownMode = false;
        tag = Throttle().Call<string>("Tag", address) ?? "";
        switch (accountState)
        {
            case "password":
                var verdict = Throttle().Call<string>("BeginPassword", name, address, body.Call<bool>("KnowsAddress", tag));
                if (verdict == "refused") { Refuse(); return; }
                knownMode = verdict == "known";
                Write("Password: ");
                InputTo(GotPassword, true);
                return;
            case "new":
            case "unset":
                if (addressLocked) { Refuse(); return; }
                if (accountState == "new" && !Throttle().Call<bool>("CanCreate", address)) { Fail("Too many new characters have been made from your address. Try again later.\n"); return; }
                if (Users().Any(u => u != Self && u.Call<string>("QueryClaim") == name))
                {
                    Discard();
                    Write($"Someone else is setting up {name} right now. Another name? ");
                    InputTo(GotName);
                    return;
                }
                claiming = true;
                Write(accountState == "new"
                    ? $"Creating a new account for {name}.\n"
                    : $"{name}, your account has no password. You must set one before you can play.\n");
                AskNewPassword();
                return;
            default:
                Fail("Something went wrong. Goodbye.\n");
                return;
        }
    }

    private void GotPassword(string password)
    {
        if (body is not { IsAlive: true }) { Fail("Something went wrong. Goodbye.\n"); return; }
        // Asked again with every password: connections that were already at the prompt when a lock began (guessing in parallel) are refused too.
        var verdict = Throttle().Call<string>("BeginPassword", name, address, body.Call<bool>("KnowsAddress", tag));
        if (verdict == "refused") { Refuse(); return; }
        knownMode = verdict == "known";
        if (body.Call<bool>("CheckPassword", password))
        {
            Throttle().Call("Succeeded", name, address);
            AfterPassword();
            return;
        }
        var mayTryAgain = Throttle().Call<bool>("Failed", name, address, knownMode);
        if (!mayTryAgain && attempts + 1 < MaxPasswordAttempts)
        {
            // The throttle ended this connection before its third try (a lock has just begun, or an address used up its own three). The words say
            // nothing about which; they only tell the character's owner what to do.
            Later(() => Fail("Wrong password. Goodbye.\nToo many wrong passwords on this name for now. If it is your character, wait a few minutes and try again.\n"));
            return;
        }
        if (!mayTryAgain || ++attempts >= MaxPasswordAttempts)
        {
            Later(() => Fail("Wrong password. Goodbye.\n"));
            return;
        }
        Later(() =>
        {
            Write("Wrong password. Password: ");
            InputTo(GotPassword, true);
        });
    }

    private void AskNewPassword()
    {
        Write($"Choose a password (at least {MinPasswordLength} characters): ");
        InputTo(GotNewPassword, true);
    }

    private void GotNewPassword(string password)
    {
        if (body is not { IsAlive: true }) { Fail("Something went wrong. Goodbye.\n"); return; }
        var problem = body.Call<string>("CheckNewPassword", password);
        if (problem != null)
        {
            Write(problem + "\n");
            AskNewPassword();
            return;
        }
        Write("Type it again: ");
        InputTo(again => GotPasswordAgain(password, again), true);
    }

    private void GotPasswordAgain(string password, string again)
    {
        if (body is not { IsAlive: true }) { Fail("Something went wrong. Goodbye.\n"); return; }
        if (again != password)
        {
            Write("The passwords do not match.\n");
            AskNewPassword();
            return;
        }
        if (accountState == "new" && !Throttle().Call<bool>("CanCreate", address))
        {
            Fail("Too many new characters have been made from your address. Try again later.\n");
            return;
        }
        if (!body.Call<bool>("SetPassword", password))
        {
            Write("That password cannot be used.\n");
            AskNewPassword();
            return;
        }
        if (accountState == "new") Throttle().Call("Created", address);
        Write(accountState == "new" ? "Account created.\n" : "Password set.\n");
        AfterPassword();
    }

    // ---- The choice of a calling (docs/CLASS_SYSTEM.md section 7) ----
    // A character with no archetype, new or saved before classes existed, chooses one after the password and before
    // entering the game. The list comes from the class files; the body decides what the choice gives.

    private void AfterPassword()
    {
        if (body is not { IsAlive: true }) { Fail("Something went wrong. Goodbye.\n"); return; }
        if (!body.Call<bool>("NeedsArchetype"))
        {
            EnterGame();
            return;
        }
        var text = new StringBuilder("\nBefore you set out, choose your calling. Each is a way of facing the dangers of Hollowmere:\n\n");
        foreach (var id in Callings())
            if (Registry().Call<ObjectRef>("Class", id) is { } cls)
                text.Append($"  {cls.Call<string>("QueryClassName")}: {cls.Call<string>("QueryTagline")}\n");
        text.Append("\nType the name of a calling to choose it, or help and a name (for example: help mage) to read more.\nYour calling: ");
        Write(text.ToString());
        InputTo(GotCalling);
    }

    private ObjectRef Registry() => LoadObject("/secure/classes");

    private List<string> Callings() => Registry().Call<List<string>>("EnabledOfTier", "archetype") ?? new List<string>();

    private ObjectRef? FindCalling(string word)
    {
        word = word.Trim().ToLowerInvariant();
        foreach (var id in Callings())
            if (Registry().Call<ObjectRef>("Class", id) is { } cls
                && (id == word || (cls.Call<string>("QueryClassName") ?? "").ToLowerInvariant() == word)) return cls;
        return null;
    }

    private void GotCalling(string input)
    {
        if (body is not { IsAlive: true }) { Fail("Something went wrong. Goodbye.\n"); return; }
        input = input.Trim();
        if (input.StartsWith("help", StringComparison.OrdinalIgnoreCase))
        {
            var cls = FindCalling(input.Substring(4));
            Write(cls == null ? "Say help and the name of a calling.\nYour calling: " : $"{cls.Call<string>("QueryClassName")}: {cls.Call<string>("QueryDescription")}\nYour calling: ");
            InputTo(GotCalling);
            return;
        }
        var chosen = FindCalling(input);
        if (chosen == null)
        {
            Write("There is no such calling. Your calling: ");
            InputTo(GotCalling);
            return;
        }
        var problem = body.Call<string>("ChooseArchetype", chosen.Call<string>("QueryId"));
        if (problem != null)
        {
            Write(problem + " Your calling: ");
            InputTo(GotCalling);
            return;
        }
        Write($"You take up the calling of the {chosen.Call<string>("QueryClassName")}.\n");
        EnterGame();
    }

    private void EnterGame()
    {
        claiming = false;
        // Checked again: the same player may have logged in from elsewhere meanwhile.
        if (IsOnline(name))
        {
            Discard();
            Write($"{name} is already here. Another name? ");
            InputTo(GotName);
            return;
        }
        // A dropped body of this name is still in the world: the connection goes to it, so that one name never has two bodies.
        if (LoadObject("/secure/linkdead").Call<ObjectRef>("Find", name) is { } lingering)
        {
            Discard();
            if (tag != "") lingering.Call("NoteAddress", tag);
            Exec(lingering, Self);
            lingering.Call("Reconnected");
            Destruct();
            return;
        }
        var player = body!;
        body = null;
        if (tag != "") player.Call("NoteAddress", tag);
        Exec(player, Self);
        player.Call("SetupPlayer", name);
        Destruct();
    }

    /// <summary>Destruct the body that was never handed the connection.</summary>
    private void Discard()
    {
        if (body is { IsAlive: true }) Destruct(body);
        body = null;
        claiming = false;
    }

    private void Fail(string message)
    {
        Write(message);
        Discard();
        Destruct(); // closes the connection
    }

    public override void NetDead()
    {
        Discard();
        Destruct();
    }
}
