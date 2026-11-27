// @inherit /std/item

/// <summary>
/// A wieldable weapon. Set DamageMin, DamageMax and optionally ToHit in Create(), and its timing:
/// ReactionMod is added to the wielder's Reaction Time (heavy weapons are slow to bring to bear),
/// Recovery is the Recovery of its basic attack (time units of 2 seconds).
/// </summary>
public class Weapon : Item
{
    protected int DamageMin = 1;
    protected int DamageMax = 2;
    protected int ToHit;
    /// <summary>Added to the wielder's Reaction Time while wielded.</summary>
    protected double ReactionMod;
    /// <summary>Recovery of the basic attack with this weapon.</summary>
    protected double Recovery = 0.5;
    /// <summary>"dagger", "sword", "spear", "axe", "mace", "staff" or "bow". Which characters may wield it depends on this.</summary>
    protected string Category = "";

    public override string QuerySlot() => "weapon";

    public int QueryDamageMin() => DamageMin;

    public int QueryDamageMax() => DamageMax;

    public int QueryToHit() => ToHit;

    public override double QueryReactionMod() => ReactionMod;

    public double QueryRecovery() => Recovery;

    public string QueryCategory() => Category;

    protected override string TagCarrier() => "weapon";

    /// <summary>A weapon whose file states no kind has its Category as its kind (the runtime implies its reach and damage type from that, /secure/tags).</summary>
    protected override void CollectTags(List<string> tags)
    {
        if (Category == "") return;
        List<string> kinds;
        try { kinds = LoadObject("/secure/tags").Call<List<string>>("QueryWeaponKinds") ?? new List<string>(); }
        catch (MudRuntimeException) { return; }
        if (!tags.Any(t => kinds.Contains(t))) tags.Add(Category);
    }

    /// <summary>" (slashing, melee)" for a weapon with tags, else "".</summary>
    private string TagWords()
    {
        var damage = QueryTagIn("damage");
        var reach = QueryTagIn("reach");
        return damage == "" && reach == "" ? "" : " (" + string.Join(", ", new[] { damage, reach }.Where(x => x != "")) + ")";
    }

    public override string Appraisal() =>
        $"{Cap(Short() ?? Name)}: {(Category == "" ? "a weapon" : AOrAn(Category))}{TagWords()}. Damage {DamageMin}-{DamageMax}, to hit +{ToHit}, reaction modifier {Num(ReactionMod)}, recovery {Num(Recovery)}. Worth {QueryValue()} gold." + AppraisalQuality() +
        (Magic != "" ? $" Magical property: {Magic}." : " It holds no magic.") + "\n";

    public override string Stat() => base.Stat() + $"  Weapon: damage {DamageMin}-{DamageMax}, to hit {ToHit}, reaction {Num(ReactionMod)}, recovery {Num(Recovery)}\n";
}
