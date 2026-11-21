// @inherit /std/consumable

/// <summary>
/// A crafted consumable whose effect is data: food, drink, a bandage, a salve, a tonic. Set the effect fields in Create() and the
/// item does the rest; quality scales every amount by the effect-scale table of /crafting/numbers.cs (Poor 0.6 to Legendary
/// 2.2 by default), or by this item's own QualityScale if it sets one, so a Fine salve heals more than a Common one.
///
///   RestoreVitality   Vitality restored at once
///   RestoreEndurance    Endurance restored at once
///   HealPerTick, Ticks, TickSeconds   healing over time: HealPerTick Vitality every TickSeconds, Ticks times. A character has one
///                     healing over time at a time; a new one replaces the old.
///   UseVerb           "eat", "drink" or "apply" (bandages and salves); the command of that name uses it, on the combat clock
///
/// Stackable by default. A crafted consumable that is also an ingredient (Berry Juice in Sweet Berry Juice) is registered in the
/// materials table like any other material. See docs/CRAFTING.md, "Crafted effects".
/// </summary>
public class Crafted : Consumable
{
    protected int RestoreVitality;
    protected int RestoreEndurance;
    /// <summary>OBSOLETE name of RestoreEndurance, kept so that content written against it still loads.</summary>
    protected int RestoreStamina { get => RestoreEndurance; set => RestoreEndurance = value; }
    protected int HealPerTick;
    protected int Ticks;
    protected int TickSeconds = 5;
    /// <summary>Per quality (Poor .. Legendary), what the amounts are multiplied by. Null: the table in /crafting/numbers.cs.</summary>
    protected double[]? QualityScale;

    public Crafted()
    {
        Stackable = true;
        UseVerb = "drink";
    }

    public bool IsCrafted() => true;

    private double Scale()
    {
        var q = QueryQuality();
        if (QualityScale != null && q < QualityScale.Length) return QualityScale[q];
        try { return LoadObject("/secure/crafting").Call<double>("EffectScale", q); }
        catch (MudRuntimeException) { return 1.0; }
    }

    private int Scaled(int amount) => amount <= 0 ? 0 : Math.Max(1, (int)Math.Round(amount * Scale(), MidpointRounding.AwayFromZero));

    /// <summary>The amounts this item gives at its quality: vitality now, endurance now, healing per tick, ticks, seconds per tick.</summary>
    public List<int> QueryEffect() => new() { Scaled(RestoreVitality), Scaled(RestoreEndurance), Scaled(HealPerTick), Ticks, TickSeconds };

    protected override void OnUse(ObjectRef user)
    {
        var e = QueryEffect();
        var lines = new List<string>();
        if (e[0] > 0)
        {
            var healed = user.Call<int>("Heal", e[0]);
            if (healed > 0) lines.Add($"You recover {healed} Vitality." + (user.Call<string>("HealNote", healed) ?? ""));
        }
        if (e[1] > 0)
        {
            var before = user.Call<int>("QueryEndurance");
            user.Call("RefundResources", e[1], 0);
            var gained = user.Call<int>("QueryEndurance") - before;
            if (gained > 0) lines.Add($"You recover {gained} Endurance.");
        }
        if (e[2] > 0 && e[3] > 0)
        {
            try
            {
                if (LoadObject("/secure/crafting").Call<bool>("StartHeal", user, e[2], e[3], Math.Max(1, e[4]), Cap(Name)))
                    lines.Add($"Warmth spreads through you: you will recover {e[2]} Vitality every {Math.Max(1, e[4])} seconds, {e[3]} times.");
            }
            catch (MudRuntimeException) { }
        }
        if (lines.Count > 0) TellObject(user, string.Join(" ", lines) + "\n");
    }

    public override string Appraisal()
    {
        var e = QueryEffect();
        var parts = new List<string>();
        if (e[0] > 0) parts.Add($"restores {e[0]} Vitality");
        if (e[1] > 0) parts.Add($"restores {e[1]} Endurance");
        if (e[2] > 0 && e[3] > 0) parts.Add($"heals {e[2]} every {e[4]} s, {e[3]} times");
        return base.Appraisal().TrimEnd('\n') + (parts.Count > 0 ? " Effect: " + string.Join(", ", parts) + "." : "") + "\n";
    }

    public override string Stat() => base.Stat() + $"  Crafted: vitality {RestoreVitality}, endurance {RestoreEndurance}, over time {HealPerTick} x {Ticks} every {TickSeconds}s; at this quality {string.Join("/", QueryEffect())}\n";
}
