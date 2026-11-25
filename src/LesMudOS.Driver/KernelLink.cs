using LesMudOS.Api;
using LesMudOS.Driver.Net;

namespace LesMudOS.Driver;

// The link-dead body (docs/specs/play-feel.md section 14): a player whose connection ends without `quit` stays in the world for a fixed time.
// The mudlib decides everything; the driver gives it the length of that time (a setting), a way to drop a connection from the game side (the idle
// timeout) and the one extra word in the disconnect line of the log.
internal sealed partial class Kernel
{
    public const int DefaultLingerSeconds = 60;

    /// <summary>Seconds a dropped player's body stays in the world. 60, or LESMUDOS_LINGER_SECONDS (1 to 3600; for tests).</summary>
    public int LingerSeconds()
        => int.TryParse(Environment.GetEnvironmentVariable("LESMUDOS_LINGER_SECONDS"), out var seconds) && seconds is >= 1 and <= 3600 ? seconds : DefaultLingerSeconds;

    /// <summary>A player's body drops its own connection (the idle timeout). The connection ends as if the client had closed it: NetDead follows.</summary>
    public bool DropConnection(ObjectRef caller, ObjectRef target)
    {
        // Only a body for itself, and never while sandbox code is acting (docs/SANDBOX.md).
        if (Restricted || caller != target || target.Domain.Length > 0 || !target.IsAlive || target.Connection is not Connection connection) return false;
        connection.Close("closed by the game (idle timeout)");
        return true;
    }

    /// <summary>For the disconnect line of the log: ", body lingers 60 s" when the player's body is going to stay, else "".</summary>
    private string LingerNote(Connection connection)
    {
        try
        {
            if (connection.Owner is { IsAlive: true } owner && CallOther(owner, "QueryLingerSeconds", []) is int seconds && seconds > 0) return $", body lingers {seconds} s";
        }
        catch (Exception) { }
        return "";
    }
}
