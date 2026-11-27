// @inherit /std/monster

/// <summary>
/// Field Rabbit, level 1. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmFieldRabbit : Monster
{

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "rabbit";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "rabbit";

    public override void Create()
    {
        Name = "Field Rabbit";
        Ids.Add("rabbit");
        Ids.Add("field rabbit");
        ShortDesc = "a field rabbit";
        Tags.Add("beast"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("bite");
        LongDesc = "A plump brown rabbit sits bolt upright in the verge, nose working, one ear torn at the tip. It looks like an animal that has eaten well all summer and fears nothing larger than a hawk. If it is cornered it kicks and bites with surprising fury.";
        Level = 1;
        MaxVitality = 8;
        DamageMin = 1;
        DamageMax = 2;
        ToHit = 0;
        Armor = 0;
        XpValue = 12;
        Aggressive = false;
        Assist = false;
        BaseReaction = 1.5;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
    }
}
