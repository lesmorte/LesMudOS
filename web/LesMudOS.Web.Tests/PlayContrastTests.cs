using System.Text.RegularExpressions;
using Xunit;

namespace LesMudOS.Web.Tests;

/// <summary>Every text pair of the client's terminal palette (docs/specs/web-client.md 7.5), recomputed from the variables in play.css.</summary>
public class PlayContrastTests
{
    private static string Css => File.ReadAllText(Path.Combine(Paths.ProjectDir, "wwwroot", "css", "play.css"));

    private static Dictionary<string, string> Block(string css, string startMarker)
    {
        var i = css.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(i >= 0, startMarker);
        var j = css.IndexOf('}', i);
        return Regex.Matches(css.Substring(i, j - i), @"(--[a-z0-9-]+):\s*(#[0-9a-fA-F]{6})").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
    }

    private static readonly string[] Ansi = ["c30", "c90", "c31", "c91", "c32", "c92", "c33", "c93", "c34", "c94", "c35", "c95", "c36", "c96", "c37", "c97"];

    private static void Check(Dictionary<string, string> v, double minText)
    {
        foreach (var c in Ansi) Assert.True(ContrastTests.Ratio(v["--" + c], v["--p-bg"]) >= minText, $"{c} {v["--" + c]} on {v["--p-bg"]} is {ContrastTests.Ratio(v["--" + c], v["--p-bg"]):F2}");
        foreach (var (fg, bg, min) in new[] { ("--p-fg", "--p-bg", 7.0), ("--p-fg", "--p-panel", 7.0), ("--p-dim", "--p-bg", minText), ("--p-dim", "--p-panel", minText), ("--p-accent", "--p-bg", minText), ("--p-accent", "--p-panel", minText), ("--p-on-accent", "--p-accent", minText) })
            Assert.True(ContrastTests.Ratio(v[fg], v[bg]) >= min, $"{fg} on {bg}: {ContrastTests.Ratio(v[fg], v[bg]):F2}");
        // the code buttons and the cyan command words sit on the panel
        Assert.True(ContrastTests.Ratio(v["--c96"], v["--p-panel"]) >= minText);
        // background colours carry dark text (the game sends none today)
        foreach (var c in Ansi) Assert.True(ContrastTests.Ratio(v["--" + c], v["--p-bg"]) >= minText);
    }

    [Fact] public void The_dark_palette_passes_aa() => Check(Block(Css, "html.play-page {"), 4.5);

    [Fact]
    public void The_high_contrast_palette_passes_7_to_1()
    {
        var css = Css;
        var i = css.IndexOf("@media (prefers-contrast: more)", StringComparison.Ordinal);
        Check(Block(css.Substring(i), "html.play-page {"), 7.0);
    }

    [Fact]
    public void The_dark_palette_has_the_values_of_the_spec_table()
    {
        var v = Block(Css, "html.play-page {");
        Assert.Equal("#e0786a", v["--c31"]); Assert.Equal("#ff9a8c", v["--c91"]); Assert.Equal("#6fc6c6", v["--c36"]); Assert.Equal("#8fe3e3", v["--c96"]);
        Assert.Equal("#15120e", v["--p-bg"]); Assert.Equal("#e8dfc9", v["--p-fg"]);
        Assert.True(Math.Abs(ContrastTests.Ratio(v["--p-fg"], v["--p-bg"]) - 14.07) < 0.05);
    }
}
