#!/usr/bin/env python3
"""Lint the area files under mudlib/domains for common mistakes, without running the game.

Usage (from the repository root):
    python3 tests/lint_area.py [--mudlib DIR] [filter]

Scans every .cs file under mudlib/domains (skipping mudlib/domains/town and files
named area.cs). With a filter, only problems in files whose mudlib path (for
example /domains/hollow/village/square) contains the filter text are printed.
The cross-file checks still read every file they need, whatever the filter.

Prints one line per problem, "ERROR <mudlib path>: <message>" or
"WARN <mudlib path>: <message>", sorted by path, then a last line with the
counts. Exits with status 1 if there is any error, else 0.

Nothing is compiled. A small lexer skips comments and reads string literals
(plain, $-interpolated and @-verbatim); the checks then match patterns over the
token stream, so text inside comments and inside strings is never mistaken for code.
"""
import argparse
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_MUDLIB = os.path.join(os.path.dirname(HERE), "mudlib")

# Colour and command tags from docs/BUILDING.md, as thing.cs renders them. "/" ends every style.
TAGS = frozenset({
    "cmd", "title", "b", "u",
    "red", "green", "yellow", "blue", "magenta", "cyan", "white",
    "bred", "bgreen", "byellow", "bblue", "bmagenta", "bcyan", "bwhite",
})
CLOSE_TAG = "/"
PATH_PREFIXES = ("/domains/", "/std/", "/secure/", "/skills/", "/classes/")
FORBIDDEN_NAMES = ("All Hall", "Green Vale", "Oakenfall", "Oakfell")
STOCK_CALLS = ("AddStock", "AddRareStock")
TOP_DOMAINS_FOR_PEOPLE = ("hollow", "greenvale")

ERROR = "ERROR"
WARN = "WARN"

STR_START = re.compile(r'(\$@|@\$|\$|@)?"')
IDENT = re.compile(r"[A-Za-z_]\w*")
NUMBER = re.compile(r"\d\w*(?:\.\d+)?")
INHERIT_RE = re.compile(r"// @inherit (/std/\S+)")
TAG_TOKEN_RE = re.compile(r"\{\{|\{([^{}]*)\}")
INTERP_TOKEN_RE = re.compile(r"\{\{|\}\}|\{[^{}]*\}")
EOF = ("eof", "")


class Lit:
    """A string literal. raw is the text between the quotes, exactly as written."""
    __slots__ = ("raw", "interp", "verbatim")

    def __init__(self, raw, interp, verbatim):
        self.raw = raw
        self.interp = interp
        self.verbatim = verbatim


def lex(text):
    """Turn C# source into (kind, value) tokens. Comments are dropped."""
    toks = []
    i, n = 0, len(text)
    while i < n:
        ch = text[i]
        if ch.isspace():
            i += 1
            continue
        if text.startswith("//", i):
            end = text.find("\n", i)
            i = n if end < 0 else end
            continue
        if text.startswith("/*", i):
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
            continue
        m = STR_START.match(text, i)
        if m:
            prefix = m.group(1) or ""
            verbatim = "@" in prefix
            start = m.end()
            j = start
            while j < n:
                c2 = text[j]
                if verbatim and c2 == '"':
                    if text.startswith('""', j):
                        j += 2
                        continue
                    break
                if not verbatim and c2 == "\\":
                    j += 2
                    continue
                if not verbatim and c2 == '"':
                    break
                j += 1
            toks.append(("str", Lit(text[start:j], "$" in prefix, verbatim)))
            i = j + 1
            continue
        if ch == "'":
            j = i + 1
            while j < n and text[j] != "'":
                j += 2 if text[j] == "\\" else 1
            toks.append(("chr", text[i:j + 1]))
            i = j + 1
            continue
        m = IDENT.match(text, i)
        if m:
            toks.append(("id", m.group()))
            i = m.end()
            continue
        m = NUMBER.match(text, i)
        if m:
            toks.append(("num", m.group()))
            i = m.end()
            continue
        toks.append(("op", ch))
        i += 1
    return toks


def at(toks, k):
    return toks[k] if 0 <= k < len(toks) else EOF


def is_op(tok, ch):
    return tok[0] == "op" and tok[1] == ch


def is_id(tok, name):
    return tok[0] == "id" and tok[1] == name


def is_str(tok):
    return tok[0] == "str"


def assignments(toks, name):
    """Every `name = <literal> + <literal> ...;` as (list of Lit, whole value is literal)."""
    found = []
    for k in range(len(toks)):
        if not is_id(toks[k], name) or not is_op(at(toks, k + 1), "="):
            continue
        if is_op(at(toks, k + 2), "="):  # ==
            continue
        if is_op(at(toks, k - 1), ".") and not is_id(at(toks, k - 2), "this"):
            continue  # somebody else's field
        lits = []
        ok = True
        expect_str = True
        j = k + 2
        while j < len(toks) and not is_op(toks[j], ";"):
            if expect_str and is_str(toks[j]):
                lits.append(toks[j][1])
                expect_str = False
            elif not expect_str and is_op(toks[j], "+"):
                expect_str = True
            else:
                ok = False
                break
            j += 1
        if not is_op(at(toks, j), ";") or not lits or expect_str:
            ok = False
        found.append((lits, ok))
    return found


def exits_in(toks):
    """(direction, destination) for every `Exits["dir"] = "/path";` with plain literals."""
    out = []
    for k in range(len(toks)):
        if not (is_id(toks[k], "Exits") and is_op(at(toks, k + 1), "[") and is_str(at(toks, k + 2))
                and is_op(at(toks, k + 3), "]") and is_op(at(toks, k + 4), "=") and is_str(at(toks, k + 5))):
            continue
        key, dest = toks[k + 2][1], toks[k + 5][1]
        if key.interp or dest.interp:
            continue
        out.append((key.raw, dest.raw))
    return out


def detail_keys(toks):
    """The first argument of every AddDetail("keys", ...)."""
    return [at(toks, k + 2)[1].raw for k in range(len(toks))
            if is_id(toks[k], "AddDetail") and is_op(at(toks, k + 1), "(") and is_str(at(toks, k + 2))]


def stock_calls(toks):
    """(call name, path Lit or None, price text) for every AddStock and AddRareStock."""
    out = []
    for k in range(len(toks)):
        if toks[k][0] != "id" or toks[k][1] not in STOCK_CALLS:
            continue
        if not (is_op(at(toks, k + 1), "(") and is_str(at(toks, k + 2)) and is_op(at(toks, k + 3), ",")):
            continue
        path = toks[k + 2][1]
        j, depth, price = k + 4, 0, []
        while j < len(toks):
            t = toks[j]
            if depth == 0 and (is_op(t, ",") or is_op(t, ")")):
                break
            if is_op(t, "("):
                depth += 1
            elif is_op(t, ")"):
                depth -= 1
            price.append('"' + t[1].raw + '"' if t[0] == "str" else t[1])
            j += 1
        out.append((toks[k][1], None if path.interp else path, "".join(price)))
    return out


def ids_adds(toks):
    """Each Ids.Add(...) as a Lit, or None when the argument is not a plain literal."""
    out = []
    for k in range(len(toks)):
        if (is_id(toks[k], "Ids") and is_op(at(toks, k + 1), ".") and is_id(at(toks, k + 2), "Add")
                and is_op(at(toks, k + 3), "(")):
            arg = at(toks, k + 4)
            out.append(arg[1] if is_str(arg) and not arg[1].interp else None)
    return out


def brace_tokens(view):
    """The content of each {tag} or {word} in a plain string view; {{ is an escaped brace."""
    out = []
    for m in TAG_TOKEN_RE.finditer(view):
        if m.group(0) != "{{":
            out.append(m.group(1))
    return out


def interp_view(raw):
    """An interpolated string with its C# holes {expr} removed, leaving the text the player sees."""
    return INTERP_TOKEN_RE.sub(lambda m: "{{" if m.group(0) == "{{" else "", raw)


def lit_view(lit):
    return interp_view(lit.raw) if lit.interp else lit.raw


def tag_problems(view):
    problems = []
    opened = []
    for content in brace_tokens(view):
        if content == CLOSE_TAG:
            opened = []
        elif content in TAGS:
            opened.append(content)
        else:
            problems.append("unknown brace token {" + content + "} in a string")
    for tag in opened:
        problems.append("{" + tag + "} is never closed with {/} in the same string")
    return problems


def parse_price(text):
    try:
        return float(text)
    except ValueError:
        return None


class Source:
    """One .cs file: its first line, its tokens and the classes it declares."""

    def __init__(self, mudpath, abspath):
        self.path = mudpath
        with open(abspath, encoding="utf-8-sig", errors="replace") as fh:
            text = fh.read()
        self.first_line = next((ln.strip() for ln in text.splitlines() if ln.strip()), "")
        m = INHERIT_RE.fullmatch(self.first_line)
        self.inherit = m.group(1) if m else None
        self.toks = lex(text)
        self.classes = [self.toks[k + 2][1] for k in range(len(self.toks))
                        if is_id(self.toks[k], "public") and is_id(at(self.toks, k + 1), "class")
                        and at(self.toks, k + 2)[0] == "id"]


class Mudlib:
    def __init__(self, root):
        self.root = root
        self.cache = {}

    def abspath(self, mudpath):
        return os.path.join(self.root, mudpath.lstrip("/") + ".cs")

    def is_file(self, mudpath):
        # A save record names a file plus instance state ("/std/skin?p.kind=wolf"): the file is what must exist.
        return os.path.isfile(self.abspath(mudpath.split("?")[0]))

    def is_path(self, mudpath):
        return self.is_file(mudpath) or os.path.isdir(os.path.join(self.root, mudpath.lstrip("/")))

    def source(self, mudpath):
        if mudpath not in self.cache:
            self.cache[mudpath] = Source(mudpath, self.abspath(mudpath))
        return self.cache[mudpath]

    def all_paths(self):
        out = []
        for dirpath, _dirnames, filenames in os.walk(self.root):
            for fn in filenames:
                if fn.endswith(".cs"):
                    rel = os.path.relpath(os.path.join(dirpath, fn), self.root).replace(os.sep, "/")
                    out.append("/" + rel[:-3])
        return sorted(out)


def in_scope(mudpath):
    parts = mudpath.split("/")
    if len(parts) < 3 or parts[1] != "domains":
        return False
    if parts[2] == "town" or parts[-1] == "area":
        return False
    return True


def run(root, flt):
    lib = Mudlib(root)
    everything = lib.all_paths()
    scoped = [p for p in everything if in_scope(p)]
    problems = []

    def report(path, check, sev, msg):
        problems.append((path, check, sev, msg))

    # Check 5: class names are unique across the whole mudlib.
    by_class = {}
    for p in everything:
        for name in lib.source(p).classes:
            by_class.setdefault(name, []).append(p)
    for name, paths in by_class.items():
        if len(paths) > 1:
            for p in paths:
                others = ", ".join(o for o in paths if o != p)
                report(p, 5, ERROR, f"public class {name} is also declared in {others}")

    people = {}  # (top-level domain, first word of Name, lower case) -> [(display word, path)]

    for p in scoped:
        src = lib.source(p)
        parts = p.split("/")
        is_room = src.inherit == "/std/room"
        is_npc = "npc" in parts[:-1]
        exits = exits_in(src.toks)

        # Check 4: the inherit line.
        if src.inherit is None:
            report(p, 4, ERROR, f"first non-blank line is not '// @inherit /std/<name>': {src.first_line[:60]!r}")
        elif not lib.is_file(src.inherit):
            report(p, 4, ERROR, f"inherits {src.inherit}, which does not exist")

        # Check 1: every exit names an existing file.
        for direction, dest in exits:
            if not lib.is_file(dest):
                report(p, 1, ERROR, f'exit "{direction}" leads to {dest}, which does not exist')

        # Check 2: every exit has a way back (a warning: one-way exits are sometimes deliberate).
        for direction, dest in exits:
            if lib.is_file(dest):
                back = [d for _, d in exits_in(lib.source(dest).toks)]
                if p not in back:
                    report(p, 2, WARN, f'exit "{direction}" to {dest} has no exit back to {p}')

        # Checks 3, 7 and 8 and 9 look at every string literal in the file.
        strings = [t[1] for t in src.toks if t[0] == "str"]
        for lit in strings:
            # Check 3: path strings name something that exists.
            if not lit.interp and lit.raw.startswith(PATH_PREFIXES) and not lib.is_path(lit.raw):
                report(p, 3, ERROR, f"string names {lit.raw}, which does not exist")
            # Check 7: colour tags are documented and closed.
            for msg in tag_problems(lit_view(lit)):
                report(p, 7, ERROR, msg)
            # Check 8: no prompt text.
            if "> " in lit.raw:
                report(p, 8, ERROR, f'string contains "> " (prompt text): {lit.raw[:60]!r}')
            # Check 9: forbidden names.
            for name in FORBIDDEN_NAMES:
                if re.search(r"\b" + re.escape(name), lit.raw, re.IGNORECASE):
                    report(p, 9, ERROR, f'string contains the forbidden name "{name}"')

        # Check 7 (continued): no tags at all in Name, ShortDesc or Ids.Add.
        for field in ("Name", "ShortDesc"):
            for lits, _ok in assignments(src.toks, field):
                for lit in lits:
                    for content in brace_tokens(lit_view(lit)):
                        if content == CLOSE_TAG or content in TAGS:
                            report(p, 7, ERROR, f"tag {{{content}}} inside {field}")
        for lit in ids_adds(src.toks):
            if lit is None:
                continue
            for content in brace_tokens(lit_view(lit)):
                if content == CLOSE_TAG or content in TAGS:
                    report(p, 7, ERROR, f"tag {{{content}}} inside an Ids.Add")

        # Check 13: shop stock names an existing file and a positive price.
        for call, path, price in stock_calls(src.toks):
            if path is not None and not lib.is_file(path.raw):
                report(p, 13, ERROR, f"{call} names {path.raw}, which does not exist")
            value = parse_price(price)
            if value is None or value <= 0:
                report(p, 13, ERROR, f"{call} price {price!r} is not a positive number")

        if is_room:
            short_ok = bool(assignments(src.toks, "ShortDesc"))
            long_found = assignments(src.toks, "LongDesc")
            # Check 10: rooms have the basics.
            if not short_ok:
                report(p, 10, ERROR, "room has no ShortDesc")
            if not long_found:
                report(p, 10, ERROR, "room has no LongDesc")
            if not exits:
                report(p, 10, ERROR, "room has no exit")
            details = detail_keys(src.toks)
            if not details:
                report(p, 10, WARN, "room has no AddDetail")

            # Check 12: each detail's keys appear in the description.
            if long_found and all(ok for _, ok in long_found):
                long_text = "".join(lit.raw for lits, _ in long_found for lit in lits).lower()
                for raw_keys in details:
                    keys = [k.strip().lower() for k in raw_keys.split(",") if k.strip()]
                    if keys and not any(k in long_text for k in keys):
                        report(p, 12, WARN, f'AddDetail "{raw_keys}" matches no word in LongDesc')

        if is_npc:
            # Check 11: people have the basics.
            for field in ("Name", "ShortDesc", "LongDesc"):
                if not assignments(src.toks, field):
                    report(p, 11, ERROR, f"person has no {field}")
            if not ids_adds(src.toks):
                report(p, 11, ERROR, "person has no Ids.Add")

            # Check 6 input: the first word of Name.
            if parts[2] in TOP_DOMAINS_FOR_PEOPLE:
                names = assignments(src.toks, "Name")
                if names and names[0][0]:
                    words = names[0][0][0].raw.split()
                    if words:
                        key = (parts[2], words[0].lower())
                        people.setdefault(key, []).append((words[0], p))

    # Check 6: no two people in the same top-level domain share a first name (a warning).
    for (_domain, _word), entries in people.items():
        if len(entries) > 1:
            for word, p in entries:
                others = ", ".join(o for _, o in entries if o != p)
                report(p, 6, WARN, f'first name "{word}" is also used by {others}')

    shown = [r for r in problems if flt in r[0]]
    shown.sort(key=lambda r: (r[0], r[1], r[3]))
    for path, _check, sev, msg in shown:
        print(f"{sev} {path}: {msg}")
    errors = sum(1 for r in shown if r[2] == ERROR)
    warnings = sum(1 for r in shown if r[2] == WARN)
    print(f"{errors} error(s), {warnings} warning(s)")
    return 1 if errors else 0


def main(argv=None):
    ap = argparse.ArgumentParser(description="Lint the area files under mudlib/domains without running the game.")
    ap.add_argument("filter", nargs="?", default="",
                    help="only report files whose mudlib path contains this text")
    ap.add_argument("--mudlib", default=DEFAULT_MUDLIB,
                    help="the mudlib directory to lint (default: this repository's mudlib)")
    args = ap.parse_args(argv)
    return run(os.path.abspath(args.mudlib), args.filter)


if __name__ == "__main__":
    sys.exit(main())
