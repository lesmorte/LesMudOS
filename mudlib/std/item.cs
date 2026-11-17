// @inherit /std/thing

/// <summary>A portable object. Set Value (gold), QuestItem and ReadText in Create() as needed.</summary>
public class Item : Thing
{
    /// <summary>Worth in gold. Shopkeepers pay half; 0 means they will not buy it.</summary>
    protected int Value;
    /// <summary>Quest items cannot be sold.</summary>
    protected bool QuestItem;
    /// <summary>If set, "read <thing>" shows this text.</summary>
    protected string? ReadText;

    public override bool CanGet() => true;

    public int QueryValue() => Value;

    public bool QueryQuestItem() => QuestItem;

    public string? QueryReadText() => ReadText;

    /// <summary>The equipment slot this item goes in ("weapon", "head", ...), or "" if it cannot be worn or wielded.</summary>
    public virtual string QuerySlot() => "";

    /// <summary>Called after a player has picked this item up. ThisPlayer() is the taker.</summary>
    protected virtual void OnGet(ObjectRef taker) { }

    /// <summary>Called by the "get" command after the item has been moved to the taker.</summary>
    public void Taken(ObjectRef taker)
    {
        if (Environment() == taker) OnGet(taker);
    }

    /// <summary>Remove this item from the game (sold, eaten, handed in).</summary>
    public void Vanish() => Destruct();

    public override string Stat() => base.Stat() + $"  Value {Value} gold, quest item {QuestItem}\n";
}
