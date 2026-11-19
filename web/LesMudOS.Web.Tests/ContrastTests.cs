using System.Text.RegularExpressions;
using Xunit;

namespace LesMudOS.Web.Tests;

/// <summary>Every text-on-background pair the stylesheet uses must pass WCAG AA (4.5:1; 7:1 for the body text). The pairs are listed here by CSS variable.</summary>
public class ContrastTests
{
    public static readonly (string Fg, string Bg, double Min, string Use)[] Pairs =
    {
        ("ink", "parch", 7, "body text on the page"), ("ink", "parch-2", 7, "body text on alternate bands"), ("ink", "card", 7, "body text on panels"),
        ("ink-2", "parch", 4.5, "secondary text"), ("ink-2", "parch-2", 4.5, "secondary text on bands"), ("ink-2", "card", 4.5, "secondary text on panels"),
        ("oak", "parch", 7, "headings"), ("oak", "parch-2", 7, "headings on bands"), ("oak", "card", 7, "headings on panels"),
        ("link", "parch", 4.5, "links"), ("link", "parch-2", 4.5, "links on bands"), ("link", "card", 4.5, "links on panels"),
        ("copper", "parch", 4.5, "eyebrows, dates, numerals"), ("copper", "parch-2", 4.5, "eyebrows on bands"), ("copper", "card", 4.5, "badges on panels"),
        ("parch", "green", 4.5, "masthead text"), ("gilt", "green", 4.5, "masthead hover"),
        ("card", "copper", 4.5, "primary buttons, Play Now"), ("copper", "card", 4.5, "primary button hover"),
        ("card", "oak", 4.5, "active tab"), ("parch", "oak", 4.5, "footer text"), ("gilt", "oak", 4.5, "footer links"),
        ("green", "parch", 4.5, "map wood label"), ("ink-2", "card", 4.5, "plate captions"),
    };

    public static Dictionary<string, string> Variables()
    {
        var css = File.ReadAllText(Path.Combine(Paths.ProjectDir, "wwwroot", "css", "site.css"));
        return Regex.Matches(css, @"--([a-z0-9-]+):\s*(#[0-9a-fA-F]{6})").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
    }

    private static double Lum(string hex)
    {
        double C(int i) { var v = Convert.ToInt32(hex.Substring(1 + i * 2, 2), 16) / 255.0; return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
        return 0.2126 * C(0) + 0.7152 * C(1) + 0.0722 * C(2);
    }

    public static double Ratio(string a, string b)
    {
        var (x, y) = (Lum(a), Lum(b));
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    [Fact]
    public void Every_text_pair_passes_wcag_aa()
    {
        var v = Variables();
        var lines = new List<string>();
        foreach (var (fg, bg, min, use) in Pairs)
        {
            var r = Ratio(v[fg], v[bg]);
            lines.Add($"{use}|{fg} {v[fg]}|{bg} {v[bg]}|{r:F2}");
            Assert.True(r >= min, $"{use}: {fg} on {bg} is {r:F2}:1, needs {min}");
        }
        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "hollowmere-contrast.txt"), lines);
    }
}
