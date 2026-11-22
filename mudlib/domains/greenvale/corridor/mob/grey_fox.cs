// @inherit /std/monster

/// <summary>
/// Grey Fox, level 4. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmGreyFox : Monster
{

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "fox";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "fox";

    public override void Create()
    {
        Name = "Grey Fox";
        Ids.Add("fox");
        Ids.Add("grey fox");
        ShortDesc = "a grey fox";
        LongDesc = "A lean grey fox with a rust-coloured throat and a black-tipped brush slips along the hedgerow. Its narrow yellow eyes follow every movement and its feet barely trouble the grass. It darts and snaps rather than standing to fight.";
        Level = 4;
        MaxVitality = 26;
        DamageMin = 2;
        DamageMax = 4;
        ToHit = 1;
        Armor = 1;
        Dodge = 10;
        XpValue = 65;
        Aggressive = false;
        BaseReaction = 1.5;
        AddCombatAction("nip", "Nip", preparation: 0, recovery: 0.5);
    }
}
