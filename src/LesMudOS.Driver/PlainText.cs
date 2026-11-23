using System.Text;

namespace LesMudOS.Driver;

/// <summary>
/// Text from sandbox code, made safe to show to a person (docs/SANDBOX.md, "Text from the sandbox"). Removed: the C0 controls but
/// tab and newline (ESC among them, so no terminal escape: no concealed text, cleared screen, moved cursor or OSC), DEL, the C1
/// controls, the Unicode line and paragraph separators (which end a C# comment and split a line in some clients), the zero-width
/// characters and the bidirectional controls (which reorder what is shown), and the byte-order mark.
/// </summary>
internal static class PlainText
{
    public static bool IsUnsafe(char c)
    {
        int u = c;
        return (u < 0x20 && u != 0x0A && u != 0x09) || (u >= 0x7F && u <= 0x9F)
               || u == 0x2028 || u == 0x2029 || u == 0x061C || u == 0xFEFF || u == 0x180E
               || (u >= 0x200B && u <= 0x200F) || (u >= 0x202A && u <= 0x202E) || (u >= 0x2060 && u <= 0x2069);
    }

    public static string Clean(string text)
    {
        var i = 0;
        while (i < text.Length && !IsUnsafe(text[i])) i++;
        if (i == text.Length) return text;
        var clean = new StringBuilder(text.Length);
        clean.Append(text, 0, i);
        for (; i < text.Length; i++)
            if (!IsUnsafe(text[i])) clean.Append(text[i]);
        return clean.ToString();
    }
}
