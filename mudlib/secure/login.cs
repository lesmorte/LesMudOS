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

    public void Logon()
    {
        if (!Interactive(Self)) return; // only a login object the driver gave a connection to
        Write("\nWelcome to LesMudOS.\n\nBy what name are you known? ");
        InputTo(GotName);
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

        if (IsOnline(name))
        {
            Write($"{name} is already here. Another name? ");
            InputTo(GotName);
            return;
        }

        body = CloneObject("/std/player");
        accountState = body.Call<string>("BeginLogin", name) ?? "";
        attempts = 0;
        switch (accountState)
        {
            case "password":
                Write("Password: ");
                InputTo(GotPassword, true);
                return;
            case "new":
            case "unset":
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
        if (body.Call<bool>("CheckPassword", password))
        {
            EnterGame();
            return;
        }
        if (++attempts >= MaxPasswordAttempts)
        {
            Fail("Wrong password. Goodbye.\n");
            return;
        }
        Write("Wrong password. Password: ");
        InputTo(GotPassword, true);
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
        if (!body.Call<bool>("SetPassword", password))
        {
            Write("That password cannot be used.\n");
            AskNewPassword();
            return;
        }
        Write(accountState == "new" ? "Account created.\n" : "Password set.\n");
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
        var player = body!;
        body = null;
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
