/// <summary>
/// The register of doors (docs/specs/sneak.md, "Doors"; docs/BUILDING.md, "A room with a door").
///
/// A door has two halves, one in each of two rooms, and one state: open or closed, locked or not. The state lives here and nowhere else,
/// so neither room can ever disagree with the other: opening the door from one side is changing the one record both read. A room that is
/// reloaded, or reset, asks again and finds the record as it was; a reset closes the door for both. Only if this daemon itself is reloaded
/// is a record lost, and then each door is re-created in the state its room declares the first time a room asks.
///
/// A door is named by the two rooms it joins (their program paths, in order of the alphabet, joined by "|"): the same name from either
/// side, so two rooms can share only one door. Every call is made by a room about its own door and is checked here:
///  - the caller (PreviousObject) must be a loaded room, not a clone, and must be one of the two rooms named by the key;
///  - so no other object (a wizard's thing, a creature, a player) can open, close, lock, unlock or reset any door, and a room under /wiz
///    can touch only the doors that have it as one end, never a door of the game's rooms.
/// Nothing here is saved. Blueprint only.
/// </summary>
public class Doors : MudObject
{
    /// <summary>Bit 1 in the answers: the door is open. Bit 2: it is locked.</summary>
    public const int OpenBit = 1;
    public const int LockedBit = 2;

    private readonly Dictionary<string, int> state = new();

    /// <summary>The key of the door between two rooms: the same from either side.</summary>
    public static string KeyOf(string roomA, string roomB) =>
        string.CompareOrdinal(roomA, roomB) <= 0 ? roomA + "|" + roomB : roomB + "|" + roomA;

    private bool FromEnd(string key)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller.IsClone || !caller.IsAlive || key == null) return false;
        var parts = key.Split('|');
        return parts.Length == 2 && (parts[0] == caller.ProgramPath || parts[1] == caller.ProgramPath) && parts[0] != parts[1];
    }

    /// <summary>
    /// The state of a door: bit 1 open, bit 2 locked; -1 if the caller is not one of its two rooms. A door nobody has asked about before is
    /// made now in the state the asking room declares (startOpen, startLocked); a door that exists keeps its state whatever the room says.
    /// </summary>
    public int State(string key, bool startOpen, bool startLocked)
    {
        if (!FromEnd(key)) return -1;
        if (!state.TryGetValue(key, out var value))
        {
            value = (startOpen && !startLocked ? OpenBit : 0) | (startLocked ? LockedBit : 0);
            state[key] = value;
        }
        return value;
    }

    private int Current(string key) => state.TryGetValue(key, out var v) ? v : -1;

    /// <summary>Open the door. False if it is locked or already open (or the caller is not one of its rooms, or the door is unknown).</summary>
    public bool Open(string key)
    {
        if (!FromEnd(key)) return false;
        var v = Current(key);
        if (v < 0 || (v & LockedBit) != 0 || (v & OpenBit) != 0) return false;
        state[key] = v | OpenBit;
        return true;
    }

    /// <summary>Close the door. False if it is already closed.</summary>
    public bool Close(string key)
    {
        if (!FromEnd(key)) return false;
        var v = Current(key);
        if (v < 0 || (v & OpenBit) == 0) return false;
        state[key] = v & ~OpenBit;
        return true;
    }

    /// <summary>Lock a closed door. False if it is open or already locked.</summary>
    public bool Lock(string key)
    {
        if (!FromEnd(key)) return false;
        var v = Current(key);
        if (v < 0 || (v & OpenBit) != 0 || (v & LockedBit) != 0) return false;
        state[key] = v | LockedBit;
        return true;
    }

    /// <summary>Unlock a locked door. False if it is not locked.</summary>
    public bool Unlock(string key)
    {
        if (!FromEnd(key)) return false;
        var v = Current(key);
        if (v < 0 || (v & LockedBit) == 0) return false;
        state[key] = v & ~LockedBit;
        return true;
    }

    /// <summary>
    /// A room's reset closes the door (and locks it again if relock is true). Returns true if the door was open, that is, if it has just
    /// been shut: the room tells both sides. Called from the room's own timed reset, never from another object.
    /// </summary>
    public bool Reset(string key, bool relock)
    {
        if (!FromEnd(key)) return false;
        var v = Current(key);
        if (v < 0) return false;
        var wasOpen = (v & OpenBit) != 0;
        state[key] = (v & ~OpenBit) | (relock ? LockedBit : (v & LockedBit));
        return wasOpen;
    }

    /// <summary>How many doors are on record (for the wizards' tools).</summary>
    public int QueryDoors() => state.Count;

    /// <summary>The state of a door, read by a wizard's tool or a test; -1 if unknown. Reading changes nothing.</summary>
    public int QueryState(string key) => key != null && state.TryGetValue(key, out var v) ? v : -1;
}
