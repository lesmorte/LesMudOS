/// <summary>
/// The wizard registry: the single source of truth for who holds which wizard
/// level. Levels run from 0 (an ordinary player, no wizard commands) to 10.
///
/// All the rules about who may change a level are enforced here, not in the
/// player object. The caller is identified with PreviousObject(), so another
/// mudlib object cannot pass itself off as a wizard by handing in a name.
/// </summary>
public class WizList : MudObject
{
    public const int MaxLevel = 10;
    private const string SaveFile = "/secure/wizlist";

    /// <summary>Lower-cased player name to level. Level-0 players are not listed.</summary>
    [Saved] private Dictionary<string, int> levels = new();

    public override void Create()
    {
        if (!Self.IsClone) RestoreObject(SaveFile);
    }

    public int QueryLevel(string name) => levels.GetValueOrDefault(name.ToLower());

    public bool HasTopWizard() => levels.Values.Any(level => level >= MaxLevel);

    /// <summary>"name (level)" for every wizard, highest level first.</summary>
    public List<string> QueryWizards()
        => levels.OrderByDescending(e => e.Value).ThenBy(e => e.Key).Select(e => $"{e.Key} ({e.Value})").ToList();

    /// <summary>
    /// The name of the player issuing the current command, but only if that
    /// player's own body is what called us. Null for anything else.
    /// </summary>
    private string? CallingPlayer()
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller != ThisPlayer()) return null;
        if (!caller.IsClone || caller.ProgramPath != "/std/player" || caller.Domain != "") return null;
        if (RestrictedCaller() != null) return null; // a wizard's sandbox code acting through a body (docs/SANDBOX.md)
        return caller.Call<string>("QueryName")?.ToLower();
    }

    /// <summary>
    /// Set another player's level. A wizard may set any level from 0 up to
    /// their own, and only for players currently below their own level.
    /// Returns null on success, otherwise the reason for refusing.
    /// </summary>
    public string? Promote(string target, int level)
    {
        var actor = CallingPlayer();
        if (actor == null) return "Only a player can do that.";
        var mine = QueryLevel(actor);
        if (mine < 1) return "You are not a wizard.";

        target = target.Trim().ToLower();
        if (target == actor) return "You cannot change your own level.";
        if (level < 0 || level > mine) return $"You can set levels from 0 to {mine}.";
        if (QueryLevel(target) >= mine) return "You can only change players below your own level.";

        var online = Users().FirstOrDefault(u => u.Call<string>("QueryName")?.ToLower() == target);
        if (online == null && !levels.ContainsKey(target)) return "No player of that name is online.";

        if (level == 0) levels.Remove(target);
        else levels[target] = level;
        SaveObject(SaveFile);

        if (online != null) TellObject(online, $"Your wizard level is now {level}.\n");
        return null;
    }

    /// <summary>
    /// Make the calling player a level-10 wizard, if the admin secret matches
    /// and there is no level-10 wizard yet. Returns true on success. The two
    /// reasons for failure are deliberately not distinguished.
    /// </summary>
    public bool Bootstrap(string secret)
    {
        var actor = CallingPlayer();
        if (actor == null || HasTopWizard()) return false;
        if (!CheckAdminSecret(secret)) return false;

        levels[actor] = MaxLevel;
        SaveObject(SaveFile);
        return true;
    }
}
