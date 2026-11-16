// @inherit /std/thing

/// <summary>Shows HeartBeat and an object that offers its own verb.</summary>
public class Cat : Thing
{
    private static readonly string[] Antics =
    {
        "The cat washes a paw with great concentration.",
        "The cat watches something invisible cross the room.",
        "The cat stretches, yawns, and resettles by the hearth.",
    };

    private int beats;

    public override void Create()
    {
        Name = "cat";
        ShortDesc = "a ginger tavern cat";
        LongDesc = "A large ginger cat with one torn ear. It looks like it would tolerate being petted.";
        SetHeartBeat(true);
    }

    public override void Init() => AddAction("pet", CmdPet);

    private bool CmdPet(string args)
    {
        if (!Id(args)) { NotifyFail("Pet what?"); return false; }
        Write("The cat purrs like a small engine.\n");
        Say($"{ThisPlayer()?.Call<string>("QueryName")} pets the cat.\n");
        return true;
    }

    public override void HeartBeat()
    {
        if (++beats % 3 != 0) return;
        var room = Environment();
        if (room != null) TellRoom(room, Antics[Random(Antics.Length)] + "\n");
    }
}
