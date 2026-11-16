/// <summary>
/// Owns a new connection until the visitor has given a name, then hands the
/// connection over to a freshly cloned player body.
/// </summary>
public class Login : MudObject
{
    public void Logon()
    {
        Write("\nWelcome to LesMudOS.\n\nBy what name are you known? ");
        InputTo(GotName);
    }

    private void GotName(string name)
    {
        name = name.Trim();
        if (name.Length < 2 || name.Length > 16 || !name.All(char.IsLetter))
        {
            Write("Names are 2 to 16 letters. Try again: ");
            InputTo(GotName);
            return;
        }
        name = char.ToUpper(name[0]) + name.Substring(1).ToLower();

        if (Users().Any(u => u.Call<string>("QueryName") == name))
        {
            Write($"{name} is already here. Another name? ");
            InputTo(GotName);
            return;
        }

        var player = CloneObject("/std/player");
        Exec(player, Self);
        player.Call("SetupPlayer", name);
        Destruct();
    }

    public override void NetDead() => Destruct();
}
