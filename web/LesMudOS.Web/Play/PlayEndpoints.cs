using System.Text.Json;

namespace LesMudOS.Web.Play;

public static class PlayEndpoints
{
    /// <summary>/play/ws (the bridge) and /play/status. /play itself is a Razor page; the install files are static.</summary>
    public static void MapPlay(this WebApplication app)
    {
        var runtime = app.Services.GetRequiredService<PlayRuntime>();

        app.MapGet("/play/status", (HttpContext ctx) =>
        {
            ctx.Response.Headers.CacheControl = "no-store";
            var on = runtime.IsOn();
            // No player count and no name: only whether the button should be offered.
            return Results.Text(JsonSerializer.Serialize(new { v = 1, enabled = on, full = on && runtime.IsFull }), "application/json");
        });

        app.MapGet("/play/ws", (HttpContext ctx) => runtime.HandleAsync(ctx));
    }
}
