// @inherit /std/thing

/// <summary>
/// One thing a combatant can do: an attack, a potion, a special move. It is data.
/// Total delay = (final Reaction Time + ReactionMod + Recovery) x 2 seconds; the
/// first Preparation x 2 seconds of that are the wind-up, then the action resolves.
/// docs/COMBAT_TIMING.md section 2.
/// </summary>
public class CombatAction
{
    /// <summary>Short key a creature's ChooseAction uses to find it ("slash").</summary>
    public string Id = "";
    /// <summary>What the action is called in stat ("Axe Slash").</summary>
    public string Name = "";
    /// <summary>Wind-up before the effect, in time units (2 seconds each). 0 for basic attacks.</summary>
    public double Preparation;
    /// <summary>How long the action takes to complete and recover from, in time units.</summary>
    public double Recovery;
    /// <summary>Added to the combatant's Reaction Time for this action only.</summary>
    public double ReactionMod;
    /// <summary>Shown to the room when the wind-up begins: a verb phrase, "draws his axe far back".</summary>
    public string WindUp = "";
    /// <summary>Shown to the room when the blow lands, before the roll. Optional.</summary>
    public string Strike = "";
    /// <summary>Damage multiplier of an attack action.</summary>
    public int DamageMultiplier = 1;
    /// <summary>Resource cost, unused until the skills pass.</summary>
    public int StaminaCost;
    /// <summary>Resource cost, unused until the spells pass.</summary>
    public int ManaCost;
    /// <summary>"attack" (a to-hit roll and damage), "use" (a consumable), or a builder's own kind handled in Special().</summary>
    public string Kind = "attack";
    /// <summary>The consumable of a "use" action.</summary>
    public ObjectRef? Item;
}

/// <summary>A haste or slow effect: it multiplies Reaction Time until it expires.</summary>
public class SpeedEffect
{
    public double Factor = 1.0;
    public long Until;
}

/// <summary>
/// Anything that has hit points and can fight: players, villagers, monsters.
/// Holds the numbers, the equipment slots and the combat engine, which runs on the
/// event clock of docs/COMBAT_TIMING.md: every combatant has a Reaction Time, every
/// action a Preparation and a Recovery, and a combatant's next step is one driver
/// call-out. Set the numbers in Create(); Hp starts full by itself.
/// </summary>
public abstract class Living : Thing
{
    private const int ForgetMs = 30000; // an enemy that stays out of the room this long is forgotten
    private const int UnitMs = 2000; // one time unit of the brief is 2 seconds
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
    /// <summary>Base Reaction Time, 1.0 (fast) to 6.0 (slow) in steps of 0.5. When can I begin my next action?</summary>
    protected double BaseReaction = 2.5;

    /// <summary>True once Die has run for a creature that does not come back.</summary>
    protected bool Dead;

    private readonly List<ObjectRef> enemies = new();
    private readonly Dictionary<ObjectRef, long> absentSince = new();
    private readonly Dictionary<string, ObjectRef> equipment = new();
    private readonly List<CombatAction> actions = new();
    private readonly Dictionary<string, SpeedEffect> speedEffects = new();
    private bool wasFighting;

    // The combat clock. Exactly one call-out (timerId) drives a combatant; chain is its token.
    private string phase = "idle"; // idle, preparing (wind-up running), recovering
    private CombatAction? current;
    private ObjectRef? currentTarget;
    private CombatAction? queued;
    private long actionStart;
    private long resolveAt;
    private long readyAt;
    private int timerId;
    private long timerDue;
    private int chain;
    private int actionsTaken;

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
        $"  Peaceful {QueryPeaceful()}, enemies {enemies.Count(e => e.IsAlive)}\n" +
        TimingStat();

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
        ResetCombat();
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

    // ---- Reaction Time ----

    /// <summary>Add equipment's reaction modifiers here; pass 5 adds the load.</summary>
    protected virtual double Encumbrance() => 0;

    /// <summary>Base Reaction Time, before anything else.</summary>
    public double QueryBaseReaction() => BaseReaction;

    /// <summary>The sum of the reaction modifiers of everything equipped.</summary>
    public double QueryEquipmentReaction()
    {
        var total = 0.0;
        foreach (var item in QueryEquipment()) total += item.Call<double>("QueryReactionMod");
        return total;
    }

    /// <summary>The product of all active haste and slow effects. 1.0 when there are none.</summary>
    public double QuerySpeedFactor()
    {
        var now = TimeMs();
        var factor = 1.0;
        foreach (var id in speedEffects.Keys.ToList())
        {
            if (speedEffects[id].Until <= now) { speedEffects.Remove(id); continue; }
            factor *= speedEffects[id].Factor;
        }
        return factor;
    }

    /// <summary>Final Reaction Time = (base + equipment modifiers + encumbrance) x speed factor.</summary>
    public double QueryReaction() => (BaseReaction + QueryEquipmentReaction() + Encumbrance()) * QuerySpeedFactor();

    /// <summary>
    /// Haste (factor below 1) or slow (above 1) on Reaction Time, for this many seconds. 20% haste is 0.8,
    /// 20% slow is 1.2. An effect with the same id replaces the old one; 0 seconds removes it.
    /// Every speed effect in the game goes through this.
    /// </summary>
    public void AddSpeedEffect(string id, double factor, int seconds)
    {
        if (Dead) return;
        if (seconds <= 0) { speedEffects.Remove(id); return; }
        speedEffects[id] = new SpeedEffect { Factor = Math.Max(0.1, Math.Min(10.0, factor)), Until = TimeMs() + seconds * 1000L };
    }

    public void RemoveSpeedEffect(string id) => speedEffects.Remove(id);

    /// <summary>The delay of an action in milliseconds: (final Reaction Time + action modifier + action Recovery) x 2 s.</summary>
    private long DelayMs(CombatAction a) => Math.Max(0, (long)Math.Round((QueryReaction() + a.ReactionMod + a.Recovery) * UnitMs));

    /// <summary>The preparation of an action in milliseconds, never longer than its total delay.</summary>
    private long PrepMs(CombatAction a) => Math.Min(DelayMs(a), Math.Max(0, (long)Math.Round(a.Preparation * UnitMs)));

    /// <summary>The delay in seconds of an action of this creature, as stat shows it.</summary>
    public double QueryDelay(CombatAction a) => DelayMs(a) / 1000.0;

    /// <summary>Seconds between this creature's basic actions (its first listed action, or the wielded weapon's attack). For "consider".</summary>
    public double QueryTypicalDelay() => QueryDelay(actions.Count > 0 ? actions[0] : BasicAttack());

    /// <summary>True if the creature has more than one kind of action.</summary>
    public bool QueryVariedAttacks() => actions.Count > 1;

    // ---- Actions ----

    /// <summary>
    /// Declare an action this creature can take. preparation and recovery are in time units (2 s each),
    /// windUp is a verb phrase shown when the preparation begins, damageMultiplier scales an attack.
    /// A creature with no declared action uses the plain attack of its weapon (or its fists).
    /// </summary>
    protected CombatAction AddCombatAction(string id, string name, double preparation = 0, double recovery = 0.5,
        string windUp = "", int damageMultiplier = 1, double reactionMod = 0, string strike = "", string kind = "attack")
    {
        var action = new CombatAction
        {
            Id = id, Name = name, Preparation = preparation, Recovery = recovery, WindUp = windUp,
            DamageMultiplier = damageMultiplier, ReactionMod = reactionMod, Strike = strike, Kind = kind,
        };
        actions.Add(action);
        return action;
    }

    /// <summary>A declared action by id, or null.</summary>
    protected CombatAction? FindAction(string id)
    {
        foreach (var action in actions) if (action.Id == id) return action;
        return null;
    }

    /// <summary>All declared actions.</summary>
    protected List<CombatAction> Actions() => actions;

    /// <summary>The plain attack with the wielded weapon, or unarmed (Recovery 0.5).</summary>
    protected CombatAction BasicAttack()
    {
        var weapon = QueryEquipped("weapon");
        var recovery = weapon != null ? weapon.Call<double>("QueryRecovery") : 0.5;
        return new CombatAction { Id = "attack", Name = weapon != null ? "Attack" : "Unarmed attack", Recovery = recovery };
    }

    /// <summary>
    /// How many actions this creature has chosen since this fight began (0 while choosing the first). Potions
    /// and other queued actions do not count. For patterns: "every third action" is ActionsTaken % 3 == 2.
    /// </summary>
    protected int ActionsTaken() => actionsTaken;

    /// <summary>
    /// The next action when this creature is ready and has an enemy. Players, and creatures with one declared action,
    /// need no override: the default is the first declared action, or the basic attack of the weapon.
    /// </summary>
    protected virtual CombatAction ChooseAction(ObjectRef target) => actions.Count > 0 ? actions[0] : BasicAttack();

    /// <summary>The effect of an action whose kind is not "attack" or "use". Own kinds (a battle cry, a summons) go here.</summary>
    protected virtual void Special(CombatAction action, ObjectRef? target) { }

    // ---- Combat: fights ----

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
    public bool StartFight(ObjectRef enemy) => BeginFight(enemy, false);

    /// <summary>
    /// Start a fight. The one who starts it acts as soon as it is ready. A creature that is drawn in (drawn is
    /// true: it joins a fight someone else started) and the one attacked cannot begin their first action
    /// until their own Reaction Time x 2 seconds have passed.
    /// </summary>
    protected bool BeginFight(ObjectRef enemy, bool drawn)
    {
        if (CanFight(enemy) != null) return false;
        var fresh = !enemies.Contains(enemy);
        if (drawn && !QueryInCombat()) DelayFirstAction();
        AddEnemyCore(enemy);
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

    /// <summary>Another creature has made this one its enemy. Unless this one is already fighting, its first action is delayed by its Reaction Time.</summary>
    public void AddEnemy(ObjectRef enemy)
    {
        if (Dead || enemy == Self || !enemy.IsAlive) return;
        if (!enemies.Contains(enemy) && !QueryInCombat()) DelayFirstAction();
        AddEnemyCore(enemy);
    }

    private void AddEnemyCore(ObjectRef enemy)
    {
        if (Dead || enemy == Self || !enemy.IsAlive) return;
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
        absentSince.Remove(enemy);
        wasFighting = true;
        Kick();
    }

    private void DelayFirstAction()
    {
        var until = TimeMs() + (long)Math.Round(QueryReaction() * UnitMs);
        if (until > readyAt) readyAt = until;
    }

    /// <summary>Make this enemy the one attacked first. A player's "kill" uses it to switch targets. False if it is not an enemy.</summary>
    public bool Focus(ObjectRef enemy)
    {
        var index = enemies.IndexOf(enemy);
        if (index < 0) return false;
        if (index > 0)
        {
            enemies.RemoveAt(index);
            enemies.Insert(0, enemy);
        }
        return true;
    }

    public void ForgetEnemy(ObjectRef enemy)
    {
        enemies.Remove(enemy);
        absentSince.Remove(enemy);
        CheckFightOver();
    }

    /// <summary>Drop every enemy, and make them drop this creature.</summary>
    public void ClearEnemies()
    {
        foreach (var enemy in enemies.ToList())
            if (enemy.IsAlive) enemy.Call("ForgetEnemy", Self);
        enemies.Clear();
        absentSince.Clear();
        CheckFightOver();
    }

    /// <summary>Is there an enemy in the room right now?</summary>
    public bool QueryInCombat()
    {
        var room = Environment();
        return room != null && enemies.Any(e => e.IsAlive && Environment(e) == room);
    }

    public bool QueryFightingWith(ObjectRef other) => enemies.Contains(other);

    /// <summary>The enemies in the order they will be attacked.</summary>
    protected List<ObjectRef> Enemies() => enemies.Where(e => e.IsAlive).ToList();

    private ObjectRef? FirstPresentEnemy()
    {
        var room = Environment();
        return room == null ? null : enemies.FirstOrDefault(e => e.IsAlive && Environment(e) == room);
    }

    private void CheckFightOver()
    {
        if (enemies.Count > 0 || !wasFighting) return;
        wasFighting = false;
        actionsTaken = 0;
        FightEnded();
    }

    /// <summary>Drop dead enemies and enemies that have been away for 30 seconds; a safe room ends every fight.</summary>
    private void PruneEnemies()
    {
        var room = Environment();
        if (room != null && room.Call<bool>("QuerySafe")) { ClearEnemies(); return; }
        var now = TimeMs();
        foreach (var enemy in enemies.ToList())
        {
            if (!enemy.IsAlive || enemy.Call<bool>("IsDead")) { ForgetEnemy(enemy); continue; }
            if (room != null && Environment(enemy) == room) { absentSince.Remove(enemy); continue; }
            if (!absentSince.TryGetValue(enemy, out var since)) absentSince[enemy] = now;
            else if (now - since >= ForgetMs) ForgetEnemy(enemy);
        }
        CheckFightOver();
    }

    /// <summary>The last word on outgoing damage, after the roll and before it is applied.</summary>
    protected virtual int ModifyDamage(int damage, ObjectRef target) => damage;

    /// <summary>Called once when the last enemy is gone and the fight is over.</summary>
    protected virtual void FightEnded() { }

    /// <summary>Per-heartbeat work for subclasses (regeneration, idle actions). Combat does not use heartbeats.</summary>
    protected virtual void Tick() { }

    /// <summary>Should the heartbeat keep running?</summary>
    protected virtual bool NeedsHeartBeat() => false;

    /// <summary>Called when another living thing comes within reach. ThisPlayer() is that thing.</summary>
    protected virtual void Noticed(ObjectRef other) { }

    public override void Init()
    {
        var other = ThisPlayer();
        if (other == null || other == Self || !other.IsAlive) return;
        Noticed(other);
        if (enemies.Contains(other)) Kick(); // an enemy came back within the 30 seconds
    }

    public override void HeartBeat()
    {
        if (Dead) return;
        Tick();
        if (Self.IsAlive && !NeedsHeartBeat()) SetHeartBeat(false);
    }

    // ---- Combat: the clock ----

    /// <summary>Forget everything in progress: the call-out, the current and the queued action. A revived player starts clean.</summary>
    protected void ResetCombat()
    {
        chain++;
        if (timerId != 0) RemoveCallOut(timerId);
        timerId = 0;
        phase = "idle";
        current = null;
        currentTarget = null;
        queued = null;
        readyAt = TimeMs();
        actionsTaken = 0;
    }

    /// <summary>
    /// Make sure the one call-out of this combatant is set for the next thing that has to happen: the end of a wind-up,
    /// or the moment it is ready. Safe to call at any time and as often as you like; it never leaves two call-outs.
    /// </summary>
    private void Kick()
    {
        if (Dead || !Self.IsAlive) return;
        if (phase == "preparing") Schedule(resolveAt);
        else Schedule(readyAt);
    }

    private void Schedule(long at)
    {
        var now = TimeMs();
        var due = Math.Max(at, now);
        if (timerId != 0)
        {
            if (timerDue == due) return;
            RemoveCallOut(timerId);
            timerId = 0;
        }
        var token = ++chain;
        timerDue = due;
        timerId = CallOut(() => OnTimer(token), (due - now) / 1000.0);
    }

    private void OnTimer(int token)
    {
        if (token != chain) return; // an older chain, cancelled
        timerId = 0;
        if (Dead || !Self.IsAlive) return;
        var now = TimeMs();
        if (phase == "preparing")
        {
            var action = current!;
            var target = currentTarget;
            phase = "recovering";
            Resolve(action, target);
            if (Dead || !Self.IsAlive) return;
            Schedule(readyAt);
            return;
        }
        if (now < readyAt) { Schedule(readyAt); return; } // ready time was pushed back
        phase = "idle";
        current = null;
        currentTarget = null;
        TryBegin();
    }

    /// <summary>Ready: take the queued action if there is one, else, with an enemy present, whatever ChooseAction says.</summary>
    private void TryBegin()
    {
        if (Dead || !Self.IsAlive) return;
        PruneEnemies();
        if (Dead || !Self.IsAlive) return;
        var action = queued;
        queued = null;
        var target = FirstPresentEnemy();
        if (action != null && action.Item != null && (!action.Item.IsAlive || Environment(action.Item) != Self))
        {
            Tell("You no longer have what you meant to use.\n");
            action = null;
        }
        if (action == null)
        {
            if (target == null)
            {
                // Nothing to do. Remembered enemies that are away are forgotten after 30 seconds.
                if (absentSince.Count > 0) Schedule(absentSince.Values.Min() + ForgetMs);
                return;
            }
            action = ChooseAction(target);
            actionsTaken++;
        }
        Begin(action, target);
    }

    /// <summary>Start an action now: announce the wind-up, resolve at once if there is none, and set the Ready Time.</summary>
    private void Begin(CombatAction action, ObjectRef? target)
    {
        var now = TimeMs();
        var prep = PrepMs(action);
        current = action;
        currentTarget = target;
        actionStart = now;
        resolveAt = now + prep;
        readyAt = now + DelayMs(action);
        var room = Environment();
        if (prep > 0 && action.WindUp != "" && room != null) TellRoom(room, $"{Cap(QueryLabel())} {action.WindUp}...\n");
        if (prep > 0)
        {
            phase = "preparing";
            Schedule(resolveAt);
            return;
        }
        phase = "recovering";
        Resolve(action, target);
        if (Dead || !Self.IsAlive) return;
        Schedule(readyAt);
    }

    /// <summary>The effect of an action. A target that died or left makes it wasted.</summary>
    private void Resolve(CombatAction action, ObjectRef? target)
    {
        var room = Environment();
        if (room == null) return;
        if (action.Kind == "use")
        {
            if (action.Item != null && action.Item.IsAlive && Environment(action.Item) == Self) action.Item.Call("Consume", Self);
            return;
        }
        var present = target != null && target.IsAlive && !target.Call<bool>("IsDead") && Environment(target) == room;
        if (action.Kind == "attack")
        {
            if (!present)
            {
                if (action.Preparation > 0) TellRoom(room, $"{Cap(QueryLabel())}'s blow finds nothing to hit.\n");
                PruneEnemies();
                return;
            }
            if (action.Strike != "") TellRoom(room, $"{Cap(QueryLabel())} {action.Strike}\n");
            Attack(target!, action);
        }
        else Special(action, present ? target : null);
        if (!Dead && Self.IsAlive) PruneEnemies();
    }

    private void Attack(ObjectRef target, CombatAction action)
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
        damage = Math.Max(1, ModifyDamage(damage, target)) * Math.Max(1, action.DamageMultiplier);
        var tier = damage <= 2 ? 0 : damage <= 4 ? 1 : damage <= 7 ? 2 : 3;
        Tell($"You {HitYou[tier]} {theirs}{HitHow[tier]}.\n");
        TellObject(target, $"{label} {HitThem[tier]} you{HitHow[tier]}.\n");
        TellRoom(room, $"{label} {HitThem[tier]} {theirs}{HitHow[tier]}.\n", Self, target);
        target.Call("TakeDamage", damage, Self);
    }

    // ---- Combat: the queue ----

    /// <summary>Can this creature begin an action this very moment?</summary>
    public bool QueryReady() => phase != "preparing" && TimeMs() >= readyAt;

    /// <summary>
    /// Use a consumable as an action (Recovery from the item). If this creature is ready it begins at once;
    /// otherwise the action is queued, replacing any queued one, and happens at the next Ready Time in place of an attack.
    /// </summary>
    protected void UseAsAction(ObjectRef item)
    {
        var action = new CombatAction
        {
            Id = "use", Name = "Use " + (item.Call<string>("Short") ?? "item"), Kind = "use",
            Recovery = item.Call<double>("QueryRecovery"), Item = item,
        };
        if (QueryReady())
        {
            queued = null;
            Begin(action, FirstPresentEnemy());
            return;
        }
        var replaced = queued != null;
        queued = action;
        Tell($"You are not ready yet. You will {item.Call<string>("QueryUseVerb")} {item.Call<string>("Short")} as soon as you can" +
             (replaced ? ", instead of what you had planned" : "") + ". ({cmd}stop{/} cancels.)\n");
        Kick();
    }

    /// <summary>Clear the queued action. True if there was one.</summary>
    protected bool ClearQueued()
    {
        var had = queued != null;
        queued = null;
        return had;
    }

    protected bool HasQueued() => queued != null;

    // ---- Combat: what stat shows ----

    private string ActionLine(CombatAction a) =>
        $"    {a.Name}: preparation {Num(a.Preparation)}, recovery {Num(a.Recovery)}, modifier {Num(a.ReactionMod)}, delay {Num(QueryDelay(a))} s" +
        (a.WindUp != "" ? $", wind-up \"{a.WindUp}\"" : "") + (a.DamageMultiplier > 1 ? $", damage x{a.DamageMultiplier}" : "") + "\n";

    private string TimingStat()
    {
        var text = new StringBuilder();
        text.Append($"  Reaction Time: base {Num(BaseReaction)}, equipment {Num(QueryEquipmentReaction())}, encumbrance {Num(Encumbrance())}, speed x{Num(QuerySpeedFactor())} = final {Num(QueryReaction())}\n");
        text.Append("  Actions:\n");
        if (actions.Count == 0) text.Append(ActionLine(BasicAttack()));
        foreach (var action in actions) text.Append(ActionLine(action));
        var now = TimeMs();
        var state = phase == "preparing" ? $"preparing {current?.Name}, resolves in {Num(Math.Max(0, resolveAt - now) / 1000.0)} s, ready in {Num(Math.Max(0, readyAt - now) / 1000.0)} s"
            : now < readyAt ? $"recovering, ready in {Num((readyAt - now) / 1000.0)} s"
            : "ready";
        text.Append($"  Now: {state}; queued: {(queued == null ? "nothing" : queued.Name)}; actions this fight {actionsTaken}\n");
        return text.ToString();
    }
}
