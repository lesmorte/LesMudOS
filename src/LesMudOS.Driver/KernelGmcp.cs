using LesMudOS.Api;
using LesMudOS.Driver.Net;

namespace LesMudOS.Driver;

// GMCP efuns (docs/specs/gmcp.md, docs/DRIVER_API.md). The protocol itself is in Net/Gmcp.cs.
internal sealed partial class Kernel
{
    /// <summary>Only code under these directories may send GMCP. A wizard's object lives under /wiz or elsewhere and cannot.</summary>
    private static bool MaySendGmcp(ObjectRef caller)
        => caller.IsAlive && (caller.ProgramPath.StartsWith("/std/", StringComparison.Ordinal) || caller.ProgramPath.StartsWith("/secure/", StringComparison.Ordinal));

    public bool GmcpSend(ObjectRef caller, ObjectRef player, string package, object? value)
    {
        if (!MaySendGmcp(caller)) throw new MudRuntimeException($"{caller.Name} may not send GMCP");
        if (!Gmcp.ValidPackageName(package)) throw new MudRuntimeException("GmcpSend: bad package name");
        if (!player.IsAlive || player.Connection is not Connection connection) return false;
        if (!connection.GmcpSession.Enabled || !connection.GmcpSession.Wants(package)) return false; // the client did not ask: encode nothing
        var json = Gmcp.EncodeJson(value); // throws for a value that is not plain data; nothing is queued then
        return connection.SendGmcp(package, json);
    }

    /// <summary>Does this player's connection have GMCP on? A harmless boolean: anyone may ask.</summary>
    public bool GmcpActive(ObjectRef player)
        => player.IsAlive && player.Connection is Connection connection && connection.GmcpSession.Enabled;
}
