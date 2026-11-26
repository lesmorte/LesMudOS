// @inherit /std/thing

/// <summary>
/// A light that is part of a place: a lantern on a hook, a lamp on a table. It cannot be taken and never goes out; it adds +1 (LightValue) to the
/// light of the room it stands in (docs/specs/light.md, section 11). Put it in a room with AddSpawn. Set Name, Ids, ShortDesc, LongDesc and HereLine
/// ("A storm lantern hangs from a hook.", the sentence the room lists it by) in Create().
/// </summary>
public class WallLight : Thing
{
    /// <summary>How much light it gives (default +1).</summary>
    protected int LightValue = 1;
    /// <summary>The sentence the room lists it by.</summary>
    protected string HereLine = "";

    public bool IsLightSource() => true;

    public bool QueryLit() => true;

    public int QueryLightValue() => LightValue;

    public string QueryHereLine() => HereLine != "" ? HereLine : Cap(Short() ?? "A lamp") + " burns here.";

    public override bool CanGet() => false;

    public override void OnMoved(ObjectRef? from)
    {
        if (Environment() == null) return;
        // After the room has finished making itself: the register looks at the room, which may be in the middle of its own Create().
        CallOut(() =>
        {
            try { if (Environment() != null) LoadObject("/secure/light").Call("AddSource", Self); }
            catch (MudRuntimeException) { }
        }, 0.2);
    }

    public override void OnDestruct()
    {
        try { LoadObject("/secure/light").Call("DropSource", Self, Environment()); }
        catch (MudRuntimeException) { }
    }
}
