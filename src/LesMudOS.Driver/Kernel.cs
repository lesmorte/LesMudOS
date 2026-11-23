using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using LesMudOS.Api;
using LesMudOS.Driver.Compilation;
using LesMudOS.Driver.Net;
using Runtime = LesMudOS.Api.Runtime;

namespace LesMudOS.Driver;

internal static class Log
{
    public static void Info(string message) => Console.WriteLine($"{DateTime.Now:HH:mm:ss} {message}");
}

/// <summary>A move the master refused (IMaster.MoveRefusal). The message is the master's one-line reason.</summary>
internal class RefusedException : MudRuntimeException
{
    public RefusedException(string message) : base(message) { }
}

/// <summary>A sandbox domain has used its share of the game thread's time for now (Kernel.EnterDomain). Not logged each time.</summary>
internal sealed class SandboxBudgetException : RefusedException
{
    public SandboxBudgetException(string message) : base(message) { }
}

/// <summary>
/// The driver kernel: object table, containment tree, command dispatch,
/// timers and the single-threaded game loop. Everything that touches world
/// state runs on the game thread; the network layer only posts events to it.
/// </summary>
internal sealed partial class Kernel : IDriverServices
{
    public const string MasterPath = "/secure/master";
    private static readonly TimeSpan HeartBeatInterval = TimeSpan.FromSeconds(2);

    private readonly string _mudlibRoot;
    private readonly ProgramCompiler _compiler;
    private readonly SaveStore _saves;
    private readonly BlockingCollection<Action> _events = new();
    private readonly Dictionary<string, ObjectRef> _objects = new();
    private readonly List<ObjectRef> _heartBeats = new();
    private readonly PriorityQueue<CallOutEntry, DateTime> _callOuts = new();
    private readonly HashSet<int> _cancelledCallOuts = new();
    private readonly List<Connection> _connections = new();
    private readonly ConditionalWeakTable<Type, Dictionary<(string, int), MethodInfo?>> _methodCache = new();

    private readonly List<ObjectRef> _callStack = new();
    private int _restricted;    // how many frames on _callStack belong to restricted objects (a domain other than "")
    private int _masterDepth;   // > 0 while the master answers a question: its own calls are not put to ValidCall
    private int _refusals;      // ValidCall refusals in this execution, for the log
    private volatile string? _lastRestricted; // the sandbox domain whose frame was pushed last, for the stall watchdog
    private readonly byte[]? _adminSecretHash;
    private int _adminSecretFailures;

    private (ObjectRef Ref, Type Type)? _pending;
    private ObjectRef? _master;
    private ObjectRef? _thisPlayer;
    private string _verb = "";
    private long _nextCloneId;
    private int _nextCallOutId;
    private DateTime _nextHeartBeat = DateTime.UtcNow + HeartBeatInterval;
    private bool _shutdown;

    private sealed record CallOutEntry(int Id, ObjectRef Owner, ObjectRef? Player, Action Function);

    private readonly DriverSettings _settings;

    public Kernel(string mudlibRoot, string saveRoot, string? adminSecret = null, DriverSettings? settings = null)
    {
        _settings = settings ?? new DriverSettings();
        if (!string.IsNullOrEmpty(adminSecret))
            _adminSecretHash = SHA256.HashData(Encoding.UTF8.GetBytes(adminSecret));
        _mudlibRoot = Path.GetFullPath(mudlibRoot).TrimEnd(Path.DirectorySeparatorChar);
        _compiler = new ProgramCompiler(mudlibRoot)
        {
            DomainOf = path => _master is { IsAlive: true } ? AskMaster(m => m.ObjectDomain(path, false)) ?? "" : "",
        };
        _compiler.CompileStarted = CompileStarted;
        _compiler.CompileEnded = CompileEnded;
        _saves = new SaveStore(saveRoot);
        _quarantineFile = Path.Combine(Path.GetFullPath(saveRoot), "sandbox_quarantine.txt");
        LoadQuarantine();
        DriverHost.Services = this;
        Runtime.FilterCheck = domain => _masterDepth == 0 && RestrictedCaller()?.Domain == domain;
    }

    private IMaster MasterObject => _master?.Target as IMaster ?? throw new MudRuntimeException("No master object");

    /// <summary>Ask the master object a question, with the master on the call stack.</summary>
    private T AskMaster<T>(Func<IMaster, T> question)
    {
        _masterDepth++;
        try { return In(_master!, () => question(MasterObject)); }
        finally { _masterDepth--; }
    }

    // ---- Call stack: which object's code is running, for PreviousObject() ----

    // Runtime.CurrentDomain always names the domain of the top frame: sandbox code checks it at its first statement (TickInjector).

    private void Push(ObjectRef ob)
    {
        _callStack.Add(ob);
        if (ob.Domain.Length > 0)
        {
            _restricted++;
            _lastRestricted = ob.Domain;
        }
        Runtime.CurrentDomain = ob.Domain;
    }

    private void Pop()
    {
        if (_callStack.Count > 0)
        {
            var ob = _callStack[^1];
            _callStack.RemoveAt(_callStack.Count - 1);
            if (ob.Domain.Length > 0 && _restricted > 0) _restricted--;
        }
        Runtime.CurrentDomain = _callStack.Count > 0 ? _callStack[^1].Domain : null;
    }

    private void ClearStack()
    {
        _callStack.Clear();
        _restricted = 0;
        _masterDepth = 0;
        Runtime.CurrentDomain = null;
    }

    private string TopDomain => _callStack.Count > 0 ? _callStack[^1].Domain : "";

    private void In(ObjectRef ob, Action work) => In<bool>(ob, () => { work(); return true; });

    /// <summary>
    /// Run work in ob's frame. Two rules make a domain boundary hold (docs/SANDBOX.md, "Sandbox code runs only in its own frames"):
    /// an exception leaves a frame of another domain only as a plain MudRuntimeException with its text, read here while ob's frame
    /// is still on top (an AggregateException or any other wrapper included, whatever its type); and sandbox code called while the
    /// master answers a question is restricted all the same.
    /// </summary>
    private T In<T>(ObjectRef ob, Func<T> work)
    {
        var below = TopDomain;
        var masterDepth = _masterDepth;
        var outermost = ob.Domain.Length > 0 && _restricted == 0;
        if (outermost) EnterDomain(ob.Domain);
        if (ob.Domain.Length > 0) _masterDepth = 0;
        Push(ob);
        try { return work(); }
        catch (Exception error) when (ob.Domain != below && !CrossesAsIs(error))
        {
            throw new MudRuntimeException(CrossingText(error, ob));
        }
        finally
        {
            Pop();
            _masterDepth = masterDepth;
            if (outermost) LeaveDomain(ob.Domain);
        }
    }

    // ---- Time spent in sandbox code (F1b of the November 2026 review; docs/SANDBOX.md, "A stall") ----
    // Every loop and body of sandbox code ticks, so its own work is bounded by the tick budget. Framework code it calls is not:
    // Enumerable.Range(0, 100000).SelectMany(_ => list).Count() runs ten billion steps with a hundred thousand ticks, and .NET
    // cannot stop a thread. Two limits: each sandbox domain may use the game thread for at most a share of every window (time
    // spent compiling not counted), after which entering its code is refused until the next window; and a watchdog thread stops
    // the driver if one step of the game loop runs longer than the stall limit with sandbox code on the stack, after writing that
    // domain into the quarantine file, so that the restarted driver does not load its code until an administrator reopens it.

    private static readonly long DomainWindow = System.Diagnostics.Stopwatch.Frequency * 2;  // two seconds
    private static readonly long DomainShare = System.Diagnostics.Stopwatch.Frequency;       // one second of each window
    private readonly Dictionary<string, (long Start, long Used)> _domainTime = new();
    private long _domainEnteredAt;
    private long _compileTimeAtEntry;
    private long _compileTime;         // total time spent compiling, so that a domain's share leaves it out
    private long _compileStarted;
    private int _compileDepth;
    private long _busySince;           // Stopwatch time at which the current step of the game loop began; 0 while waiting or compiling
    private readonly HashSet<string> _quarantined = new(StringComparer.Ordinal);
    private readonly string _quarantineFile;

    private void EnterDomain(string domain)
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var (start, used) = _domainTime.GetValueOrDefault(domain);
        if (now - start > DomainWindow) _domainTime[domain] = (now, 0);
        else if (used > DomainShare)
        {
            if (used < long.MaxValue / 2)
            {
                Log.Info($"sandbox: {domain} has used its share of time in this window: its code waits");
                _domainTime[domain] = (start, long.MaxValue / 2);
            }
            throw new SandboxBudgetException("This workshop's code has used its share of time. It runs again in a moment.");
        }
        _domainEnteredAt = now;
        _compileTimeAtEntry = _compileTime;
    }

    private void LeaveDomain(string domain)
    {
        var spent = System.Diagnostics.Stopwatch.GetTimestamp() - _domainEnteredAt - (_compileTime - _compileTimeAtEntry);
        var (start, used) = _domainTime.GetValueOrDefault(domain);
        if (used < long.MaxValue / 2) _domainTime[domain] = (start, used + Math.Max(0, spent));
    }

    private volatile string? _compilingDomain; // the sandbox domain whose program is being compiled (outermost compile), for the watchdog

    private void CompileStarted(string domain)
    {
        if (_compileDepth++ == 0)
        {
            _compileStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            _compilingDomain = domain.Length > 0 ? domain : null;
        }
        Volatile.Write(ref _busySince, 0); // a compile is the driver's own work: the stall clock waits (but see the watchdog)
    }

    private void CompileEnded()
    {
        if (--_compileDepth > 0) return;
        _compilingDomain = null;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        _compileTime += now - _compileStarted;
        if (_inStep) Volatile.Write(ref _busySince, now);
    }

    private bool _inStep;

    private void StepBegins()
    {
        _inStep = true;
        Volatile.Write(ref _busySince, System.Diagnostics.Stopwatch.GetTimestamp());
        _lastRestricted = null;
    }

    private void StepEnds()
    {
        _inStep = false;
        Volatile.Write(ref _busySince, 0);
    }

    /// <summary>Start the stall watchdog (Program does; a kernel made for a tool runs without one).</summary>
    public void StartWatchdog()
    {
        var limit = (long)(_settings.StallSeconds * System.Diagnostics.Stopwatch.Frequency);
        // Source written to make the compiler work for minutes is a stall too: a home's program may take six times the stall limit
        // (at least 30 seconds, so that a large file on a busy machine is not mistaken for one) to compile.
        var compileLimit = Math.Max(30 * System.Diagnostics.Stopwatch.Frequency, 6 * limit);
        var thread = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(100);
                if (_compilingDomain is { } compiling && System.Diagnostics.Stopwatch.GetTimestamp() - Volatile.Read(ref _compileStarted) > compileLimit)
                    Stall(compiling);
                var since = Volatile.Read(ref _busySince);
                if (since == 0 || System.Diagnostics.Stopwatch.GetTimestamp() - since < limit) continue;
                if (Volatile.Read(ref _restricted) == 0 || _lastRestricted is not { } domain) continue; // the game's own code: trusted
                Stall(domain);
            }
        }) { IsBackground = true, Name = "sandbox watchdog" };
        thread.Start();
    }

    /// <summary>The watchdog's last resort: close the domain for the next start and stop the process (the service restarts it).</summary>
    private void Stall(string domain)
    {
        try
        {
            File.AppendAllText(_quarantineFile, $"{domain} {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ} held the game thread for {_settings.StallSeconds:0.#}s\n");
        }
        catch (Exception) { }
        Log.Info($"sandbox: STALL: code of {domain} held the game thread for more than {_settings.StallSeconds:0.#}s. It is closed until an " +
                 $"administrator removes its line from {_quarantineFile}. Stopping the driver; the service restarts it.");
        Console.Out.Flush();
        Environment.Exit(3);
    }

    private void LoadQuarantine()
    {
        if (!File.Exists(_quarantineFile)) return;
        foreach (var line in File.ReadAllLines(_quarantineFile))
        {
            var word = line.Trim().Split(' ', 2)[0];
            if (word.Length > 0 && !word.StartsWith('#')) _quarantined.Add(word);
        }
        if (_quarantined.Count > 0) Log.Info($"sandbox: closed after a stall (see {_quarantineFile}): {string.Join(", ", _quarantined)}");
    }

    /// <summary>The driver's own sealed exceptions, whose text is a plain string, cross as they are; everything else is converted.</summary>
    private static bool CrossesAsIs(Exception error)
        => error is EvalCostExceededException or InsufficientExecutionStackException or RefusedException or SandboxFrameException;

    /// <summary>The text of an exception leaving ob's frame. From sandbox code: one line, without control characters, at most 300 characters.</summary>
    private static string CrossingText(Exception error, ObjectRef ob)
    {
        var text = SafeMessage(error);
        if (ob.Domain.Length == 0) return text;
        text = PlainText.Clean(text.Replace('\n', ' ').Replace('\r', ' '));
        return text.Length > 300 ? text[..300] + "..." : text;
    }

    // ---- Values that cross between domains (docs/SANDBOX.md) ----
    // A value made by one domain's code must not carry that code into another domain, where it would run with the other's
    // call stack: an object whose ToString, Equals or comparer is mudlib code, an exception whose Message is, an enumerator.
    // So arguments and results that cross go as plain data: scalars, strings, handles, and copies of arrays, lists, sets and
    // dictionaries (with the default comparer) of those. Anything else does not cross. An exception of a mudlib type crosses
    // as a plain MudRuntimeException with its message, read before the frame that threw it is left.

    private static bool IsMudlibType(Type type) => type.Assembly.GetName().Name?.StartsWith(ProgramCompiler.AssemblyPrefix, StringComparison.Ordinal) == true;

    private static string SafeMessage(Exception error)
    {
        try { return error.Message ?? "error"; }
        catch (Exception) { return error.GetType().Name; }
    }

    private static readonly HashSet<Type> PlainTypes = new()
    {
        typeof(string), typeof(bool), typeof(char), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(ObjectRef),
    };

    private static bool PlainElement(Type type, int depth = 0)
    {
        if (depth > 6) return false;
        if (PlainTypes.Contains(type) || type.IsEnum || type == typeof(object)) return true;
        if (type.IsArray) return PlainElement(type.GetElementType()!, depth + 1);
        if (!type.IsGenericType) return false;
        var definition = type.GetGenericTypeDefinition();
        // A value tuple or nullable is copied by value, so it may hold only scalars, strings, handles and enums: a list inside a tuple
        // would be shared between the domains, not copied (F4 of the November 2026 review).
        if (definition == typeof(Nullable<>) || (type.IsValueType && definition.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true))
            return type.GetGenericArguments().All(a => PlainTypes.Contains(a) || a.IsEnum);
        if (definition == typeof(List<>) || definition == typeof(HashSet<>) || definition == typeof(Dictionary<,>))
            return type.GetGenericArguments().All(a => PlainElement(a, depth + 1));
        return false;
    }

    /// <summary>
    /// A copy of a value that may cross between domains, or false if it may not cross. With clean (a value going from sandbox code to
    /// real code), every string in it loses the characters PlainText removes, so that no terminal control reaches a screen that way.
    /// </summary>
    private static bool TryCross(object? value, out object? copy, bool clean = false, int depth = 0)
    {
        copy = value;
        if (value == null) return true;
        if (depth > 6) return false;
        var type = value.GetType();
        if (type == typeof(string))
        {
            if (clean) copy = PlainText.Clean((string)value);
            return true;
        }
        if (PlainTypes.Contains(type) || type.IsEnum) return true;
        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            if (!PlainElement(element)) return false;
            var source = (Array)value;
            var array = Array.CreateInstance(element, source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                if (!TryCross(source.GetValue(i), out var item, clean, depth + 1)) return false;
                array.SetValue(item, i);
            }
            copy = array;
            return true;
        }
        if (!type.IsGenericType || !PlainElement(type)) return false;
        var definition = type.GetGenericTypeDefinition();
        if (type.IsValueType)
        {
            // A value tuple of scalars, strings, handles and enums (PlainElement refused anything else): its strings are cleaned.
            if (clean && value is System.Runtime.CompilerServices.ITuple tuple)
            {
                var parts = new object?[tuple.Length];
                var changed = false;
                for (var i = 0; i < tuple.Length; i++)
                {
                    parts[i] = tuple[i] is string text ? PlainText.Clean(text) : tuple[i];
                    changed |= !Equals(parts[i], tuple[i]);
                }
                if (changed) copy = Activator.CreateInstance(type, parts);
            }
            return true;
        }
        if (definition == typeof(List<>) || definition == typeof(HashSet<>))
        {
            var result = Activator.CreateInstance(type)!;
            var add = type.GetMethod("Add")!;
            foreach (var item in (System.Collections.IEnumerable)value)
            {
                if (!TryCross(item, out var plain, clean, depth + 1)) return false;
                add.Invoke(result, [plain]);
            }
            copy = result;
            return true;
        }
        if (definition == typeof(Dictionary<,>))
        {
            var result = (System.Collections.IDictionary)Activator.CreateInstance(type)!; // the default comparer, whatever the original had
            foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary)value)
            {
                if (!TryCross(entry.Key, out var key, clean, depth + 1) || key == null || !TryCross(entry.Value, out var item, clean, depth + 1)) return false;
                result[key] = item;
            }
            copy = result;
            return true;
        }
        return false;
    }

    // ---- Restricted code (docs/SANDBOX.md): objects with a domain, and the master's ValidCall ----

    /// <summary>
    /// Is restricted code on the call stack, outside the master's own answers? An execution for a restricted ThisPlayer (a sandbox
    /// creature's arrival offering Init to the things around it) counts too: real code running for a sandbox object acts for it.
    /// </summary>
    private bool Restricted => (_restricted > 0 || _thisPlayer is { Domain.Length: > 0 }) && _masterDepth == 0;

    /// <summary>
    /// May the running code reach this object in this way? Always yes unless restricted code is running; then yes for an object of
    /// the acting sandbox code's own domain, and otherwise (a real object, or another wizard's sandbox) as the master's ValidCall says.
    /// </summary>
    private bool Permit(ObjectRef target, string function, bool quiet = false)
    {
        if (!Restricted || _master is not { IsAlive: true }) return true;
        if (RestrictedCaller() is { } actor && target.Domain.Length > 0 && target.Domain == actor.Domain) return true;
        if (AskMaster(m => m.ValidCall(target, function))) return true;
        if (!quiet) NoteRefusal($"refused {function} on {target.Name} (restricted code: {RestrictedCaller()?.Name ?? "?"})");
        return false;
    }

    /// <summary>
    /// Text on its way to a listener. From real code it goes as it is. From restricted code it loses every terminal control
    /// (PlainText), and it goes as it is only where the master allows CatchTell (the wizard in their own workshop, sandbox objects);
    /// to a visitor in the workshop it goes quoted, every line marked with the workshop it came from, where the master allows
    /// CatchTellQuoted; to anyone else not at all.
    /// </summary>
    private string? TextFor(ObjectRef listener, string message)
    {
        if (!Restricted || _master is not { IsAlive: true }) return message;
        message = PlainText.Clean(message);
        if (Permit(listener, "CatchTell", quiet: true)) return message;
        if (Permit(listener, "CatchTellQuoted")) return Quoted(message, RestrictedCaller()?.Domain ?? "");
        return null;
    }

    private static string Quoted(string message, string domain)
    {
        var mark = "[" + (domain.StartsWith("wiz:", StringComparison.Ordinal) ? domain[4..] + "'s workshop" : domain) + "] ";
        var lines = message.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Length > 0) lines[i] = mark + lines[i];
        return string.Join('\n', lines);
    }

    /// <summary>A refusal goes to the log, a few per execution at most, so that a loop of refused calls cannot fill the disk.</summary>
    private void NoteRefusal(string text)
    {
        _refusals++;
        if (_refusals <= 3) Log.Info("sandbox: " + text);
        else if (_refusals == 4) Log.Info("sandbox: further refusals in this execution are not logged");
    }

    /// <summary>
    /// A delayed call, an action handler or an input handler runs later in its owner's frame, so it must be the owner's own code:
    /// a method or lambda of the owner's class or one of its base classes, bound to the owner (or to nothing, or to a closure).
    /// Otherwise one object could hand another a delegate and have it run later as the other.
    /// </summary>
    private static void RequireOwnCode(ObjectRef owner, Delegate function, string what)
    {
        if (function.Target is MudObject bound && !ReferenceEquals(bound, owner.Target))
            throw new MudRuntimeException($"{what}: {owner.Name} may only use its own code");
        var assembly = function.Method.DeclaringType?.Assembly;
        if (assembly == null || assembly == typeof(MudObject).Assembly) return;
        for (var t = owner.Target?.GetType(); t != null && t != typeof(MudObject); t = t.BaseType)
            if (t.Assembly == assembly) return;
        throw new MudRuntimeException($"{what}: {owner.Name} may only use its own code");
    }

    // =====================================================================
    // Game loop
    // =====================================================================

    /// <summary>Queue work for the game thread. Safe to call from any thread.</summary>
    public void Post(Action work) => _events.Add(work);

    public void Run()
    {
        Boot();
        while (!_shutdown)
        {
            var now = DateTime.UtcNow;
            var next = _nextHeartBeat;
            if (_callOuts.TryPeek(out _, out var due) && due < next) next = due;
            var wait = Math.Clamp((int)(next - now).TotalMilliseconds, 0, 1000);
            if (_events.TryTake(out var work, wait))
            {
                StepBegins();
                work();
            }
            StepBegins();
            RunTimers();
            StepEnds();
        }
        foreach (var connection in _connections.ToArray()) connection.Close();
        Log.Info("driver stopped");
    }

    private void Boot()
    {
        _master = LoadObject(MasterPath);
        if (_master.Target is not IMaster) throw new MudRuntimeException($"{MasterPath} must implement IMaster");
        foreach (var path in AskMaster(m => m.Preload().ToList()))
            Execute(null, () => LoadObject(path));
        Log.Info($"boot complete, {_objects.Count} objects loaded");
    }

    private void RunTimers()
    {
        RunDueCallOuts();
        if (DateTime.UtcNow >= _nextHeartBeat)
        {
            _nextHeartBeat = DateTime.UtcNow + HeartBeatInterval;
            foreach (var ob in _heartBeats.ToArray())
                if (ob is { IsAlive: true, HeartBeat: true })
                    Execute(ob.Living ? ob : null, () => In(ob, () => ob.Target?.HeartBeat()));
            RunDueCallOuts(); // what a heartbeat scheduled for now runs before the next line any player typed
        }
    }

    /// <summary>
    /// Run every call-out that is due, including one made for "now" by a call-out of this same pass (so that work scheduled with
    /// no delay, such as the restore of a character leaving the workshop, runs before the next network event). Bounded, so that a
    /// call-out that reschedules itself with no delay cannot stall the loop.
    /// </summary>
    private void RunDueCallOuts()
    {
        var budget = _callOuts.Count + 64;
        var now = DateTime.UtcNow;
        while (budget-- > 0 && _callOuts.TryPeek(out var entry, out var due) && due <= now)
        {
            _callOuts.Dequeue();
            if (_cancelledCallOuts.Remove(entry.Id) || !entry.Owner.IsAlive) continue;
            Execute(entry.Player is { IsAlive: true } ? entry.Player : null, () => In(entry.Owner, entry.Function));
            now = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Run one top-level execution thread: a command, a heartbeat, a call_out
    /// or a network event. Resets the tick budget, sets this_player, and makes
    /// sure no mudlib error escapes into the game loop.
    /// </summary>
    private void Execute(ObjectRef? player, Action work)
    {
        Runtime.Ticks = 0;
        _thisPlayer = player;
        _verb = "";
        ClearStack();
        _refusals = 0;
        Exception? failure = null;
        try
        {
            work();
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            _thisPlayer = null;
            _pending = null;
            ClearStack();
        }
        // Reported here, not inside the catch block: a catch handler still runs
        // on top of the faulting stack, which after runaway recursion is nearly full.
        if (failure is not null) ReportError(player, failure);
        FlushPrompts();
    }

    // ---- Prompts ----
    // After every line a player types the driver sends the owner's Prompt(). Output that arrives without a command (a combat round,
    // someone arriving) leaves the player looking at old text, so every connection that received output during an execution gets a
    // fresh prompt when it ends, if its owner is a player body that asks for it and is not in the middle of a question.

    private readonly HashSet<Connection> _owing = new();
    private Connection? _promptSkip; // the connection whose line is being handled: its prompt is sent when the line is done

    private void FlushPrompts()
    {
        if (_owing.Count == 0) return;
        foreach (var connection in _owing.ToArray())
        {
            if (connection == _promptSkip) continue;
            _owing.Remove(connection);
            if (connection.Owner is { IsAlive: true, InputTo: null } owner) SendPrompt(connection, owner, true);
        }
    }

    private void SendPrompt(Connection connection, ObjectRef owner, bool afterOutput)
    {
        string text;
        Runtime.Ticks = 0;
        _thisPlayer = owner;
        ClearStack();
        try
        {
            if (afterOutput && !In(owner, () => owner.Target?.PromptsAfterOutput() ?? false)) return;
            text = In(owner, () => owner.Target?.Prompt() ?? "> ") ?? "> ";
        }
        catch (Exception error)
        {
            Log.Info($"ERROR (prompt of {owner.Name}): {SafeMessage(error)}");
            text = "> ";
        }
        finally
        {
            _thisPlayer = null;
            ClearStack();
        }
        connection.Send(text);
        connection.AtPrompt = true;
    }

    private void ReportError(ObjectRef? player, Exception error)
    {
        // The call stack is empty here. Every exception that left a sandbox frame was converted on the way out (In), so this reads
        // only the driver's and real code's text; and should anything else get here, its getters are not trusted to succeed.
        string message;
        try { message = error is InsufficientExecutionStackException ? "Recursion too deep" : error.Message ?? "error"; }
        catch (Exception) { message = error.GetType().Name; }
        var text = message;
        if (error is not MudRuntimeException)
        {
            // Keep the innermost frames; a runaway recursion has thousands of identical ones.
            string trace;
            try { trace = error.StackTrace ?? ""; } catch (Exception) { trace = ""; }
            var frames = trace.Split('\n').Take(12).Select(f => f.TrimEnd());
            text = $"{error.GetType().Name}: {message}\n{string.Join("\n", frames)}";
        }
        // A move the master refused is policy at work, not a fault: it is logged as a refusal.
        if (error is not SandboxBudgetException)
            Log.Info(error is RefusedException ? $"refused ({player?.Name ?? "driver"}): {text}" : $"ERROR ({player?.Name ?? "driver"}): {text}");
        Runtime.Ticks = 0; // give the error handlers a budget of their own
        try
        {
            if (player is { IsAlive: true }) Receive(player, $"*** {message}\n");
            if (_master is { IsAlive: true }) AskMaster(m => { m.LogError(player?.Name ?? "driver", text); return true; });
        }
        catch (Exception nested)
        {
            Log.Info($"ERROR in error handler: {SafeMessage(nested)}");
        }
    }

    // =====================================================================
    // Network events (posted by TelnetServer, run on the game thread)
    // =====================================================================

    public void OnConnect(Connection connection)
    {
        _connections.Add(connection);
        connection.SendRaw(Gmcp.WillOffer); // the standard offer; a client that does not answer DO sees nothing else of it
        connection.LastInputMs = TimeMs();
        Execute(null, () =>
        {
            var login = AskMaster(m => m.Connect());
            if (!login.IsAlive) throw new MudRuntimeException("master.Connect() returned a dead object");
            login.Connection = connection;
            connection.Owner = login;
            _thisPlayer = login;
            CallOther(login, "Logon", []);
        });
        if (connection.Owner is null) connection.Close();
    }

    public void OnLine(Connection connection, string line)
    {
        if (connection.Owner is not { IsAlive: true } owner) return;
        if (connection.EchoSuppressed)
        {
            // The line was typed with echo off (a password): hand echo back to
            // the client and end the line the user could not see.
            connection.EchoSuppressed = false;
            connection.SendRaw(TelnetWontEcho);
            connection.Send("\n");
        }
        if (line.Trim().Length > 0) connection.LastInputMs = TimeMs(); // an empty line is a client keep-alive, not activity
        connection.AtPrompt = false; // the player pressed Enter: the cursor is on a fresh line
        _promptSkip = connection;
        Execute(owner, () =>
        {
            if (owner.InputTo is { } handler)
            {
                var inputOwner = owner.InputToOwner is { IsAlive: true } o ? o : owner;
                owner.InputTo = null;
                owner.InputToOwner = null;
                In(inputOwner, () => handler(line));
            }
            else
            {
                Command(owner, line);
            }
        });
        // The owner may have changed (Exec) or gone away (quit) during the command.
        _promptSkip = null;
        _owing.Remove(connection);
        if (connection.Owner is { IsAlive: true, InputTo: null } now) SendPrompt(connection, now, false);
        FlushPrompts(); // the other connections that received output while this line was handled
    }

    public void OnDisconnect(Connection connection)
    {
        _connections.Remove(connection);
        _owing.Remove(connection);
        if (connection.Owner is not { IsAlive: true } owner) return;
        connection.Owner = null;
        owner.Connection = null;
        Execute(owner, () => In(owner, () => owner.Target?.NetDead()));
    }

    private void Command(ObjectRef player, string line)
    {
        line = line.Trim();
        if (line.Length == 0) return;
        // A leading single quote is a command word by itself, so 'hello means the same as ' hello (say).
        if (line.Length > 1 && line[0] == '\'' && line[1] != ' ') line = "' " + line[1..];
        var space = line.IndexOf(' ');
        _verb = space < 0 ? line : line[..space];
        var args = space < 0 ? "" : line[(space + 1)..].Trim();
        player.NotifyFail = null;

        // Newest bindings first, so a room or item can override a built-in verb.
        var bindings = player.Actions.ToArray();
        for (var i = bindings.Length - 1; i >= 0; i--)
        {
            var binding = bindings[i];
            if (binding.Verb != _verb || !binding.Owner.IsAlive || !player.Actions.Contains(binding)) continue;
            if (In(binding.Owner, () => binding.Handler(args))) return;
            if (!player.IsAlive) return;
        }
        // Through CatchTell, like every other message, so that a player body can render colour tags in it.
        TellObject(player, (player.NotifyFail ?? "What?") + "\n");
    }

    // =====================================================================
    // IDriverServices: objects and programs
    // =====================================================================

    public ObjectRef ClaimPendingRef(MudObject instance)
    {
        if (_pending is not { } pending || pending.Type != instance.GetType())
            throw new MudRuntimeException("Mud objects cannot be created with 'new'; use CloneObject or LoadObject");
        _pending = null;
        pending.Ref.Target = instance;
        return pending.Ref;
    }

    public ObjectRef LoadObject(string path)
    {
        path = ProgramCompiler.NormalizePath(path);
        return _objects.TryGetValue(path, out var existing) ? existing : Instantiate(_compiler.Get(path), path, isClone: false);
    }

    public ObjectRef CloneObject(string path)
    {
        var program = _compiler.Get(path);
        return Instantiate(program, $"{program.Path}#{++_nextCloneId}", isClone: true);
    }

    /// <summary>
    /// A constructor or Create() may call EnableCommands(), which makes the new
    /// object ThisPlayer(). Whoever is creating it (a command, a room stocking
    /// itself) keeps their own ThisPlayer() for the rest of the execution.
    /// </summary>
    private ObjectRef Instantiate(MudProgram program, string name, bool isClone)
    {
        var creator = _thisPlayer;
        try
        {
            return InstantiateCore(program, name, isClone);
        }
        finally
        {
            _thisPlayer = creator;
        }
    }

    private ObjectRef InstantiateCore(MudProgram program, string name, bool isClone)
    {
        if (program.ObjectType is null)
            throw new MudRuntimeException($"{program.Path} is abstract: it can be inherited but not loaded or cloned");

        var handle = new ObjectRef(name, program.Path, isClone);
        // The domain comes first, from the cloner's call stack; then a blueprint is made with an empty stack, so that loading a
        // program behaves the same whoever happens to load it first.
        handle.Domain = _master is { IsAlive: true } ? AskMaster(m => m.ObjectDomain(program.Path, isClone)) ?? "" : "";
        if (_quarantined.Contains(handle.Domain))
            throw new MudRuntimeException($"This workshop ({handle.Domain}) is closed: its code held up the whole game. An administrator reopens it.");
        List<ObjectRef>? outer = null;
        var outerMasterDepth = _masterDepth;
        if (!isClone)
        {
            outer = new List<ObjectRef>(_callStack);
            ClearStack();
            _thisPlayer = null; // Instantiate gives the creator's back afterwards
        }
        try
        {
            _pending = (handle, program.ObjectType);
            var belowDomain = TopDomain;
            Push(handle);
            try
            {
                Activator.CreateInstance(program.ObjectType);
            }
            catch (TargetInvocationException wrapped) when (wrapped.InnerException is not null)
            {
                handle.Target = null;
                var inner = wrapped.InnerException;
                // A constructor's exception leaves the new object's frame (still on top here) like any other: converted, whatever its type.
                if (handle.Domain != belowDomain && !CrossesAsIs(inner))
                    throw new MudRuntimeException(CrossingText(inner, handle));
                throw inner;
            }
            finally
            {
                _pending = null;
                Pop();
            }

            _objects[name] = handle;
            try
            {
                In(handle, () => handle.Target!.Create());
            }
            catch
            {
                Destroy(handle);
                throw;
            }
            return handle;
        }
        finally
        {
            if (outer != null)
            {
                ClearStack();
                foreach (var ob in outer) Push(ob);
                _masterDepth = outerMasterDepth;
            }
        }
    }

    public ObjectRef? FindObject(string name)
    {
        name = name.Trim();
        if (!name.Contains('#')) name = ProgramCompiler.NormalizePath(name);
        return _objects.GetValueOrDefault(name);
    }

    public void Destruct(ObjectRef caller, ObjectRef target)
    {
        if (!target.IsAlive) return;
        if (caller != target && !AskMaster(m => m.ValidOperation("destruct", caller, target.Name)))
            throw new MudRuntimeException($"{caller.Name} may not destruct {target.Name}");
        if (target == _master) throw new MudRuntimeException("The master object cannot be destructed");
        // Real code destructing a sandbox object does not run its code (F3 of the November 2026 review): a restore, a quit or a
        // link death destructs what the workshop made as a step that must complete, and a throwing OnDestruct stopped it halfway.
        if (target.Domain.Length == 0 || Restricted) In(target, () => target.Target!.OnDestruct());
        Destroy(target);
    }

    private void Destroy(ObjectRef target)
    {
        if (!target.IsAlive) return;
        var environment = target.Environment;

        // Contents fall out into the environment, or are destroyed with the container (also when the master would not let them be
        // there). When real code destructs, sandbox contents are destroyed with it: moving them would run their code (OnMoved, Init)
        // in the middle of a destruct that must complete.
        var sandboxGoes = !Restricted;
        foreach (var item in target.Inventory.ToArray())
        {
            if (!item.IsAlive) continue;
            if (sandboxGoes && item.Domain.Length > 0) Destroy(item);
            else if (environment is { IsAlive: true } && MoveRefusalFor(item, environment, target) == null) MoveCore(item, environment);
            else Destroy(item);
        }

        if (environment is not null)
        {
            environment.Inventory.Remove(target);
            UnbindActionsOwnedBy(target, environment);
            target.Environment = null;
        }
        target.Actions.Clear();
        target.HeartBeat = false;
        _heartBeats.Remove(target);

        if (target.Connection is Connection connection)
        {
            connection.Owner = null;
            target.Connection = null;
            connection.Close();
        }

        target.Target = null; // every handle to the object is now dead
        _objects.Remove(target.Name);
    }

    public IReadOnlyList<string> UpdateProgram(ObjectRef caller, string path)
    {
        path = ProgramCompiler.NormalizePath(path);
        if (!AskMaster(m => m.ValidOperation("update", caller, path)))
            throw new MudRuntimeException($"{caller.Name} may not update {path}");
        var stale = _compiler.Invalidate(path);
        _compiler.Get(path); // recompile now so that errors reach whoever asked for the update
        return stale;
    }

    public object? CallOther(ObjectRef target, string function, object?[] args)
    {
        if (target.Target is not { } instance) return null;
        if (!Permit(target, function)) return null;
        var methods = _methodCache.GetOrCreateValue(instance.GetType());
        if (!methods.TryGetValue((function, args.Length), out var method))
        {
            method = instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == function && !m.IsGenericMethod && m.GetParameters().Length == args.Length);
            methods[(function, args.Length)] = method;
        }
        if (method is null) return null;
        // Across a domain boundary only plain data goes in and comes out (see TryCross).
        var from = TopDomain;
        var crossing = from != target.Domain;
        if (crossing)
        {
            var plain = new object?[args.Length];
            for (var i = 0; i < args.Length; i++)
                if (!TryCross(args[i], out plain[i], clean: from.Length > 0 && target.Domain.Length == 0))
                {
                    NoteRefusal($"refused {function} on {target.Name}: argument {i + 1} ({args[i]!.GetType().Name}) cannot cross between domains");
                    return null;
                }
            args = plain;
        }
        try
        {
            var result = In(target, () => method.Invoke(instance, BindingFlags.DoNotWrapExceptions, null, args, null));
            if (!crossing) return result;
            if (TryCross(result, out var copy, clean: target.Domain.Length > 0 && from.Length == 0)) return copy;
            NoteRefusal($"dropped the result of {function} on {target.Name} ({result!.GetType().Name}): it cannot cross between domains");
            return null;
        }
        catch (ArgumentException mismatch)
        {
            throw new MudRuntimeException($"Bad argument calling {function}() in {target.Name}: {mismatch.Message}");
        }
    }

    // =====================================================================
    // IDriverServices: containment
    // =====================================================================

    public void Move(ObjectRef caller, ObjectRef what, ObjectRef destination)
    {
        if (!what.IsAlive || !destination.IsAlive) throw new MudRuntimeException("Cannot move a destructed object");
        for (var e = destination; e is not null; e = e.Environment)
            if (e == what) throw new MudRuntimeException($"Cannot move {what.Name} into itself");
        if (MoveRefusalFor(what, destination, caller) is { } refusal) throw new RefusedException(refusal);
        MoveCore(what, destination);
    }

    /// <summary>The master's reason to refuse a move, or null. Not asked while the master itself is answering.</summary>
    private string? MoveRefusalFor(ObjectRef what, ObjectRef destination, ObjectRef caller)
        => _master is { IsAlive: true } && _masterDepth == 0 ? AskMaster(m => m.MoveRefusal(what, destination, caller)) : null;

    private void MoveCore(ObjectRef what, ObjectRef destination)
    {
        var from = what.Environment;
        // Leave the old environment and drop every command binding that depended on being there.
        if (what.Environment is { } old)
        {
            old.Inventory.Remove(what);
            if (what.Living) what.Actions.RemoveAll(a => a.Owner != what && a.Owner.Environment != what);
            UnbindActionsOwnedBy(what, old);
        }

        what.Environment = destination;
        destination.Inventory.Add(what);

        // Offer commands in both directions, LPmud style.
        var saved = _thisPlayer;
        try
        {
            bool StillThere() => what.IsAlive && what.Environment == destination && destination.IsAlive;

            // The object hears of its own move first, before anything in its new place can offer it commands.
            In(what, () => what.Target?.OnMoved(from));
            if (!StillThere()) return;

            if (what.Living)
            {
                _thisPlayer = what;
                In(destination, () => destination.Target?.Init());
            }
            foreach (var other in destination.Inventory.ToArray())
            {
                if (!StillThere()) return;
                if (other == what || !other.IsAlive || other.Environment != destination) continue;
                if (what.Living)
                {
                    _thisPlayer = what;
                    In(other, () => other.Target?.Init());
                }
                if (other.Living && StillThere())
                {
                    _thisPlayer = other;
                    In(what, () => what.Target?.Init());
                }
            }
            if (destination.Living && StillThere())
            {
                _thisPlayer = destination;
                In(what, () => what.Target?.Init());
            }
        }
        finally
        {
            _thisPlayer = saved;
        }
    }

    private static void UnbindActionsOwnedBy(ObjectRef owner, ObjectRef environment)
    {
        if (environment.Living) environment.Actions.RemoveAll(a => a.Owner == owner);
        foreach (var neighbour in environment.Inventory)
            if (neighbour.Living) neighbour.Actions.RemoveAll(a => a.Owner == owner);
    }

    public ObjectRef? Present(string id, ObjectRef container)
    {
        id = id.Trim().ToLowerInvariant();
        if (id.Length == 0) return null;
        var contents = container.Inventory.ToArray();
        foreach (var item in contents)
            if (item.Target is { } target && In(item, () => target.Id(id))) return item;
        // Nothing answers to the whole text: it may carry a position among identical things ("rat 2", "2.rat", "second rat").
        var position = NameIndex.Split(id, out var name);
        if (position == 0) return null;
        var seen = 0;
        foreach (var item in contents)
            if (item.Target is { } target && In(item, () => target.Id(name)) && ++seen == position) return item;
        return null;
    }

    // =====================================================================
    // IDriverServices: living and interactive objects
    // =====================================================================

    public ObjectRef? PreviousObject(ObjectRef self)
    {
        // The top frame is the object whose public method or apply the driver
        // entered last. If that is the asker, its caller is one frame down.
        // If it is not, the asker was reached by a direct C# call from the top
        // frame's code, so the top frame is the caller.
        var top = _callStack.Count - 1;
        if (top < 0) return null;
        if (_callStack[top] != self) return _callStack[top];
        return top >= 1 ? _callStack[top - 1] : null;
    }

    public IReadOnlyList<ObjectRef> CallerChain() => _callStack.ToArray();

    /// <summary>The restricted object nearest the top of the call stack (the sandbox code acting most directly), or a restricted ThisPlayer.</summary>
    public ObjectRef? RestrictedCaller()
    {
        if (_restricted > 0)
            for (var i = _callStack.Count - 1; i >= 0; i--) if (_callStack[i].Domain.Length > 0) return _callStack[i];
        return _thisPlayer is { Domain.Length: > 0 } player ? player : null;
    }

    /// <summary>A typed view is a direct reference: only within one domain, and from restricted code only as the master allows.</summary>
    public bool MayView(ObjectRef target)
    {
        if (_masterDepth > 0 || target.Domain == TopDomain) return Permit(target, "As");
        return target.Domain.Length == 0 && Permit(target, "As");
    }

    public ObjectRef? ThisPlayer => _thisPlayer;
    public string Verb => _verb;

    public void EnableCommands(ObjectRef ob)
    {
        ob.Living = true;
        _thisPlayer = ob;
    }

    public IReadOnlyList<ObjectRef> Users()
        => _connections.Where(c => c.Owner is { IsAlive: true }).Select(c => c.Owner!).ToArray();

    public void Exec(ObjectRef caller, ObjectRef to, ObjectRef from)
    {
        if (!AskMaster(m => m.ValidOperation("exec", caller, to.Name)))
            throw new MudRuntimeException($"{caller.Name} may not exec");
        if (!to.IsAlive || from.Connection is not Connection connection)
            throw new MudRuntimeException("exec: source is not interactive or target is dead");
        if (to.Connection is not null) throw new MudRuntimeException("exec: target is already interactive");
        from.Connection = null;
        from.InputTo = null;
        from.InputToOwner = null;
        to.Connection = connection;
        connection.Owner = to;
        if (_thisPlayer == from) _thisPlayer = to;
    }

    public void AddAction(ObjectRef owner, string verb, ActionHandler handler)
    {
        if (_thisPlayer is not { IsAlive: true, Living: true } player) return;
        RequireOwnCode(owner, handler, "AddAction");
        // A verb the living binds for itself (look, cat, tell, ...) is asked about as such: the master lets sandbox code shadow one
        // only for the wizard in their own workshop, never for a visitor (P3a of the November 2026 review).
        var own = Restricted && player.Actions.Any(a => a.Owner == player && a.Verb == verb);
        if (!Permit(player, (own ? "OwnVerb:" : "AddAction:") + verb)) return;
        player.Actions.RemoveAll(a => a.Owner == owner && a.Verb == verb);
        player.Actions.Add(new ActionBinding(verb, owner, handler));
    }

    public void NotifyFail(string message)
    {
        // The driver prints it after the command, outside every frame: what sandbox code sets is passed through TextFor now.
        if (_thisPlayer is not null && TextFor(_thisPlayer, message) is { } text) _thisPlayer.NotifyFail = text.TrimEnd('\n');
    }

    // Telnet IAC WILL ECHO / IAC WONT ECHO. "Will echo" makes the client stop
    // echoing locally; the driver never actually echoes, so the line is hidden.
    private static readonly byte[] TelnetWillEcho = [255, 251, 1];
    private static readonly byte[] TelnetWontEcho = [255, 252, 1];

    public void InputTo(ObjectRef caller, Action<string> handler, bool noEcho)
    {
        var target = _thisPlayer ?? caller;
        RequireOwnCode(caller, handler, "InputTo");
        if (!Permit(target, noEcho ? "InputToNoEcho" : "InputTo")) return;
        if (target.Connection is not Connection connection) throw new MudRuntimeException("InputTo: no interactive player");
        target.InputTo = handler;
        target.InputToOwner = caller;
        if (noEcho && !connection.EchoSuppressed)
        {
            connection.EchoSuppressed = true;
            connection.SendRaw(TelnetWillEcho);
        }
    }

    // =====================================================================
    // IDriverServices: communication
    // =====================================================================

    public void Receive(ObjectRef ob, string message)
    {
        if (ob.Connection is not Connection connection) return;
        if (connection.AtPrompt && message.Length > 0)
        {
            message = "\n" + message; // the player is looking at a prompt: start the news on a new line
            connection.AtPrompt = false;
        }
        connection.Send(message);
        _owing.Add(connection);
    }

    public void Write(string message)
    {
        if (_thisPlayer is { IsAlive: true } player) { if (TextFor(player, message) is { } text) In(player, () => player.Target!.CatchTell(text)); }
        else Log.Info($"write: {PlainText.Clean(message).TrimEnd()}");
    }

    public void TellObject(ObjectRef ob, string message)
    {
        if (TextFor(ob, message) is { } text) In(ob, () => ob.Target?.CatchTell(text));
    }

    public void TellRoom(ObjectRef room, string message, ObjectRef[] exclude)
    {
        foreach (var listener in room.Inventory.ToArray())
            if (listener.Living && Array.IndexOf(exclude, listener) < 0 && TextFor(listener, message) is { } text)
                In(listener, () => listener.Target?.CatchTell(text));
    }

    public void Say(ObjectRef caller, string message)
    {
        var speaker = _thisPlayer ?? caller;
        if (speaker.Environment is { } room) TellRoom(room, message, [speaker]);
    }

    // =====================================================================
    // IDriverServices: time
    // =====================================================================

    public long TimeMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public int IdleSeconds(ObjectRef ob) => ob.Connection is Connection c ? (int)Math.Max(0, (TimeMs() - c.LastInputMs) / 1000) : 0;

    public int IdleLimitSeconds() => (int)Math.Ceiling(_settings.IdleSeconds);

    public int TimeoutSeconds() => (int)Math.Ceiling(_settings.TimeoutSeconds);

    public int CallOut(ObjectRef owner, Action function, double delaySeconds)
    {
        RequireOwnCode(owner, function, "CallOut");
        var id = ++_nextCallOutId;
        var due = DateTime.UtcNow + TimeSpan.FromSeconds(Math.Max(0, delaySeconds));
        _callOuts.Enqueue(new CallOutEntry(id, owner, _thisPlayer, function), due);
        return id;
    }

    public bool RemoveCallOut(ObjectRef owner, int id)
    {
        var exists = _callOuts.UnorderedItems.Any(e => e.Element.Id == id && e.Element.Owner == owner);
        return exists && _cancelledCallOuts.Add(id);
    }

    public void SetHeartBeat(ObjectRef ob, bool on)
    {
        ob.HeartBeat = on;
        if (on && !_heartBeats.Contains(ob)) _heartBeats.Add(ob);
        if (!on) _heartBeats.Remove(ob);
    }

    // =====================================================================
    // IDriverServices: persistence and administration
    // =====================================================================

    public bool SaveObject(ObjectRef caller, string path)
    {
        path = ProgramCompiler.NormalizePath(path);
        if (!AskMaster(m => m.ValidWrite(path, caller))) throw new MudRuntimeException($"{caller.Name} may not write {path}");
        _saves.Save(caller.Target!, path);
        return true;
    }

    public bool RestoreObject(ObjectRef caller, string path)
    {
        path = ProgramCompiler.NormalizePath(path);
        if (!AskMaster(m => m.ValidRead(path, caller))) throw new MudRuntimeException($"{caller.Name} may not read {path}");
        return _saves.Restore(caller.Target!, path);
    }

    // ---- Mudlib file access ----

    private const int MaxFileBytes = 256 * 1024;

    /// <summary>
    /// Check a mudlib path, ask the master, and map it to the real file system.
    /// Accepts only "/" or "/a/b": absolute, no "." or ".." segments, no hidden
    /// names, so the result cannot lie outside the mudlib root.
    /// </summary>
    private (string MudPath, string FullPath, string[] Segments) ResolveMudlibPath(ObjectRef caller, string operation, string path)
    {
        path = path.Trim();
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (!path.StartsWith('/') || segments.Any(s => s.StartsWith('.') || s.EndsWith('.') || s.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || s.IndexOfAny(['\\', ':', '*', '?', '"', '<', '>', '|']) >= 0))
            throw new MudRuntimeException($"Illegal path '{path}'");
        var mudPath = "/" + string.Join('/', segments);
        if (!AskMaster(m => m.ValidOperation(operation, caller, mudPath)))
            throw new MudRuntimeException($"{caller.Name} may not {operation} {mudPath}");
        return (mudPath, Path.Combine([_mudlibRoot, .. segments]), segments);
    }

    public IReadOnlyList<DirEntry>? GetDir(ObjectRef caller, string path)
    {
        var (_, fullPath, segments) = ResolveMudlibPath(caller, "get-dir", path);
        var directory = new DirectoryInfo(fullPath);
        if (!directory.Exists) return null;

        // Hide dot-files and the build output that `dotnet build` leaves beside Mudlib.csproj.
        var directories = directory.GetDirectories()
            .Where(d => !d.Name.StartsWith('.') && !(segments.Length == 0 && d.Name is "bin" or "obj"))
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .Select(d => new DirEntry(d.Name, true, 0) { IsLink = d.LinkTarget != null });
        var files = directory.GetFiles()
            .Where(f => !f.Name.StartsWith('.'))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => new DirEntry(f.Name, false, f.Length) { IsLink = f.LinkTarget != null });
        return directories.Concat(files).ToList();
    }

    public string? ReadFile(ObjectRef caller, string path)
    {
        var (mudPath, fullPath, _) = ResolveMudlibPath(caller, "read-file", path);
        var file = new FileInfo(fullPath);
        if (!file.Exists) return null;
        if (file.Length > MaxFileBytes) throw new MudRuntimeException($"{mudPath} is too large to read ({file.Length} bytes)");
        return File.ReadAllText(fullPath);
    }

    public void WriteFile(ObjectRef caller, string path, string content)
    {
        var (mudPath, fullPath, segments) = ResolveMudlibPath(caller, "write-file", path);
        if (segments.Length == 0 || Directory.Exists(fullPath)) throw new MudRuntimeException($"{mudPath} is a directory");
        if (!Directory.Exists(Path.GetDirectoryName(fullPath))) throw new MudRuntimeException($"No such directory for {mudPath}");
        if (Encoding.UTF8.GetByteCount(content) > MaxFileBytes) throw new MudRuntimeException($"Content for {mudPath} is too large");

        // The temporary name starts with a dot, so it is invisible to GetDir and cannot be addressed.
        var temp = Path.Combine(Path.GetDirectoryName(fullPath)!, $".{segments[^1]}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            File.Move(temp, fullPath, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            try { File.Delete(temp); } catch (Exception) { }
            throw new MudRuntimeException($"Could not write {mudPath}"); // a read-only mudlib (DEPLOY.md, MUDLIB_WRITABLE) and the like
        }
        Log.Info($"file written: {mudPath} by {caller.Name} ({content.Length} chars)");
    }

    public bool CopyFile(ObjectRef caller, string from, string to)
    {
        var source = ResolveMudlibPath(caller, "read-file", from);
        var target = ResolveMudlibPath(caller, "write-file", to);
        if (!CheckFileTransfer(source, target)) return false;
        var length = new FileInfo(source.FullPath).Length;
        if (length > MaxFileBytes) throw new MudRuntimeException($"{source.MudPath} is too large to copy ({length} bytes)");

        // Copy under a hidden temporary name, then move it into place without
        // overwriting, so readers never see half a file.
        var temp = Path.Combine(Path.GetDirectoryName(target.FullPath)!, $".{target.Segments[^1]}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.Copy(source.FullPath, temp);
            File.Move(temp, target.FullPath, overwrite: false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            File.Delete(temp);
            throw new MudRuntimeException($"Could not copy {source.MudPath} to {target.MudPath}");
        }
        Log.Info($"file copied: {source.MudPath} to {target.MudPath} by {caller.Name} ({length} bytes)");
        return true;
    }

    public bool RenameFile(ObjectRef caller, string from, string to)
    {
        var source = ResolveMudlibPath(caller, "write-file", from);
        var target = ResolveMudlibPath(caller, "write-file", to);
        if (!CheckFileTransfer(source, target)) return false;
        try
        {
            File.Move(source.FullPath, target.FullPath, overwrite: false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new MudRuntimeException($"Could not move {source.MudPath} to {target.MudPath}");
        }
        Log.Info($"file renamed: {source.MudPath} to {target.MudPath} by {caller.Name}");
        return true;
    }

    /// <summary>
    /// The checks shared by CopyFile and RenameFile. False if the source does
    /// not exist; throws for every other problem. Never allows an overwrite.
    /// </summary>
    private static bool CheckFileTransfer((string MudPath, string FullPath, string[] Segments) source, (string MudPath, string FullPath, string[] Segments) target)
    {
        if (source.MudPath == target.MudPath) throw new MudRuntimeException($"{source.MudPath} and {target.MudPath} are the same file");
        if (source.Segments.Length == 0 || Directory.Exists(source.FullPath)) throw new MudRuntimeException($"{source.MudPath} is a directory");
        if (!File.Exists(source.FullPath)) return false;
        if (target.Segments.Length == 0 || File.Exists(target.FullPath) || Directory.Exists(target.FullPath))
            throw new MudRuntimeException($"{target.MudPath} already exists");
        if (!Directory.Exists(Path.GetDirectoryName(target.FullPath))) throw new MudRuntimeException($"No such directory for {target.MudPath}");
        return true;
    }

    public bool RemoveFile(ObjectRef caller, string path)
    {
        var (mudPath, fullPath, segments) = ResolveMudlibPath(caller, "remove-file", path);
        if (segments.Length == 0) throw new MudRuntimeException("/ is the mudlib root and cannot be removed");
        RefuseLinks(mudPath, segments);
        if (Directory.Exists(fullPath)) throw new MudRuntimeException($"{mudPath} is a directory");
        if (!File.Exists(fullPath)) return false;
        try
        {
            File.Delete(fullPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new MudRuntimeException($"Could not remove {mudPath}");
        }
        Log.Info($"file removed: {mudPath} by {caller.Name}");
        return true;
    }

    public bool RemoveDir(ObjectRef caller, string path)
    {
        var (mudPath, fullPath, segments) = ResolveMudlibPath(caller, "remove-dir", path);
        if (segments.Length == 0) throw new MudRuntimeException("/ is the mudlib root and cannot be removed");
        RefuseLinks(mudPath, segments);
        if (File.Exists(fullPath)) throw new MudRuntimeException($"{mudPath} is not a directory");
        if (!Directory.Exists(fullPath)) return false;
        try
        {
            // Empty means empty on disk. Hidden entries (dot-files, the temporary
            // files of WriteFile and CopyFile) count, though GetDir does not show them.
            var entries = Directory.EnumerateFileSystemEntries(fullPath).Select(Path.GetFileName).ToList();
            if (entries.Count > 0)
                throw new MudRuntimeException(entries.All(e => e!.StartsWith('.'))
                    ? $"{mudPath} is not empty (it holds hidden files)"
                    : $"{mudPath} is not empty");
            // Never recursive: this fails rather than remove anything inside.
            Directory.Delete(fullPath, recursive: false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new MudRuntimeException($"Could not remove {mudPath}");
        }
        Log.Info($"directory removed: {mudPath} by {caller.Name}");
        return true;
    }

    /// <summary>
    /// Refuse a path any part of which, below the mudlib root, is a symbolic
    /// link or other reparse point, so that a removal can neither follow a link
    /// out of the mudlib nor remove the link itself.
    /// </summary>
    private void RefuseLinks(string mudPath, string[] segments)
    {
        var current = _mudlibRoot;
        foreach (var segment in segments)
        {
            current = Path.Combine(current, segment);
            var info = new FileInfo(current);
            // LinkTarget is set for a link even when what it points to is missing.
            if (info.LinkTarget != null) throw new MudRuntimeException($"{mudPath} goes through a symbolic link and cannot be removed");
            if (!info.Exists && !Directory.Exists(current)) return; // nothing further down exists either
            if (info.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new MudRuntimeException($"{mudPath} goes through a symbolic link and cannot be removed");
        }
    }

    public bool MakeDir(ObjectRef caller, string path)
    {
        var (mudPath, fullPath, segments) = ResolveMudlibPath(caller, "make-dir", path);
        if (segments.Length == 0 || Directory.Exists(fullPath) || File.Exists(fullPath)) return false;
        if (!Directory.Exists(Path.GetDirectoryName(fullPath))) throw new MudRuntimeException($"No such parent directory for {mudPath}");
        try { Directory.CreateDirectory(fullPath); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { throw new MudRuntimeException($"Could not create {mudPath}"); }
        Log.Info($"directory created: {mudPath} by {caller.Name}");
        return true;
    }

    private const int MaxAdminSecretFailures = 5;

    public bool CheckAdminSecret(ObjectRef caller, string candidate)
    {
        if (!AskMaster(m => m.ValidOperation("admin-secret", caller, null)))
            throw new MudRuntimeException($"{caller.Name} may not check the admin secret");
        if (_adminSecretHash is null || _adminSecretFailures >= MaxAdminSecretFailures) return false;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(candidate));
        if (CryptographicOperations.FixedTimeEquals(hash, _adminSecretHash))
        {
            Log.Info($"admin secret accepted (asked by {caller.Name}, player {_thisPlayer?.Name ?? "none"})");
            return true;
        }
        _adminSecretFailures++;
        Log.Info($"admin secret REJECTED, attempt {_adminSecretFailures} of {MaxAdminSecretFailures} (player {_thisPlayer?.Name ?? "none"})");
        if (_adminSecretFailures >= MaxAdminSecretFailures)
            Log.Info("admin secret disabled until the driver is restarted");
        return false;
    }

    // ---- Passwords ----
    // Nothing here logs, and no exception message is built from the password
    // or the hash: a failure inside the hash function is replaced by a fixed message.

    private const string PasswordScheme = "pbkdf2-sha256";
    private const int PasswordIterations = 210_000;
    private const int MaxPasswordIterations = 10_000_000; // bounds the cost of a doctored hash
    private const int PasswordSaltBytes = 16;
    private const int PasswordHashBytes = 32;

    public string HashPassword(string password)
    {
        if (password is null) throw new MudRuntimeException("HashPassword: no password given");
        var salt = RandomNumberGenerator.GetBytes(PasswordSaltBytes);
        byte[] hash;
        try
        {
            hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, PasswordHashBytes);
        }
        catch (Exception)
        {
            // An encoder error message can quote a character of the password.
            throw new MudRuntimeException("HashPassword: the password could not be hashed");
        }
        return string.Join('$', PasswordScheme, PasswordIterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public string Sha256(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    public bool VerifyPassword(string password, string hash)
    {
        if (password is null || string.IsNullOrEmpty(hash)) return false;
        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != PasswordScheme) return false;
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations < 1 || iterations > MaxPasswordIterations) return false;
        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }
        if (salt.Length == 0 || expected.Length < 16 || expected.Length > 64) return false;
        try
        {
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string Status()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var (current, staleAlive, unloaded) = _compiler.Statistics();
        var text = new StringBuilder();
        text.AppendLine($"Objects:      {_objects.Count} ({_objects.Values.Count(o => o.IsClone)} clones)");
        text.AppendLine($"Users:        {Users().Count}");
        text.AppendLine($"Heartbeats:   {_heartBeats.Count}   Call-outs pending: {_callOuts.Count}");
        text.AppendLine($"Programs:     {current} current, {staleAlive} old versions still in use, {unloaded} old versions unloaded");
        text.AppendLine($"Memory:       {Environment.WorkingSet / 1048576} MB working set, {GC.GetTotalMemory(false) / 1048576.0:F1} MB managed heap");
        return text.ToString();
    }

    public void Shutdown(ObjectRef caller)
    {
        if (!AskMaster(m => m.ValidOperation("shutdown", caller, null)))
            throw new MudRuntimeException($"{caller.Name} may not shut the driver down");
        Log.Info($"shutdown requested by {caller.Name}");
        _shutdown = true;
    }
}
