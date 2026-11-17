// @inherit /std/item

/// <summary>
/// Eaten or drunk once. Set HealAmount and UseVerb ("eat" or "drink") in
/// Create(); UseMessage replaces the default line shown to the user.
/// </summary>
public class Consumable : Item
{
    protected int HealAmount;
    protected string UseVerb = "drink";
    protected string? UseMessage;

    public string QueryUseVerb() => UseVerb;

    /// <summary>Called by the eat and drink commands. Heals the user and uses the item up.</summary>
    public void Consume(ObjectRef user)
    {
        if (Environment() != user) return;
        var healed = HealAmount > 0 ? user.Call<int>("Heal", HealAmount) : 0;
        TellObject(user, (UseMessage ?? $"You {UseVerb} {Short()}.") + (healed > 0 ? $" You recover {healed} hit points." : "") + "\n");
        OnUse(user);
        Destruct();
    }

    /// <summary>Extra effects for special consumables.</summary>
    protected virtual void OnUse(ObjectRef user) { }

    public override string Stat() => base.Stat() + $"  Consumable: {UseVerb}, heals {HealAmount}\n";
}
