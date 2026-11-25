using System.Diagnostics;
using Xunit;

namespace LesMudOS.Web.Tests;

/// <summary>Runs the Node tests in js/ (skipped where node is not installed). The page tests inside them also need jsdom (NODE_PATH) and are skipped without it.</summary>
public class NodeFactAttribute : FactAttribute
{
    public NodeFactAttribute() { if (JavaScriptTests.FindNode() == null) Skip = "node is not installed"; }
}

public class JavaScriptTests
{
    public static string? FindNode()
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var p = Path.Combine(dir, "node");
            if (File.Exists(p)) return p;
        }
        return null;
    }

    [NodeFact]
    public async Task The_script_passes_its_node_tests()
    {
        using var site = new Site(null, new() { ["Site:Play:Enabled"] = "true" });
        var (_, html, _) = await site.Get("/play");
        var htmlPath = Path.Combine(Path.GetTempPath(), "hollowmere-play-" + Guid.NewGuid().ToString("N") + ".html");
        File.WriteAllText(htmlPath, html);
        try
        {
            var psi = new ProcessStartInfo(FindNode()!, new[] { "--test", Path.Combine(Paths.WebDir, "LesMudOS.Web.Tests", "js", "play.test.mjs") })
            { RedirectStandardOutput = true, RedirectStandardError = true };
            psi.Environment["HM_PLAY_HTML"] = htmlPath;
            using var proc = Process.Start(psi)!;
            var stdout = proc.StandardOutput.ReadToEndAsync();
            var stderr = proc.StandardError.ReadToEndAsync();
            Assert.True(proc.WaitForExit(120_000), "node --test did not finish");
            var output = await stdout + await stderr;
            Assert.True(proc.ExitCode == 0, output);
        }
        finally { File.Delete(htmlPath); }
    }
}
