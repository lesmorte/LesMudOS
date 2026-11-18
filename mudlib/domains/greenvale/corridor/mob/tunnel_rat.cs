// @inherit /std/monster

/// <summary>
/// Tunnel Rat, level 7. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmTunnelRat : Monster
{

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Tunnel Rat";
        Ids.Add("rat");
        Ids.Add("tunnel rat");
        ShortDesc = "a tunnel rat";
        LongDesc = "A rat as long as a forearm scrabbles along the tunnel wall, its tail hairless and glistening with the damp of the roots. Its fur is pale from a life away from daylight and its eyes are small and red. It chitters shrilly when it sees you.";
        Level = 7;
        MaxHp = 40;
        DamageMin = 3;
        DamageMax = 6;
        ToHit = 3;
        Armor = 1;
        XpValue = 165;
        Aggressive = true;
        BaseReaction = 1.5;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
    }
}
