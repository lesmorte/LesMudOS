namespace LesMudOS.Web;

/// <summary>Everything the owner can change without touching code. Bound from the "Site" section of appsettings.json,
/// from environment variables (Site__GamePort=4000) or from /etc/lesmudos-web.env on the server.</summary>
public sealed class SiteOptions
{
    public const string Section = "Site";

    /// <summary>Public address of the website, no trailing slash. Used for canonical links, Open Graph and the sitemap.</summary>
    public string BaseUrl { get; set; } = "https://legendsofhollowmere.com";

    /// <summary>The directory the website reads game data from (read only). Either the game's own save directory
    /// (secure/chronicle.json, secure/renown.json, secure/wizlist.json, players/*.json) or the sanitized copy that
    /// deploy/lesmudos-web-sync writes (chronicle.json, renown.json, wizlist.json, roster.json). Empty: no data.</summary>
    public string SaveDirectory { get; set; } = "";

    /// <summary>News posts and world copy. A relative path is looked up under the content root, then in ../content.</summary>
    public string ContentDirectory { get; set; } = "content";

    /// <summary>The secrets filter: which Chronicle entries are public. Relative to the content root or the application directory.</summary>
    public string FilterFile { get; set; } = "publicfilter.json";

    public string GameHost { get; set; } = "legendsofhollowmere.com";
    public int GamePort { get; set; } = 4000;

    /// <summary>Invite link. When empty the Discord link is not shown anywhere.</summary>
    public string DiscordUrl { get; set; } = "";

    /// <summary>A contact line for the support page, for example an email address. When empty the page says so plainly.</summary>
    public string SupportContact { get; set; } = "";

    /// <summary>How long the Chronicle, Renown and character files are cached. 30 to 60 seconds is right for a live game.</summary>
    public int CacheSeconds { get; set; } = 45;

    public int ChroniclePageSize { get; set; } = 12;
}
