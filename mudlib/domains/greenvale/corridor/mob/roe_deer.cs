// @inherit /std/monster

/// <summary>
/// Roe Deer, level 3. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmRoeDeer : Monster
{

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "deer";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "deer";

    public override void Create()
    {
        Name = "Roe Deer";
        Ids.Add("deer");
        Ids.Add("roe deer");
        Ids.Add("roe");
        ShortDesc = "a roe deer";
        LongDesc = "A roe deer stands at the edge of the trees, its red-brown coat dappled with light and its white rump patch a flag in the shade. It watches the road with huge dark eyes, ready to bound away at the first sudden move. A cornered deer will lash out with sharp hooves.";
        Level = 3;
        MaxVitality = 22;
        DamageMin = 1;
        DamageMax = 3;
        ToHit = 0;
        Armor = 0;
        XpValue = 26;
        Aggressive = false;
        Assist = false;
        BaseReaction = 2.5;
        AddCombatAction("kick", "Kick", preparation: 0, recovery: 1);
    }
}
