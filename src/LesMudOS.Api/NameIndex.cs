namespace LesMudOS.Api;

/// <summary>
/// Telling identical things apart: a typed name may carry a position among the things that answer to it.
/// "rat 2", "2.rat" and "second rat" all mean the second thing that answers to "rat", in the order they are
/// listed in the room or the inventory. The parsing lives here, once; Present() and the mudlib's matching
/// helpers both use it.
/// </summary>
internal static class NameIndex
{
    private static readonly string[] Ordinals =
        { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth" };

    /// <summary>The most a typed number may be: a number is a position in a list, not an amount.</summary>
    public const int MaxPosition = 999;

    /// <summary>
    /// Split a typed name into the name and a position (1 or more). Returns 0 when the text carries no position;
    /// name is then the text unchanged. The forms are "2.rat", "rat 2" and "second rat" (first to tenth).
    /// </summary>
    public static int Split(string text, out string name)
    {
        name = text;
        var t = text.Trim();
        var dot = t.IndexOf('.');
        if (dot > 0 && dot < t.Length - 1 && IsNumber(t.Substring(0, dot)) && int.TryParse(t.Substring(0, dot), out var lead) && lead >= 1 && lead <= MaxPosition)
        {
            name = t.Substring(dot + 1).Trim();
            return name.Length == 0 ? 0 : lead;
        }
        var space = t.IndexOf(' ');
        if (space > 0)
        {
            var first = t.Substring(0, space).ToLowerInvariant();
            var index = Array.IndexOf(Ordinals, first);
            if (index >= 0)
            {
                name = t.Substring(space + 1).Trim();
                return name.Length == 0 ? 0 : index + 1;
            }
        }
        var last = t.LastIndexOf(' ');
        if (last > 0 && IsNumber(t.Substring(last + 1)) && int.TryParse(t.Substring(last + 1), out var tail) && tail >= 1 && tail <= MaxPosition)
        {
            name = t.Substring(0, last).Trim();
            return name.Length == 0 ? 0 : tail;
        }
        return 0;
    }

    private static bool IsNumber(string s) => s.Length > 0 && s.Length <= 3 && s.All(c => c >= '0' && c <= '9');
}
