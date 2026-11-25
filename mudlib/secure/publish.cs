/// <summary>
/// The publication register (docs/SANDBOX.md section 4, docs/PUBLISHING.md): how a wizard's work leaves the sandbox.
///
/// A wizard asks (submit); any one wizard of level 5 or higher other than the submitter decides (approve, reject); the submitter may
/// withdraw; a level 10 wizard may revoke a publication. Every request is kept for good in this daemon's save file: who asked, what,
/// where to, the SHA-256 of every file as submitted, and who decided, when, with the SHA-256 of every file as published.
///
/// Approval writes exactly the content that was checked (each file is read once, hashed, compared, rewritten and written), compiles
/// every file and loads every one that is not abstract, and removes everything it wrote if anything fails. The master lets this
/// blueprint read anywhere and write, compile and destruct under /domains and nowhere else; every function here acts only for the
/// real player whose own command is running, never for sandbox code.
/// </summary>
public class Publish : MudObject
{
    private const string SaveFile = "/secure/publish";
    private const string WizList = "/secure/wizlist";
    private const int LevelDecide = 5;
    private const int LevelRevoke = 10;
    private const int MaxFiles = 200;
    private const int MaxDepth = 8;
    private const int MaxRequestChars = 1_000_000;  // the copy a request keeps of its files
    private const int MaxWaitingChars = 4_000_000;  // all copies kept for waiting requests together

    /// <summary>
    /// The requests, flattened: "&lt;id&gt;.&lt;field&gt;" to the value. Fields: submitter, source, dest, note, time, state, files, kind,
    /// decider, decided, reason, published; and while a request waits, "copy:&lt;file&gt;", the text of each file as submitted, which is
    /// what a reviewer reads (submission N cat FILE) and what approval publishes (P2 of the November 2026 review).
    /// </summary>
    [Saved] private Dictionary<string, string> records = new();
    [Saved] private int nextId = 1;

    public override void Create()
    {
        if (Self.IsClone) return;
        RestoreObject(SaveFile);
    }

    private void Store() => SaveObject(SaveFile);

    private string Get(int id, string field) => records.GetValueOrDefault(id + "." + field) ?? "";

    private void Set(int id, string field, string value) => records[id + "." + field] = value ?? "";

    /// <summary>The copy a request keeps of its files, or null if it keeps none (decided, or from before copies were kept).</summary>
    private Dictionary<string, string>? CopyOf(int id)
    {
        var files = ParseList(Get(id, "files"));
        var copy = new Dictionary<string, string>();
        foreach (var file in files.Keys)
        {
            if (!records.TryGetValue(id + ".copy:" + file, out var text)) return null;
            copy[file] = text;
        }
        return copy.Count > 0 ? copy : null;
    }

    private void DropCopy(int id)
    {
        foreach (var key in records.Keys.Where(k => k.StartsWith(id + ".copy:")).ToList()) records.Remove(key);
    }

    private List<int> Ids()
    {
        var ids = new List<int>();
        for (var i = 1; i < nextId; i++) if (records.ContainsKey(i + ".state")) ids.Add(i);
        return ids;
    }

    // ------------------------------------------------------------------
    // Who is asking: the real player whose own command is running, never sandbox code
    // ------------------------------------------------------------------

    private (string Name, int Level, ObjectRef Body)? Actor()
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller != ThisPlayer() || RestrictedCaller() != null) return null;
        if (!caller.IsClone || caller.ProgramPath != "/std/player" || caller.Domain != "") return null;
        var name = (caller.Call<string>("QueryName") ?? "").ToLowerInvariant();
        if (name == "") return null;
        var level = LoadObject(WizList).Call<int>("QueryLevel", name);
        return level < 1 ? null : (name, level, caller);
    }

    private const string Refused = "Only a wizard's own command can do that.\n";

    // ------------------------------------------------------------------
    // Names and paths: lower-case ASCII only, so that "..", hidden names, case tricks and look-alike letters cannot be used
    // ------------------------------------------------------------------

    private static bool PlainName(string name)
        => name.Length > 0 && name.Length <= 40 && name.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');

    private static bool PlainFile(string name)
        => name.EndsWith(".cs") && PlainName(name.Substring(0, name.Length - 3));

    /// <summary>"/domains/&lt;name&gt;", exactly one plain name below /domains.</summary>
    private static bool ValidDestination(string dest)
        => dest.StartsWith("/domains/") && PlainName(dest.Substring("/domains/".Length));

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    /// <summary>
    /// Every .cs file under a directory, as paths relative to it ("rooms/hall.cs"), or the reason it cannot be published.
    /// </summary>
    private string? Walk(string root, string relative, int depth, List<string> files)
    {
        if (depth > MaxDepth) return $"{root}/{relative} is nested too deeply.";
        var dir = relative == "" ? root : root + "/" + relative;
        var entries = GetDir(dir);
        if (entries == null) return $"{dir} is not a directory.";
        foreach (var entry in entries)
        {
            var path = relative == "" ? entry.Name : relative + "/" + entry.Name;
            if (entry.IsLink) return $"{root}/{path} is a symbolic link.";
            if (entry.IsDirectory)
            {
                if (!PlainName(entry.Name)) return $"{root}/{path}: directory names may only use a-z, 0-9 and _.";
                var problem = Walk(root, path, depth + 1, files);
                if (problem != null) return problem;
            }
            else
            {
                if (!PlainFile(entry.Name)) return $"{root}/{path}: only .cs files named with a-z, 0-9 and _ can be published.";
                files.Add(path);
                if (files.Count > MaxFiles) return $"{root} holds more than {MaxFiles} files.";
            }
        }
        return null;
    }

    /// <summary>Rewrite the source path to the destination where it stands as a whole path (not "/wiz/bob/area2" for "/wiz/bob/area").</summary>
    private static string Rewrite(string text, string from, string to)
    {
        var result = new StringBuilder();
        var at = 0;
        while (true)
        {
            var hit = text.IndexOf(from, at, StringComparison.Ordinal);
            if (hit < 0) break;
            var after = hit + from.Length;
            var next = after < text.Length ? text[after] : ' ';
            var whole = !(char.IsLetterOrDigit(next) || next == '_' || next == '-' || next == '.');
            result.Append(text, at, hit - at).Append(whole ? to : from);
            at = after;
        }
        result.Append(text, at, text.Length - at);
        return result.ToString();
    }

    private static readonly HashSet<string> TypeKeywords = new() { "class", "struct", "interface", "enum", "record", "delegate" };

    /// <summary>
    /// Every name that may be a type declared in a submitted file, over-counted on purpose (L1 of the November 2026 review): each
    /// word after class, struct, interface, enum or record, anywhere in the text (comments and strings included, so that nothing can
    /// hide a declaration), with \uXXXX escapes decoded first. A comment such as "this class Room" then counts Room too; the
    /// author rewords it.
    /// </summary>
    private static List<string> DeclaredNames(string text)
    {
        var decoded = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 5 < text.Length && text[i + 1] == 'u' && Hex(text.Substring(i + 2, 4)) is { } code)
            {
                decoded.Append((char)code);
                i += 5;
            }
            else decoded.Append(text[i]);
        }
        var words = Words(decoded.ToString());
        var names = new List<string>();
        for (var i = 0; i + 1 < words.Count; i++)
            if (TypeKeywords.Contains(words[i]) && !TypeKeywords.Contains(words[i + 1]) && words[i + 1] != "where") names.Add(words[i + 1]);
        return names;
    }

    private static int? Hex(string four)
    {
        var value = 0;
        foreach (var c in four)
        {
            var digit = c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'f' ? c - 'a' + 10 : c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;
            if (digit < 0) return null;
            value = value * 16 + digit;
        }
        return value;
    }

    /// <summary>The identifier-like words of a text, in order (an @ before a word is dropped; everything else separates words).</summary>
    private static List<string> Words(string text)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) || c == '_') { word.Append(c); continue; }
            if (word.Length > 0) { words.Add(word.ToString()); word.Clear(); }
        }
        if (word.Length > 0) words.Add(word.ToString());
        return words;
    }

    /// <summary>
    /// The names of the types declared in a file of the mudlib itself (trusted, so read exactly): words after a type keyword at
    /// brace depth 0, outside comments, strings and character literals.
    /// </summary>
    private static List<string> ClassNames(string text)
    {
        var code = new StringBuilder(text.Length);
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';
            if (c == '/' && next == '/') { while (i < text.Length && text[i] != '\n') i++; code.Append(' '); continue; }
            if (c == '/' && next == '*') { i += 2; while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++; i++; code.Append(' '); continue; }
            if (c == '"' || c == '\'')
            {
                var verbatim = i > 0 && (text[i - 1] == '@' || (text[i - 1] == '$' && i > 1 && text[i - 2] == '@'));
                var quote = c;
                i++;
                while (i < text.Length && text[i] != quote) { if (!verbatim && text[i] == '\\') i++; else if (verbatim && i + 1 < text.Length && text[i] == '"' && text[i + 1] == '"') i++; i++; }
                code.Append(' ');
                continue;
            }
            if (c == '{') { depth++; code.Append(' '); continue; }
            if (c == '}') { depth--; code.Append(' '); continue; }
            code.Append(depth == 0 ? c : ' ');
        }
        var words = Words(code.ToString());
        var names = new List<string>();
        for (var i = 0; i + 1 < words.Count; i++)
            if (TypeKeywords.Contains(words[i]) && !TypeKeywords.Contains(words[i + 1])) names.Add(words[i + 1]);
        return names;
    }

    /// <summary>
    /// The first character of a text that cannot be published, as "line L, column C: U+XXXX", or null (P1 of the November 2026
    /// review): anything but tab, newline and printable characters. U+2028 ends a // comment for the compiler but not for a reader;
    /// the bidirectional controls reorder what is shown; the zero-width characters and terminal controls hide things.
    /// </summary>
    private static string? Unprintable(string text)
    {
        var line = 1;
        var column = 0;
        for (var at = 0; at < text.Length; at++)
        {
            var c = text[at];
            column++;
            if (c == '\n') { line++; column = 0; continue; }
            int u = c;
            var bad = (u < 0x20 && u != 0x09 && u != 0x0D) || (u >= 0x7F && u <= 0x9F) || u == 0x2028 || u == 0x2029 || u == 0x061C
                      || u == 0xFEFF || u == 0x180E || (u >= 0x200B && u <= 0x200F) || (u >= 0x202A && u <= 0x202E) || (u >= 0x2060 && u <= 0x2069)
                      || u == 0x00AD || u == 0x034F || u == 0x115F || u == 0x1160 || u == 0x3164 || (u >= 0xFFF0 && u <= 0xFFFF)
                      || (u >= 0xE000 && u <= 0xF8FF) || (u >= 0xFE00 && u <= 0xFE0F) || char.IsSurrogate(c);
            if (u == 0x0D) bad = at + 1 >= text.Length || text[at + 1] != '\n'; // CR only as part of a Windows line end: alone it ends a // comment
            if (bad) return $"line {line}, column {column}: U+{u:X4}";
        }
        return null;
    }

    /// <summary>Every class name declared at the top level of the mudlib's own files (outside /wiz and outside one directory).</summary>
    private void MudlibClasses(string dir, string skip, Dictionary<string, string> into, int depth)
    {
        if (depth > 12) return;
        var entries = GetDir(dir == "" ? "/" : dir);
        if (entries == null) return;
        foreach (var entry in entries)
        {
            if (entry.IsLink) continue;
            var path = dir + "/" + entry.Name;
            if (entry.IsDirectory)
            {
                if (path == "/wiz" || path == skip) continue;
                MudlibClasses(path, skip, into, depth + 1);
            }
            else if (entry.Name.EndsWith(".cs"))
            {
                string? text;
                try { text = ReadFile(path); }
                catch (MudRuntimeException) { continue; }
                if (text == null) continue;
                foreach (var name in ClassNames(text)) into.TryAdd(name, path);
            }
        }
    }

    // ------------------------------------------------------------------
    // Who owns a destination
    // ------------------------------------------------------------------

    /// <summary>The approved request now published at a destination, or 0.</summary>
    private int PublishedAt(string dest)
    {
        var found = 0;
        foreach (var id in Ids()) if (Get(id, "dest") == dest && Get(id, "state") == "approved") found = id;
        return found;
    }

    /// <summary>Why this wizard may not publish to this destination, or null; kind is "new" or "update".</summary>
    private string? DestinationRefusal(string dest, string submitter, out string kind)
    {
        kind = "new";
        if (!ValidDestination(dest)) return $"{dest}: the destination must be /domains/<name>, the name made of a-z, 0-9 and _.";
        var parent = GetDir("/domains") ?? new List<DirEntry>();
        var name = dest.Substring("/domains/".Length);
        foreach (var entry in parent)
        {
            if (entry.Name.ToLowerInvariant() == name && entry.Name != name) return $"{dest}: /domains/{entry.Name} already exists under another spelling.";
            if (entry.Name == name && entry.IsLink) return $"{dest} is a symbolic link.";
            if (entry.Name == name && !entry.IsDirectory) return $"{dest} is a file.";
        }
        var exists = parent.Any(e => e.Name == name);
        var published = PublishedAt(dest);
        if (published > 0)
        {
            if (Get(published, "submitter") != submitter) return $"{dest} was published by {Cap(Get(published, "submitter"))}: only they can update it.";
            kind = "update";
            return null;
        }
        return exists ? $"{dest} belongs to the base game (or to a publication that was revoked): choose another destination." : null;
    }

    // ------------------------------------------------------------------
    // Reading a request's files now
    // ------------------------------------------------------------------

    /// <summary>Read every file of a source directory once: relative path to content. Null with a reason if it cannot be read.</summary>
    private Dictionary<string, string>? ReadAll(string source, out string? problem)
    {
        var files = new List<string>();
        problem = Walk(source, "", 0, files);
        if (problem != null) return null;
        if (files.Count == 0) { problem = $"{source} holds no .cs files."; return null; }
        var contents = new Dictionary<string, string>();
        foreach (var file in files)
        {
            var text = ReadFile(source + "/" + file);
            if (text == null) { problem = $"{source}/{file} could not be read."; return null; }
            contents[file] = text;
        }
        return contents;
    }

    private static string FileList(Dictionary<string, string> hashes) => string.Join(";", hashes.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + "|" + kv.Value));

    private static Dictionary<string, string> ParseList(string list)
    {
        var result = new Dictionary<string, string>();
        foreach (var part in list.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var bar = part.IndexOf('|');
            if (bar > 0) result[part.Substring(0, bar)] = part.Substring(bar + 1);
        }
        return result;
    }

    /// <summary>What is wrong with a set of files as content for the real game, or null: other wizards' paths, clashing class names.</summary>
    private string? ContentProblem(Dictionary<string, string> rewritten, string dest)
    {
        foreach (var (file, text) in rewritten)
            if (text.ToLowerInvariant().Contains("/wiz/") || text.ToLowerInvariant().Contains("\"/wiz\""))
                return $"{file} names a path in a wizard's home other than its own directory: published code may not depend on a workshop.";
        var mine = new Dictionary<string, string>();
        foreach (var (file, text) in rewritten)
            foreach (var name in DeclaredNames(text).Distinct())
                if (!mine.TryAdd(name, file)) return $"the class name {name} is used twice ({mine[name]} and {file}).";
        var theirs = new Dictionary<string, string>();
        MudlibClasses("", dest, theirs, 0);
        foreach (var (name, file) in mine)
            if (theirs.TryGetValue(name, out var other)) return $"the class name {name} in {file} is already used by {other}.cs: class names must be unique in the mudlib.";
        return null;
    }

    // ------------------------------------------------------------------
    // Asking
    // ------------------------------------------------------------------

    /// <summary>submit: a directory of the wizard's own home (already resolved by the body), an optional destination and note.</summary>
    public string Submit(string source, string dest, string note)
    {
        if (Actor() is not { } actor) return Refused;
        source = (source ?? "").TrimEnd('/');
        var home = "/wiz/" + actor.Name;
        if (!source.StartsWith(home + "/")) return $"You can submit only a directory inside your own home, {home}.\n";
        var parts = source.Substring(home.Length + 1).Split('/');
        if (parts.Any(p => !PlainName(p))) return $"{source}: directory names may only use a-z, 0-9 and _.\n";
        dest = string.IsNullOrWhiteSpace(dest) ? "/domains/" + parts[^1] : dest.Trim().TrimEnd('/');
        note = (note ?? "").Trim();
        if (note.Length > 200) note = note.Substring(0, 200);

        var refusal = DestinationRefusal(dest, actor.Name, out var kind);
        if (refusal != null) return refusal + "\n";
        var contents = ReadAll(source, out var problem);
        if (contents == null) return problem + "\n";
        foreach (var (file, text) in contents.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            if (Unprintable(text) is { } where)
                return $"Not submitted: {source}/{file}, {where}: only printable characters, tabs and line ends can be published (a reviewer must see every character).\n";
        var size = contents.Values.Sum(t => t.Length);
        if (size > MaxRequestChars) return $"Not submitted: {source} holds {size} characters; a request can hold at most {MaxRequestChars}.\n";

        // Every file must compile where it is now, in the workshop. A file already compiled from exactly its present text is not
        // compiled again; the others are, for at most three seconds in this command (the second review's C2: one command must not
        // hold the game while it compiles two hundred files). What is left is named: update it, then submit again.
        var errors = new List<string>();
        var later = new List<string>();
        var started = TimeMs();
        foreach (var file in contents.Keys.OrderBy(f => f, StringComparer.Ordinal))
        {
            var program = ProgramOf(source, file);
            if (ProgramCurrent(program)) continue;
            if (TimeMs() - started > 3000) { later.Add(file); continue; }
            try { UpdateProgram(program); }
            catch (MudRuntimeException e) { errors.Add(e.Message); }
        }
        if (errors.Count > 0) return "Not submitted: these files do not compile.\n" + string.Join("\n", errors.Take(5)) + "\n";
        if (later.Count > 0)
            return $"Not submitted yet: {later.Count} file(s) still to be compiled ({string.Join(", ", later.Take(5))}{(later.Count > 5 ? ", ..." : "")}). " +
                   "Compile them with update (or wait a moment and submit again: each submit compiles for a few seconds).\n";

        var rewritten = contents.ToDictionary(kv => kv.Key, kv => Rewrite(kv.Value, source, dest));
        var content = ContentProblem(rewritten, dest);
        if (content != null) return "Not submitted: " + content + "\n";

        // A newer request for the same work replaces one still waiting.
        foreach (var old in Ids())
            if (Get(old, "state") == "waiting" && Get(old, "submitter") == actor.Name && (Get(old, "source") == source || Get(old, "dest") == dest))
            {
                Set(old, "state", "replaced");
                Set(old, "decided", Time().ToString());
                DropCopy(old);
            }
        var waitingSize = Ids().Where(i => Get(i, "state") == "waiting").Sum(i => CopyOf(i)?.Values.Sum(t => t.Length) ?? 0);
        if (waitingSize + size > MaxWaitingChars)
            return "Not submitted: the register is holding as much waiting work as it can. Ask a wizard of level 5 to decide the waiting requests first.\n";

        var id = nextId++;
        Set(id, "submitter", actor.Name);
        Set(id, "source", source);
        Set(id, "dest", dest);
        Set(id, "note", note);
        Set(id, "time", Time().ToString());
        Set(id, "state", "waiting");
        Set(id, "kind", kind);
        Set(id, "files", FileList(contents.ToDictionary(kv => kv.Key, kv => Sha256(kv.Value))));
        foreach (var (file, text) in contents) Set(id, "copy:" + file, text);
        Store();

        foreach (var user in Users())
        {
            if (user == actor.Body || !user.IsAlive) continue;
            var name = user.Call<string>("QueryName") ?? "";
            if (LoadObject(WizList).Call<int>("QueryLevel", name) >= LevelDecide)
                TellObject(user, $"[publishing] {Cap(actor.Name)} asks for {source} to be published to {dest} (request #{id}). {{cmd}}submission {id}{{/}} to review it.\n");
        }
        return $"Submitted as request #{id}: {contents.Count} file(s) from {source} to {dest}{(kind == "update" ? " (an update)" : "")}. A wizard of level {LevelDecide} or higher will review it. The request keeps its own copy of the files: that copy is what is reviewed and published, whatever happens to the files afterwards.\n";
    }

    /// <summary>How many requests are waiting (the login notice for wizards).</summary>
    public int QueryWaiting() => Ids().Count(id => Get(id, "state") == "waiting");

    // ------------------------------------------------------------------
    // Reviewing
    // ------------------------------------------------------------------

    private string Line(int id) =>
        $"#{id} {Get(id, "state"),-9} {Cap(Get(id, "submitter")),-10} {Get(id, "source")} -> {Get(id, "dest")} ({ParseList(Get(id, "files")).Count} files{(Get(id, "kind") == "update" ? ", update" : "")})" +
        (Get(id, "note") != "" ? $": {Get(id, "note")}" : "");

    public string List(string args)
    {
        if (Actor() is not { } actor) return Refused;
        var all = actor.Level >= LevelDecide;
        var ids = Ids().Where(id => all || Get(id, "submitter") == actor.Name).ToList();
        if (args.Trim() != "all") ids = ids.Where(id => Get(id, "state") == "waiting" || Get(id, "submitter") == actor.Name).ToList();
        if (ids.Count == 0) return all ? "No requests are waiting.\n" : "You have no requests.\n";
        var waiting = ids.Where(id => Get(id, "state") == "waiting").ToList();
        var rest = ids.Where(id => Get(id, "state") != "waiting").OrderByDescending(id => id).Take(20).ToList();
        var text = new StringBuilder();
        foreach (var id in waiting.Concat(rest)) text.Append(Line(id)).Append('\n');
        return text.ToString();
    }

    public string Show(int id)
    {
        if (Actor() is not { } actor) return Refused;
        if (!records.ContainsKey(id + ".state")) return $"There is no request #{id}.\n";
        if (actor.Level < LevelDecide && Get(id, "submitter") != actor.Name) return $"Request #{id} is not yours.\n";
        var text = new StringBuilder(Line(id) + "\n");
        text.Append($"  asked {Get(id, "time")} by {Cap(Get(id, "submitter"))}\n");
        if (Get(id, "decider") != "") text.Append($"  {Get(id, "state")} {Get(id, "decided")} by {Cap(Get(id, "decider"))}{(Get(id, "reason") != "" ? ": " + Get(id, "reason") : "")}\n");
        var files = ParseList(Get(id, "files"));
        var before = new Dictionary<string, string>();
        var prior = PublishedAt(Get(id, "dest"));
        if (prior > 0 && prior != id) before = ParseList(Get(prior, "submitted"));
        foreach (var (file, hash) in files.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var mark = prior > 0 && prior != id ? (!before.ContainsKey(file) ? " (new)" : before[file] != hash ? " (changed)" : "") : "";
            text.Append($"  {Get(id, "source")}/{file}  sha256 {hash}{mark}\n");
        }
        if (prior > 0 && prior != id)
            foreach (var file in before.Keys.Where(f => !files.ContainsKey(f)).OrderBy(f => f, StringComparer.Ordinal))
                text.Append($"  {file} (removed)\n");
        if (Get(id, "state") == "waiting")
            text.Append($"  Read each file with {{cmd}}submission {id} cat FILE{{/}}: that is the register's own copy, exactly what approval publishes, shown\n" +
                        $"  from here, outside any workshop, where no wizard's code can change what you see. Then {{cmd}}approve {id}{{/}} or {{cmd}}reject {id}{{/}} REASON.\n" +
                        "  Do not review from inside the submitter's workshop: it is their ground, and what their code shows you there is theirs.\n");
        return text.ToString();
    }

    /// <summary>
    /// submission N cat FILE: the register's copy of one file of a waiting request, numbered, every character that is not printable
    /// ASCII shown as &lt;U+XXXX&gt;, for the body to print exactly as it is (no colour markup is interpreted).
    /// </summary>
    public string ShowFile(int id, string file)
    {
        if (Actor() is not { } actor) return Refused;
        if (!records.ContainsKey(id + ".state")) return $"There is no request #{id}.\n";
        if (actor.Level < LevelDecide && Get(id, "submitter") != actor.Name) return $"Request #{id} is not yours.\n";
        if (CopyOf(id) is not { } copy) return $"Request #{id} keeps no copy of its files (it is {Get(id, "state")}).\n";
        file = (file ?? "").Trim().TrimStart('/');
        if (file.StartsWith(Get(id, "source").TrimStart('/') + "/")) file = file.Substring(Get(id, "source").Length);
        if (!file.EndsWith(".cs")) file += ".cs";
        if (!copy.TryGetValue(file, out var text))
            return $"Request #{id} has no file {file}. Its files: {string.Join(", ", copy.Keys.OrderBy(f => f, StringComparer.Ordinal))}.\n";
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[^1] == "") lines.RemoveAt(lines.Count - 1);
        var shown = new StringBuilder($"Request #{id}, {file} as submitted ({lines.Count} lines, sha256 {Sha256(text)}):\n");
        for (var i = 0; i < lines.Count; i++)
        {
            shown.Append($"{i + 1,4}  ");
            foreach (var c in lines[i])
            {
                if (c == '\t' || (c >= ' ' && c <= '~')) shown.Append(c);
                else shown.Append($"<U+{(int)c:X4}>");
            }
            shown.Append('\n');
        }
        return shown.ToString();
    }

    // ------------------------------------------------------------------
    // Deciding
    // ------------------------------------------------------------------

    private string? DeciderRefusal((string Name, int Level, ObjectRef Body) actor, int id, string verb)
    {
        if (!records.ContainsKey(id + ".state")) return $"There is no request #{id}.";
        if (actor.Level < LevelDecide) return $"You need wizard level {LevelDecide} to {verb} a request.";
        if (Get(id, "submitter") == actor.Name) return $"You cannot {verb} your own request: another wizard of level {LevelDecide} or higher must.";
        if (Get(id, "state") != "waiting") return $"Request #{id} is {Get(id, "state")}, not waiting.";
        return null;
    }

    public string Reject(int id, string reason)
    {
        if (Actor() is not { } actor) return Refused;
        if (DeciderRefusal(actor, id, "reject") is { } refusal) return refusal + "\n";
        reason = (reason ?? "").Trim();
        if (reason == "") return "Say why: {cmd}reject{/} NUMBER REASON.\n";
        Set(id, "state", "rejected");
        DropCopy(id);
        Set(id, "decider", actor.Name);
        Set(id, "decided", Time().ToString());
        Set(id, "reason", reason.Length > 200 ? reason.Substring(0, 200) : reason);
        Store();
        Notify(Get(id, "submitter"), $"[publishing] {Cap(actor.Name)} rejected your request #{id} ({Get(id, "source")}): {reason}\n");
        return $"Request #{id} is rejected.\n";
    }

    public string Withdraw(int id)
    {
        if (Actor() is not { } actor) return Refused;
        if (!records.ContainsKey(id + ".state")) return $"There is no request #{id}.\n";
        if (Get(id, "submitter") != actor.Name) return $"Request #{id} is not yours.\n";
        if (Get(id, "state") != "waiting") return $"Request #{id} is {Get(id, "state")}, not waiting.\n";
        Set(id, "state", "withdrawn");
        DropCopy(id);
        Set(id, "decided", Time().ToString());
        Store();
        return $"Request #{id} is withdrawn.\n";
    }

    private void Notify(string name, string text)
    {
        foreach (var user in Users())
            if ((user.Call<string>("QueryName") ?? "").ToLowerInvariant() == name) TellObject(user, text);
    }

    /// <summary>approve: check, write, compile, load; all or nothing.</summary>
    public string Approve(int id, string destOverride)
    {
        if (Actor() is not { } actor) return Refused;
        if (DeciderRefusal(actor, id, "approve") is { } refusal) return refusal + "\n";
        var source = Get(id, "source");
        var submitter = Get(id, "submitter");
        var dest = string.IsNullOrWhiteSpace(destOverride) ? Get(id, "dest") : destOverride.Trim().TrimEnd('/');
        var destRefusal = DestinationRefusal(dest, submitter, out var kind);
        if (destRefusal != null) return "Not approved: " + destRefusal + "\n";

        // 1. Exactly what was submitted and reviewed: the register's own copy, checked against the hashes taken at submission. The
        // files in the workshop are not read again (they may have changed since, for better or worse: it does not matter).
        if (CopyOf(id) is not { } contents) return $"Not approved: request #{id} keeps no copy of its files. {Cap(submitter)} must submit it again.\n";
        var submitted = ParseList(Get(id, "files"));
        var changed = submitted.Keys.Where(f => !contents.ContainsKey(f) || Sha256(contents[f]) != submitted[f]).ToList();
        if (changed.Count > 0 || contents.Count != submitted.Count) return $"Not approved: the register's copy of request #{id} does not match its hashes. {Cap(submitter)} must submit it again.\n";
        if (contents.Values.Select(Unprintable).FirstOrDefault(w => w != null) is { } where) return $"Not approved: a file holds an unprintable character ({where}).\n";

        // 2. Rewritten for the destination, and still fit for the real game.
        var rewritten = contents.ToDictionary(kv => kv.Key, kv => Rewrite(kv.Value, source, dest));
        if (ContentProblem(rewritten, dest) is { } content) return "Not approved: " + content + "\n";

        // 3. Write, compile, load; undo everything on any failure.
        var prior = PublishedAt(dest);
        var previous = prior > 0 ? ParseList(Get(prior, "published")).Keys.ToList() : new List<string>();
        var backup = new Dictionary<string, string>();
        var created = new List<string>();
        var createdDirs = new List<string>();
        var loaded = new List<ObjectRef>();
        try
        {
            if (GetDir(dest) == null) { if (MakeDir(dest)) createdDirs.Add(dest); }
            foreach (var (file, text) in rewritten.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                var path = dest + "/" + file;
                var dirs = file.Split('/');
                for (var i = 1; i < dirs.Length; i++)
                {
                    var dir = dest + "/" + string.Join("/", dirs.Take(i));
                    var listing = GetDir(dir);
                    if (listing == null) { if (MakeDir(dir)) createdDirs.Add(dir); }
                }
                var old = ReadFile(path);
                if (old != null) backup[path] = old; else created.Add(path);
                WriteFile(path, text);
            }
            foreach (var gone in previous.Where(f => !rewritten.ContainsKey(f)))
            {
                var path = dest + "/" + gone;
                var old = ReadFile(path);
                if (old == null) continue;
                backup[path] = old;
                RemoveFile(path);
            }
            foreach (var file in rewritten.Keys.OrderBy(f => f, StringComparer.Ordinal)) UpdateProgram(ProgramOf(dest, file));
            foreach (var file in rewritten.Keys.OrderBy(f => f, StringComparer.Ordinal))
            {
                var program = ProgramOf(dest, file);
                if (FindObject(program) is { } stale) Reload(stale);
                try { loaded.Add(LoadObject(program)); }
                catch (MudRuntimeException e) when (e.Message.Contains("is abstract")) { }
            }
        }
        catch (Exception e) // also a fault in a published file's Create, which need not be a MudRuntimeException
        {
            foreach (var ob in loaded) if (ob.IsAlive) { try { Destruct(ob); } catch (MudRuntimeException) { } }
            foreach (var path in created) { try { RemoveFile(path); } catch (MudRuntimeException) { } }
            foreach (var (path, text) in backup) { try { WriteFile(path, text); } catch (MudRuntimeException) { } }
            for (var i = createdDirs.Count - 1; i >= 0; i--) { try { RemoveDir(createdDirs[i]); } catch (MudRuntimeException) { } }
            foreach (var path in backup.Keys) { try { UpdateProgram(path.Substring(0, path.Length - 3)); } catch (MudRuntimeException) { } }
            return $"Not approved: publishing failed, and nothing was left behind.\n{e.Message}\n";
        }

        if (prior > 0) Set(prior, "state", "superseded");
        DropCopy(id); // the published files are the copy now
        Set(id, "state", "approved");
        Set(id, "dest", dest);
        Set(id, "kind", kind);
        Set(id, "decider", actor.Name);
        Set(id, "decided", Time().ToString());
        Set(id, "submitted", Get(id, "files"));
        Set(id, "published", FileList(rewritten.ToDictionary(kv => kv.Key, kv => Sha256(kv.Value))));
        Store();
        var rooms = rewritten.Where(kv => kv.Value.Contains("@inherit /std/room")).Select(kv => ProgramOf(dest, kv.Key)).OrderBy(p => p, StringComparer.Ordinal).ToList();
        Notify(submitter, $"[publishing] {Cap(actor.Name)} approved your request #{id}: {source} is now {dest}.\n");
        return $"Request #{id} is approved and published to {dest} ({rewritten.Count} file(s), compiled and loaded).\n" +
               (rooms.Count > 0 ? $"Rooms: {string.Join(", ", rooms.Take(10))}. Players reach them only once a real room has an exit to one (a level 10 change).\n" : "");
    }

    private static string ProgramOf(string dest, string file) => dest + "/" + file.Substring(0, file.Length - 3);

    /// <summary>A loaded blueprint of an updated file is replaced; the people in it wait in the Void meanwhile.</summary>
    private void Reload(ObjectRef stale)
    {
        var occupants = AllInventory(stale).Where(Interactive).ToList();
        var limbo = LoadObject("/std/void");
        foreach (var ob in occupants) MoveObject(ob, limbo);
        Destruct(stale);
        var fresh = LoadObject(stale.ProgramPath);
        foreach (var ob in occupants) if (ob.IsAlive) MoveObject(ob, fresh);
    }

    // ------------------------------------------------------------------
    // Revoking
    // ------------------------------------------------------------------

    public string Revoke(int id, string reason)
    {
        if (Actor() is not { } actor) return Refused;
        if (actor.Level < LevelRevoke) return $"You need wizard level {LevelRevoke} to revoke a publication.\n";
        if (!records.ContainsKey(id + ".state")) return $"There is no request #{id}.\n";
        if (Get(id, "state") != "approved") return $"Request #{id} is {Get(id, "state")}: only the request now published can be revoked.\n";
        var dest = Get(id, "dest");
        var removed = 0;
        foreach (var file in ParseList(Get(id, "published")).Keys)
        {
            try { if (RemoveFile(dest + "/" + file)) removed++; }
            catch (MudRuntimeException) { }
        }
        // Empty directories go too, deepest first.
        var dirs = ParseList(Get(id, "published")).Keys.SelectMany(f =>
        {
            var parts = f.Split('/');
            return Enumerable.Range(1, parts.Length - 1).Select(i => dest + "/" + string.Join("/", parts.Take(i)));
        }).Distinct().OrderByDescending(d => d.Length).Append(dest).ToList();
        foreach (var dir in dirs) { try { RemoveDir(dir); } catch (MudRuntimeException) { } }
        Set(id, "state", "revoked");
        Set(id, "revoker", actor.Name);
        Set(id, "revoked", Time().ToString());
        Set(id, "reason", (reason ?? "").Trim());
        Store();
        Notify(Get(id, "submitter"), $"[publishing] {Cap(actor.Name)} revoked your publication #{id} at {dest}{((reason ?? "").Trim() != "" ? ": " + reason.Trim() : "")}.\n");
        return $"Publication #{id} is revoked: {removed} file(s) removed from {dest}. Objects already loaded from them run until they are destructed or the driver restarts.\n";
    }

    /// <summary>The record of a request, one field, for tests and the wizards' tools; "" if unknown.</summary>
    public string QueryRecord(int id, string field) => Get(id, field);
}
