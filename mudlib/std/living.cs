// @inherit /std/thing

/// <summary>
/// Anything that has hit points and can fight: players, villagers, monsters.
/// Holds the numbers, the equipment slots and the round-based combat engine.
/// Set the numbers in Create(); Hp starts full by itself.
/// </summary>
public abstract class Living : Thing
{
    private static readonly string[] HitYou = { "scratch", "hit", "hit", "smash" };
    private static readonly string[] HitThem = { "scratches", "hits", "hits", "smashes" };
    private static readonly string[] HitHow = { "", "", " hard", " brutally" };

    [Saved] protected int Level = 1;
    protected int MaxHp = 10;
    [Saved] private int hp = -1; // -1: not yet set, meaning full
    protected int DamageMin = 1;
    protected int DamageMax = 2;
    protected int ToHit;
    protected int Armor;
    [Saved] protected int Gold;

    /// <summary>Set by CombatRound to skip this round's normal attack.</summary>
    protected bool SkipAttack;
    /// <summary>True once Die has run for a creature that does not come back.</summary>
    protected bool Dead;

    private readonly List<ObjectRef> enemies = new();
    private readonly Dictionary<ObjectRef, long> absentSince = new();
    private readonly Dictionary<string, ObjectRef> equipment = new();
    private bool wasFighting;

    protected int Hp
    {
        get
        {
            var max = QueryMaxHp();
            if (hp < 0 || hp > max) hp = max;
            return hp;
        }
        set => hp = Math.Max(0, Math.Min(value, QueryMaxHp()));
    }

    // ---- Queries other objects use ----

    public bool IsCreature() => true;

    public bool IsDead() => Dead;

    /// <summary>True for players; they cannot fight each other.</summary>
    public virtual bool IsPlayerCharacter() => false;

    public int QueryLevel() => Level;

    public int QueryHp() => Hp;

    public virtual int QueryMaxHp() => MaxHp + EquipmentHpBonus();

    public int QueryGold() => Gold;

    public virtual bool QueryPeaceful() => false;

    /// <summary>How this creature is named in combat messages: "a large rat", "Keith".</summary>
    public virtual string QueryLabel() => Short() ?? Name;

    public int QueryArmor()
    {
        var total = Armor;
        foreach (var item in QueryEquipment()) total += item.Call<int>("QueryArmorValue");
        return total;
    }

    public int QueryToHit() => ToHit + (QueryEquipped("weapon")?.Call<int>("QueryToHit") ?? 0);

    public int QueryDamageMin() => QueryEquipped("weapon") is { } w ? w.Call<int>("QueryDamageMin") : DamageMin;

    public int QueryDamageMax() => QueryEquipped("weapon") is { } w ? w.Call<int>("QueryDamageMax") : DamageMax;

    protected int EquipmentHpBonus()
    {
        var total = 0;
        foreach (var item in QueryEquipment()) total += item.Call<int>("QueryMaxHpBonus");
        return total;
    }

    /// <summary>One line on how hurt this creature looks.</summary>
    public string HealthLine()
    {
        var percent = Hp * 100 / Math.Max(1, QueryMaxHp());
        var state = percent >= 100 ? "is unhurt"
            : percent >= 70 ? "has a few scratches"
            : percent >= 40 ? "is wounded"
            : percent >= 15 ? "is badly wounded"
            : "is near death";
        return $"{Cap(QueryLabel())} {state}.\n";
    }

    public override string Long() => base.Long() + HealthLine();

    public override string Stat() =>
        base.Stat() +
        $"  Level {Level}, HP {Hp}/{QueryMaxHp()}, gold {Gold}\n" +
        $"  Damage {QueryDamageMin()}-{QueryDamageMax()}, to hit {QueryToHit()}, armor {QueryArmor()}\n" +
        $"  Peaceful {QueryPeaceful()}, enemies {enemies.Count(e => e.IsAlive)}\n";

    /// <summary>Send text to this creature itself. Safe to call from any execution context.</summary>
    protected void Tell(string message) => TellObject(Self, message);

    // ---- Hit points ----

    /// <summary>Restore hit points, up to the maximum. Returns how many were restored.</summary>
    public int Heal(int amount)
    {
        if (Dead || amount <= 0) return 0;
        var before = Hp;
        Hp = before + amount;
        return Hp - before;
    }

    /// <summary>Lose hit points. A creature that reaches 0 dies. The attacker, if any, becomes an enemy.</summary>
    public void TakeDamage(int amount, ObjectRef? from)
    {
        if (Dead || amount <= 0) return;
        if (from != null && from.IsAlive && from != Self && Environment(from) == Environment()) AddEnemy(from);
        Hp -= amount;
        if (hp <= 0) Die(from);
    }

    /// <summary>The creature dies. Monsters, players and bystanders override this.</summary>
    public virtual void Die(ObjectRef? killer)
    {
        if (Dead) return;
        Dead = true;
        Destruct();
    }

    // ---- Equipment ----

    /// <summary>Put an item from this creature's inventory into its slot. Replaces what was there. No messages.</summary>
    public bool Equip(ObjectRef item)
    {
        if (!item.IsAlive || Environment(item) != Self) return false;
        var slot = item.Call<string>("QuerySlot");
        if (string.IsNullOrEmpty(slot)) return false;
        equipment[slot] = item;
        return true;
    }

    public bool Unequip(string slot) => equipment.Remove(slot);

    /// <summary>The item in a slot, or null. Items that left the inventory are no longer equipped.</summary>
    public ObjectRef? QueryEquipped(string slot)
    {
        if (!equipment.TryGetValue(slot, out var item)) return null;
        if (item.IsAlive && Environment(item) == Self) return item;
        equipment.Remove(slot);
        return null;
    }

    public bool IsEquipped(ObjectRef item) => QueryEquipment().Contains(item);

    public List<ObjectRef> QueryEquipment()
    {
        var result = new List<ObjectRef>();
        foreach (var slot in equipment.Keys.ToList())
            if (QueryEquipped(slot) is { } item) result.Add(item);
        return result;
    }

    // ---- Combat ----

    /// <summary>Why this creature cannot attack the target, or null if it can.</summary>
    public string? CanFight(ObjectRef enemy)
    {
        if (Dead) return "You are in no state to fight.";
        if (!enemy.IsAlive || enemy.Call<bool>("IsDead")) return "That is already dead.";
        if (!enemy.Call<bool>("IsCreature")) return "You cannot fight that.";
        if (enemy == Self) return "Attacking yourself would not help.";
        if (IsPlayerCharacter() && enemy.Call<bool>("IsPlayerCharacter")) return "Players cannot fight each other.";
        var room = Environment();
        if (room == null || Environment(enemy) != room) return "That is not here.";
        if (room.Call<bool>("QuerySafe")) return "A calm lies over this place; nobody may fight here.";
        if (enemy.Call<bool>("QueryPeaceful")) return $"{Cap(enemy.Call<string>("QueryLabel") ?? "It")} is no threat to you.";
        return null;
    }

    /// <summary>Begin fighting. Both sides add each other as enemies. False if it is not allowed.</summary>
    public bool StartFight(ObjectRef enemy)
    {
        if (CanFight(enemy) != null) return false;
        var fresh = !enemies.Contains(enemy);
        AddEnemy(enemy);
        enemy.Call("AddEnemy", Self);
        if (!fresh) return true;

        var room = Environment()!;
        var label = QueryLabel();
        var theirs = enemy.Call<string>("QueryLabel") ?? "it";
        Tell($"You attack {theirs}.\n");
        TellObject(enemy, $"{Cap(label)} attacks you!\n");
        TellRoom(room, $"{Cap(label)} attacks {theirs}!\n", Self, enemy);
        foreach (var other in AllInventory(room))
            if (other != Self && other != enemy) other.Call("JoinFight", Self, enemy);
        return true;
    }

    public void AddEnemy(ObjectRef enemy)
    {
        if (Dead || enemy == Self || !enemy.IsAlive) return;
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
        absentSince.Remove(enemy);
        wasFighting = true;
        SetHeartBeat(true);
    }

    public void ForgetEnemy(ObjectRef enemy)
    {
        enemies.Remove(enemy);
        absentSince.Remove(enemy);
    }

    /// <summary>Drop every enemy, and make them drop this creature.</summary>
    public void ClearEnemies()
    {
        foreach (var enemy in enemies.ToList())
            if (enemy.IsAlive) enemy.Call("ForgetEnemy", Self);
        enemies.Clear();
        absentSince.Clear();
    }

    /// <summary>Is there an enemy in the room right now?</summary>
    public bool QueryInCombat()
    {
        var room = Environment();
        return room != null && enemies.Any(e => e.IsAlive && Environment(e) == room);
    }

    public bool QueryFightingWith(ObjectRef other) => enemies.Contains(other);

    /// <summary>Called once per round before the normal attack. Set SkipAttack to replace the attack. For boss abilities.</summary>
    protected virtual void CombatRound(ObjectRef target) { }

    /// <summary>The last word on outgoing damage, after the roll and before it is applied.</summary>
    protected virtual int ModifyDamage(int damage, ObjectRef target) => damage;

    /// <summary>Called once when the last enemy is gone and the fight is over.</summary>
    protected virtual void FightEnded() { }

    /// <summary>Per-heartbeat work for subclasses (regeneration, idle actions).</summary>
    protected virtual void Tick() { }

    /// <summary>Should the heartbeat keep running while there is no fight?</summary>
    protected virtual bool NeedsHeartBeat() => false;

    /// <summary>Called when another living thing comes within reach. ThisPlayer() is that thing.</summary>
    protected virtual void Noticed(ObjectRef other) { }

    public override void Init()
    {
        var other = ThisPlayer();
        if (other != null && other != Self && other.IsAlive) Noticed(other);
    }

    public override void HeartBeat()
    {
        if (Dead) return;
        if (enemies.Count > 0) FightRound();
        if (Dead || !Self.IsAlive) return;
        if (enemies.Count == 0 && wasFighting)
        {
            wasFighting = false;
            FightEnded();
        }
        Tick();
        if (Self.IsAlive && enemies.Count == 0 && !NeedsHeartBeat()) SetHeartBeat(false);
    }

    private void FightRound()
    {
        var room = Environment();
        if (room == null) return;
        if (room.Call<bool>("QuerySafe")) { ClearEnemies(); return; }

        var now = Time();
        foreach (var enemy in enemies.ToList())
        {
            if (!enemy.IsAlive || enemy.Call<bool>("IsDead")) { ForgetEnemy(enemy); continue; }
            if (Environment(enemy) == room) { absentSince.Remove(enemy); continue; }
            if (!absentSince.TryGetValue(enemy, out var since)) absentSince[enemy] = now;
            else if (now - since >= 30) ForgetEnemy(enemy);
        }

        var target = enemies.FirstOrDefault(e => Environment(e) == room);
        if (target == null) return;
        SkipAttack = false;
        CombatRound(target);
        if (Dead || !Self.IsAlive || SkipAttack || !target.IsAlive) return;
        Attack(target);
    }

    private void Attack(ObjectRef target)
    {
        var room = Environment();
        if (room == null) return;
        var label = Cap(QueryLabel());
        var theirs = target.Call<string>("QueryLabel") ?? "it";

        var roll = Random(20) + 1;
        var need = 10 + target.Call<int>("QueryArmor");
        var hit = roll == 20 || (roll != 1 && roll + Level + QueryToHit() >= need);
        if (!hit)
        {
            Tell($"You miss {theirs}.\n");
            TellObject(target, $"{label} misses you.\n");
            TellRoom(room, $"{label} misses {theirs}.\n", Self, target);
            return;
        }

        var low = QueryDamageMin();
        var high = Math.Max(low, QueryDamageMax());
        var damage = Math.Max(1, low + Random(high - low + 1));
        damage = Math.Max(1, ModifyDamage(damage, target));
        var tier = damage <= 2 ? 0 : damage <= 4 ? 1 : damage <= 7 ? 2 : 3;
        Tell($"You {HitYou[tier]} {theirs}{HitHow[tier]}.\n");
        TellObject(target, $"{label} {HitThem[tier]} you{HitHow[tier]}.\n");
        TellRoom(room, $"{label} {HitThem[tier]} {theirs}{HitHow[tier]}.\n", Self, target);
        target.Call("TakeDamage", damage, Self);
    }
}
