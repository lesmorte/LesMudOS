/// <summary>
/// The register of dropped players' bodies that are still in the world (docs/specs/play-feel.md section 14). A body puts itself here when its
/// connection ends and takes itself out when it goes; the login object asks it for a body to hand a returning player's connection to (so that
/// the same name never has two bodies), and `who` and the talking commands ask it who is link-dead. It holds nothing that outlives the driver.
/// </summary>
public class LinkDead : MudObject
{
    private readonly Dictionary<string, ObjectRef> bodies = new();

    private static bool IsBody(ObjectRef? ob) => ob != null && ob.IsAlive && ob.IsClone && ob.ProgramPath == "/std/player";

    /// <summary>A player body registers itself (only it may).</summary>
    public void Register(ObjectRef body)
    {
        if (PreviousObject() != body || !IsBody(body)) return;
        var name = body.Call<string>("QueryName") ?? "";
        if (name != "") bodies[name.ToLowerInvariant()] = body;
    }

    /// <summary>A player body takes itself out (only it may).</summary>
    public void Unregister(ObjectRef body)
    {
        if (PreviousObject() != body) return;
        var name = body.Call<string>("QueryName") ?? "";
        if (bodies.TryGetValue(name.ToLowerInvariant(), out var held) && held == body) bodies.Remove(name.ToLowerInvariant());
    }

    /// <summary>The dropped player's body of this name that is still in the world, or null.</summary>
    public ObjectRef? Find(string name)
    {
        if (name == null || !bodies.TryGetValue(name.ToLowerInvariant(), out var body)) return null;
        if (!IsBody(body) || !body.Call<bool>("QueryLinkDead")) { bodies.Remove(name.ToLowerInvariant()); return null; }
        return body;
    }

    /// <summary>Every such body, by name.</summary>
    public List<ObjectRef> All()
    {
        var list = new List<ObjectRef>();
        foreach (var name in bodies.Keys.OrderBy(n => n).ToList())
            if (Find(name) is { } body) list.Add(body);
        return list;
    }
}
