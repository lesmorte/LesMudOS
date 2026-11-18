/// <summary>
/// TEST HARNESS for tests/chronicle_test.py. It is copied into /secure of the
/// test's own copy of the mudlib and is NOT part of the shipped mudlib: it can
/// report any event for any player, which is exactly what must stay impossible
/// for ordinary objects. A wizard clones it and uses:
///
///   hrep alice,bob quest.local subject key=value ...   report an event (first name is the actor)
///   hq chronicle|renown|rumors|events Method args...   call a daemon's public method; args may be i:5, l:5, b:1 or text (~ is a space)
/// </summary>
public class ChronHarness : MudObject
{
    public override void Init()
    {
        AddAction("hrep", CmdReport);
        AddAction("hq", CmdQuery);
    }

    private ObjectRef? Player(string name)
        => Users().FirstOrDefault(u => string.Equals(u.Call<string>("QueryName"), name, StringComparison.OrdinalIgnoreCase));

    private bool CmdReport(string args)
    {
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) { Write("usage: hrep who[,who] event [subject] [key=value ...]\n"); return true; }
        var people = new List<ObjectRef>();
        foreach (var n in words[0].Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = Player(n);
            if (p == null) { Write("no such player online: " + n + "\n"); return true; }
            people.Add(p);
        }
        var subject = "";
        var names = new Dictionary<string, string>();
        foreach (var w in words.Skip(2))
        {
            var eq = w.IndexOf('=');
            if (eq > 0) names[w.Substring(0, eq)] = w.Substring(eq + 1).Replace('~', ' ');
            else if (subject == "") subject = w;
        }
        var result = LoadObject("/secure/events").Call<string>("Report", words[1], people[0], people, (ObjectRef?)null, subject, names);
        Write("report: " + result + "\nhend\n");
        return true;
    }

    private bool CmdQuery(string args)
    {
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) { Write("usage: hq daemon Method args\n"); return true; }
        var call = new List<object?>();
        foreach (var w in words.Skip(2))
        {
            if (w.StartsWith("i:")) call.Add(int.Parse(w.Substring(2)));
            else if (w.StartsWith("l:")) call.Add(long.Parse(w.Substring(2)));
            else if (w.StartsWith("b:")) call.Add(w.Substring(2) == "1");
            else call.Add(w.Replace('~', ' '));
        }
        var result = LoadObject("/secure/" + words[0]).Call(words[1], call.ToArray());
        string text;
        if (result == null) text = "(null)";
        else if (result is string s) text = s;
        else if (result is System.Collections.IEnumerable list)
            text = string.Join("\n", list.Cast<object?>().Select(x => x?.ToString() ?? ""));
        else text = result.ToString() ?? "";
        Write("hq: " + text + "\nhend\n");
        return true;
    }
}
