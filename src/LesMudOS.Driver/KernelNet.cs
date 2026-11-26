using System.Security.Cryptography;
using System.Text;
using LesMudOS.Api;
using LesMudOS.Driver.Net;

namespace LesMudOS.Driver;

// What the login throttle (mudlib/secure/throttle.cs, docs/specs/login-throttle.md) may ask of the driver. Three efuns, each refused by the master
// unless it is called by the object that needs it: the login object may read the address of its own connection (as a key, never the raw address
// of a player to a wizard), the throttle may turn a key into a tag and read its settings. No player or wizard code can reach any of them.
internal sealed partial class Kernel
{
    private byte[]? _addressSecret;

    /// <summary>
    /// The address of the caller's own connection as the throttle counts it (IPv4 text, or the /64 of an IPv6 address; for a web player the
    /// address from the PROXY line), or null if it has no connection. Only the login object, and only about itself.
    /// </summary>
    public string? ConnectionAddress(ObjectRef caller)
    {
        if (!AskMaster(m => m.ValidOperation("connection-address", caller, null)))
            throw new MudRuntimeException($"{caller.Name} may not read a connection address");
        return caller.Connection is Connection { ClientIp: { } ip } ? ConnectionLimits.AddressKey(ip) : null;
    }

    /// <summary>A keyed hash of an address key, as 16 hex characters, so that saves hold addresses only in a form that cannot be reversed without the server's secret. Only the throttle.</summary>
    public string AddressTag(ObjectRef caller, string key)
    {
        if (!AskMaster(m => m.ValidOperation("address-tag", caller, null)))
            throw new MudRuntimeException($"{caller.Name} may not tag an address");
        using var hmac = new HMACSHA256(AddressSecret());
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(key ?? "")), 0, 8).ToLowerInvariant();
    }

    /// <summary>One setting of the login throttle (see ThrottleSettings), or -1 for an unknown name. Only the throttle.</summary>
    public long ThrottleSetting(ObjectRef caller, string name)
    {
        if (!AskMaster(m => m.ValidOperation("throttle-setting", caller, null)))
            throw new MudRuntimeException($"{caller.Name} may not read the throttle settings");
        return ThrottleSettings.Get(name ?? "");
    }

    /// <summary>One line in the driver log from the throttle, cleaned of control characters, cut short, and rate limited. Only the throttle.</summary>
    public void ThrottleLog(ObjectRef caller, string text)
    {
        if (!AskMaster(m => m.ValidOperation("throttle-log", caller, null)))
            throw new MudRuntimeException($"{caller.Name} may not write the throttle log");
        var clean = new string((text ?? "").Where(c => c >= ' ' && c != 127).Take(200).ToArray());
        NetPolicy.ThrottleLog.Write($"login throttle: {clean}");
    }

    // The per-server secret: 32 random bytes in <save>/address-secret, made on first use (owner read/write only). If it cannot be stored the
    // tags last until the next restart, which only makes the saved "known addresses" forgotten, never wrong.
    private byte[] AddressSecret()
    {
        if (_addressSecret is { } known) return known;
        var file = Path.Combine(_saves.Root, "address-secret");
        try
        {
            if (File.Exists(file))
            {
                var text = File.ReadAllText(file).Trim();
                if (text.Length == 64) return _addressSecret = Convert.FromHexString(text);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException) { }
        var secret = RandomNumberGenerator.GetBytes(32);
        try
        {
            Directory.CreateDirectory(_saves.Root);
            File.WriteAllText(file, Convert.ToHexString(secret).ToLowerInvariant() + "\n");
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Info("login throttle: could not store the address secret; known addresses will be forgotten at the next restart");
        }
        return _addressSecret = secret;
    }
}
