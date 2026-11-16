// @inherit /std/item

/// <summary>Shows object-supplied verbs (AddAction) and timers (CallOut).</summary>
public class Lantern : Item
{
    private bool lit;
    private int burnOut;

    public override void Create()
    {
        Name = "lantern";
        Ids.Add("lamp");
        LongDesc = "A dented brass lantern. You could light it.";
    }

    public override string? Short() => lit ? "a brass lantern (lit)" : "a brass lantern";

    public override void Init()
    {
        AddAction("light", CmdLight);
        AddAction("extinguish", CmdExtinguish);
    }

    private bool CmdLight(string args)
    {
        if (!Id(args)) { NotifyFail("Light what?"); return false; }
        if (lit) { Write("It is already lit.\n"); return true; }
        lit = true;
        burnOut = CallOut(BurnOut, 20);
        Write("You light the lantern. It will not last long.\n");
        Say($"{ThisPlayer()?.Call<string>("QueryName")} lights a lantern.\n");
        return true;
    }

    private bool CmdExtinguish(string args)
    {
        if (!Id(args)) { NotifyFail("Extinguish what?"); return false; }
        if (!lit) { Write("It is not lit.\n"); return true; }
        lit = false;
        RemoveCallOut(burnOut);
        Write("You put the lantern out.\n");
        return true;
    }

    private void BurnOut()
    {
        lit = false;
        var holder = Environment();
        if (holder == null) return;
        if (Living(holder)) TellObject(holder, "Your lantern sputters and goes dark.\n");
        else TellRoom(holder, "The lantern sputters and goes dark.\n");
    }
}
