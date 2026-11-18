// @inherit /std/room

/// <summary>The smugglers' concealed stash behind the locked door in the Mushroom Cavern. Its loot is placed once and never restocked.</summary>
public class HiddenCache : Room
{
    public override void Create()
    {
        ShortDesc = "Hidden Cache";
        LongDesc = "A concealed smuggler stash untouched for decades.";
        Exits["south"] = "/domains/hollow/cellars/mushroom_cavern";
        Safe = false;

        AddDetail("chests,chest,stash,smuggler stash", "Sealed wooden chests are stacked against the back wall, their iron bindings furred with rust, but their locks are still whole.");
        AddDetail("dust,cobwebs,floor", "Decades of dust lie undisturbed across the floor, and the only tracks in it are the ones you have just made.");
        AddDetail("door,iron door,rust-eaten door", "The rust-eaten door you came through has been pulled shut behind you. Old scrape marks on its inner face show where it was forced open once and never properly repaired.");

        MoveObject(CloneObject("/domains/hollow/obj/cache_gold"), Self);
        MoveObject(CloneObject("/domains/hollow/obj/magical_trinket"), Self);
        if (Random(100) < 25) MoveObject(CloneObject("/domains/hollow/obj/enchanted_dagger"), Self);
    }

    // Tells the Chronicle that a player has come here. A character is credited once, so every visit may report.
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        try
        {
            LoadObject("/secure/events").Call<string>("Report", "discover.hidden", who, new List<ObjectRef> { who }, Self, "hidden-cache",
                new Dictionary<string, string> { ["name"] = "the Hidden Cache" });
        }
        catch (MudRuntimeException) { }
    }
}
