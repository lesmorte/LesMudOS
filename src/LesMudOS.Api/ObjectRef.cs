namespace LesMudOS.Api;

/// <summary>
/// A handle to a mud object. Mudlib code stores and passes handles, never the
/// underlying <see cref="MudObject"/> instance.
///
/// There is exactly one handle per object, so reference equality is object
/// identity. When the object is destructed the handle goes dead everywhere at
/// once (<see cref="IsAlive"/> becomes false), which is the C# equivalent of
/// an LPC object variable turning into 0.
/// </summary>
public sealed class ObjectRef
{
    internal ObjectRef(string name, string programPath, bool isClone)
    {
        Name = name;
        ProgramPath = programPath;
        IsClone = isClone;
    }

    /// <summary>Object name: "/domains/town/square" for a blueprint, "/std/item#12" for a clone.</summary>
    public string Name { get; }

    /// <summary>Mudlib path of the source file this object was compiled from, without ".cs".</summary>
    public string ProgramPath { get; }

    public bool IsClone { get; }

    /// <summary>False once the object has been destructed.</summary>
    public bool IsAlive => Target is not null;

    /// <summary>
    /// call_other: invoke a public method by name. Returns null if the object
    /// is dead or has no public method of that name and arity, as LPC returns 0.
    /// This is the reload-safe way to talk to another object, because it does
    /// not depend on the compile-time type of the target.
    /// </summary>
    public object? Call(string function, params object?[] args)
        => DriverHost.Services.CallOther(this, function, args);

    /// <summary>call_other with a typed result. A missing function yields default(T).</summary>
    public T? Call<T>(string function, params object?[] args)
        => DriverHost.Services.CallOther(this, function, args) is T value ? value : default;

    /// <summary>
    /// Typed view of the object, or null if it is dead or not a T. Only safe
    /// for interfaces in LesMudOS.Api and for types the calling file inherits
    /// or uses; after the target's program is updated its type is a different
    /// .NET type and this returns null. Do not store the result in a field.
    /// </summary>
    public T? As<T>() where T : class => Target as T;

    public override string ToString() => Name;

    // ---- Kernel state. Invisible to mudlib code. ----
    internal MudObject? Target;
    internal ObjectRef? Environment;
    internal readonly List<ObjectRef> Inventory = new();
    internal bool Living;
    internal bool HeartBeat;
    internal object? Connection;
    internal readonly List<ActionBinding> Actions = new();
    internal Action<string>? InputTo;
    internal ObjectRef? InputToOwner;
    internal string? NotifyFail;
}

/// <summary>Handler for a command verb bound with AddAction. Return true if the command was handled.</summary>
public delegate bool ActionHandler(string args);

internal sealed record ActionBinding(string Verb, ObjectRef Owner, ActionHandler Handler);
