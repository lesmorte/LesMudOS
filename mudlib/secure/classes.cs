/// <summary>
/// The class and skill registry: finds the class files in /classes and the skill and trait files in /skills.
/// There is no list to maintain. A file in the directory is a class (or a skill), and its id is the file name.
/// This blueprint is the only object the master lets list those two directories (ValidOperation "get-dir"), and
/// it only ever lists them. The characters ask it for ids and objects; it keeps no state, so adding or removing a
/// file takes effect on the next question.
/// </summary>
public class ClassRegistry : MudObject
{
    private const string ClassDir = "/classes";
    private const string SkillDir = "/skills";

    private static bool ValidId(string id) => id.Length > 0 && id.Length <= 40 && id.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_');

    private List<string> Ids(string dir)
    {
        var result = new List<string>();
        var entries = GetDir(dir);
        if (entries == null) return result;
        foreach (var entry in entries)
        {
            if (entry.IsDirectory || !entry.Name.EndsWith(".cs")) continue;
            var id = entry.Name.Substring(0, entry.Name.Length - 3);
            if (ValidId(id)) result.Add(id);
        }
        return result;
    }

    /// <summary>The ids of all class files.</summary>
    public List<string> ClassIds() => Ids(ClassDir);

    /// <summary>The ids of all skill, spell and trait files.</summary>
    public List<string> SkillIds() => Ids(SkillDir);

    /// <summary>The class with this id, or null if there is no such file or it does not compile.</summary>
    public ObjectRef? Class(string id) => Find(ClassDir, id);

    /// <summary>The skill, spell or trait with this id, or null.</summary>
    public ObjectRef? Skill(string id) => Find(SkillDir, id);

    private ObjectRef? Find(string dir, string id)
    {
        if (!ValidId(id)) return null;
        try { return LoadObject(dir + "/" + id); }
        catch (MudRuntimeException) { return null; }
    }

    /// <summary>The ids of the enabled classes of one tier, in name order. For the choice at character creation, tier is "archetype".</summary>
    public List<string> EnabledOfTier(string tier)
    {
        var found = new List<(int Order, string Id)>();
        foreach (var id in ClassIds())
        {
            var cls = Class(id);
            if (cls != null && cls.Call<bool>("QueryEnabled") && !cls.Call<bool>("QueryHidden") && cls.Call<string>("QueryTier") == tier) found.Add((cls.Call<int>("QuerySortOrder"), id));
        }
        return found.OrderBy(f => f.Order).ThenBy(f => f.Id, StringComparer.Ordinal).Select(f => f.Id).ToList();
    }

    /// <summary>The ids of the hidden classes (enabled or not), for the register, the lint and the level-10 command. Refuses restricted code.</summary>
    public List<string> HiddenIds()
    {
        var result = new List<string>();
        if (RestrictedCaller() != null) return result;
        foreach (var id in ClassIds())
            if (Class(id)?.Call<bool>("QueryHidden") == true) result.Add(id);
        return result;
    }

    /// <summary>
    /// The enabled classes of one tier whose parent is the given class (an archetype's advanced classes), for the Hall of Legends.
    /// </summary>
    public List<string> EnabledChildren(string parent, string tier)
    {
        var result = new List<string>();
        foreach (var id in EnabledOfTier(tier))
            if (Class(id)?.Call<string>("QueryParent") == parent) result.Add(id);
        return result;
    }
}
