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


def gated_exits_in(toks):
    """(direction, destination) for every `GatedExit("dir", "/path", ...)` with plain literals (docs/specs/hidden-classes.md section 6)."""
    out = []
    for k in range(len(toks)):
        if (is_id(toks[k], "GatedExit") and is_op(at(toks, k + 1), "(") and is_str(at(toks, k + 2)) and is_op(at(toks, k + 3), ",")
                and is_str(at(toks, k + 4)) and not toks[k + 2][1].interp and not toks[k + 4][1].interp):
            out.append((toks[k + 2][1].raw, toks[k + 4][1].raw))
    return out


def doors_in(toks):
    """The direction of every `AddDoor("dir", ...)` with a plain literal."""
    out = []
    for k in range(len(toks)):
        if is_id(toks[k], "AddDoor") and is_op(at(toks, k + 1), "(") and is_str(at(toks, k + 2)) and not toks[k + 2][1].interp:
            out.append(toks[k + 2][1].raw)
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
        self.text = text
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


HIDE_CALLS = ("HideExit", "HiddenExit", "HideItem", "HideNode", "HideClue")
DIFFICULTY_NAMES = {"SearchEasy": 7, "SearchModerate": 12, "SearchHard": 17, "SearchVeryHard": 22}


def call_args(toks, k):
    """The arguments of the call whose name is at toks[k]: a list of token lists, split at top-level commas (named-argument labels are dropped)."""
    if not is_op(at(toks, k + 1), "("):
        return None
    args, cur, depth, j = [], [], 0, k + 2
    while j < len(toks):
        t = toks[j]
        if is_op(t, "(") or is_op(t, "[") or is_op(t, "{"):
            depth += 1
        elif is_op(t, ")") or is_op(t, "]") or is_op(t, "}"):
            if depth == 0:
                break
            depth -= 1
        if depth == 0 and is_op(t, ","):
            args.append(cur)
            cur = []
        else:
            cur.append(t)
        j += 1
    if cur:
        args.append(cur)
    out = []
    for arg in args:
        if len(arg) >= 3 and arg[0][0] == "id" and is_op(arg[1], ":") and not is_op(at(arg, 2), ":"):
            arg = arg[2:]  # a named argument: byFeel: true
        out.append(arg)
    return out


def literal(arg):
    """The text of a plain string literal argument, or None."""
    return arg[0][1].raw if len(arg) == 1 and arg[0][0] == "str" and not arg[0][1].interp else None


def hidden_calls(toks):
    """Every hide call of a room, as (call name, [args])."""
    out = []
    for k in range(len(toks)):
        if toks[k][0] == "id" and toks[k][1] in HIDE_CALLS and not is_op(at(toks, k - 1), "."):
            args = call_args(toks, k)
            if args is not None:
                out.append((toks[k][1], args))
    return out


def difficulty_of(arg):
    """The difficulty an argument spells: a constant of /std/room or a number; None if it is neither."""
    if len(arg) == 1 and arg[0][0] == "id" and arg[0][1] in DIFFICULTY_NAMES:
        return DIFFICULTY_NAMES[arg[0][1]]
    if len(arg) == 1 and arg[0][0] == "num" and arg[0][1].isdigit():
        return int(arg[0][1])
    return None


def number_assign(toks, name):
    """`name = 12;` as 12, or None."""
    for k in range(len(toks)):
        if is_id(toks[k], name) and is_op(at(toks, k + 1), "=") and not is_op(at(toks, k + 2), "=") and at(toks, k + 2)[0] == "num" and is_op(at(toks, k + 3), ";"):
            text = toks[k + 2][1]
            if text.isdigit():
                return int(text)
    return None


def spawn_paths(toks):
    """The path of every AddSpawn("path"...) with a plain literal."""
    return [at(toks, k + 2)[1].raw for k in range(len(toks))
            if is_id(toks[k], "AddSpawn") and is_op(at(toks, k + 1), "(") and is_str(at(toks, k + 2)) and not at(toks, k + 2)[1].interp]


def hidden_exits_in(toks):
    """(direction, destination) of every HideExit or HiddenExit with plain literals."""
    out = []
    for name, args in hidden_calls(toks):
        if name in ("HideExit", "HiddenExit") and len(args) >= 2 and literal(args[0]) and literal(args[1]):
            out.append((literal(args[0]), literal(args[1])))
    return out


def in_scope(mudpath):
    parts = mudpath.split("/")
    if len(parts) < 3 or parts[1] != "domains":
        return False
    if parts[2] == "town" or parts[-1] == "area":
        return False
    return True


# ---- Check 17: tags (docs/specs/tags-and-saves.md section 3). Check 18: what a name or a description says about a weapon. ----

TAG_SCOPES = ("/std/", "/secure/", "/skills/", "/classes/", "/domains/")
DERIVED_TAGS = {"boss": "Boss = true;", "alert": "Alert = true;", "darksight": "DarkSight (it is true unless the file sets DarkSight = false;)", "elite": "Elite = true;"}

# A word in the name of an action or a weapon says what it does: (stem, kind it names or None, damage type it needs or None). The stems are the
# start of a word; "crush" and "maul" are left out on purpose (a crushing blow may be struck with a blade).
NAME_WORDS = (
    ("spear", "spear", "piercing"), ("jab", None, "piercing"), ("stab", None, "piercing"), ("thrust", None, "piercing"), ("pierc", None, "piercing"),
    ("dagger", "dagger", "piercing"), ("dirk", "dagger", "piercing"), ("fang", None, "piercing"), ("nip", None, "piercing"), ("gore", "gore", "piercing"),
    ("pick", "pick", "piercing"), ("bite", "bite", "piercing"),
    ("slash", None, "slashing"), ("cleav", None, "slashing"), ("hack", None, "slashing"), ("sabre", "sword", "slashing"), ("sword", "sword", "slashing"),
    ("axe", "axe", "slashing"), ("claw", "claw", "slashing"), ("swipe", None, "slashing"), ("rak", None, "slashing"),
    ("hammer", "mace", "blunt"), ("mace", "mace", "blunt"), ("club", "club", "blunt"), ("staff", "staff", "blunt"), ("bash", None, "blunt"),
    ("smash", None, "blunt"), ("pommel", None, "blunt"), ("kick", "kick", "blunt"), ("slam", "slam", "blunt"), ("lash", "lash", "blunt"),
    ("touch", "touch", "blunt"), ("fist", None, "blunt"),
)
WEAPON_NAME_WORDS = (("sword", "sword"), ("sabre", "sword"), ("dagger", "dagger"), ("dirk", "dagger"), ("spear", "spear"), ("axe", "axe"), ("mace", "mace"),
                     ("staff", "staff"), ("bow", "bow"), ("longbow", "bow"), ("recurve", "bow"))
# A carried weapon a description may name, and the kind an action must then have. Natural weapons (teeth, claws) are not carried.
CARRIED_WORDS = (("sword", "sword"), ("sabre", "sword"), ("longsword", "sword"), ("greatsword", "sword"), ("cutlass", "sword"), ("dagger", "dagger"), ("knife", "dagger"),
                 ("spear", "spear"), ("axe", "axe"), ("cleaver", "axe"), ("hatchet", "axe"), ("mace", "mace"), ("hammer", "mace"), ("club", "club"), ("cudgel", "club"),
                 ("pick", "pick"), ("staff", "staff"))


def read_registry(lib):
    """The vocabulary of /secure/tags, read from its text: families {name: (exclusive, active, carriers, tags)}, only, implied, ladders."""
    if not lib.is_file("/secure/tags"):
        return None
    text = lib.source("/secure/tags").text
    fam = {}
    for m in re.finditer(r'^\s*Family\("(\w+)",\s*(Exclusive|Open),\s*(Active|Reserved),\s*"([^"]*)",\s*"([^"]*)"\);', text, re.M):
        fam[m.group(1)] = (m.group(2) == "Exclusive", m.group(3) == "Active", m.group(4).split(), m.group(5).split())
    only = {(m.group(1), m.group(2)): m.group(3).split() for m in re.finditer(r'Only\("(\w+)",\s*"(\w+)",\s*"([^"]*)"\)', text)}
    implied = {m.group(1): (m.group(2), m.group(3)) for m in re.finditer(r'Implied\("(\w+)",\s*"(\w+)",\s*"(\w+)"\)', text)}
    ladders = {(m.group(1), m.group(2)) for m in re.finditer(r'Ladder\("(\w+)",\s*"(\w+)"\)', text)}
    natural = set()
    if lib.is_file("/secure/blows"):
        m = re.search(r'Natural\("([^"]*)"\)', lib.source("/secure/blows").text)
        natural = set(m.group(1).split()) if m else set()
    return fam, only, implied, ladders, natural


def family_of(reg, tag):
    fam = reg[0]
    for name, (_ex, _act, _car, tags) in fam.items():
        if tag in tags:
            return name
    if re.fullmatch(r"(weak|resists|immune)-(fire|frost|shock|holy|shadow)", tag):
        return "affinity"
    return None


def call_parts(toks, k):
    """The arguments of the call at toks[k] as (positional token lists, {name: token list})."""
    if not is_op(at(toks, k + 1), "("):
        return None
    pos, named, cur, depth, j = [], {}, [], 0, k + 2

    def flush():
        if not cur:
            return
        if len(cur) >= 3 and cur[0][0] == "id" and is_op(cur[1], ":") and not is_op(at(cur, 2), ":"):
            named[cur[0][1]] = cur[2:]
        else:
            pos.append(list(cur))

    while j < len(toks):
        t = toks[j]
        if is_op(t, "(") or is_op(t, "[") or is_op(t, "{"):
            depth += 1
        elif is_op(t, ")") or is_op(t, "]") or is_op(t, "}"):
            if depth == 0:
                break
            depth -= 1
        if depth == 0 and is_op(t, ","):
            flush()
            cur = []
        else:
            cur.append(t)
        j += 1
    flush()
    return pos, named


def tag_literals(toks):
    """Every `Tags.Add("x")` / `Tags.AddRange(new[] { "a", "b" })` in order, as a list of literal strings."""
    out = []
    for k in range(len(toks)):
        if is_id(toks[k], "Tags") and is_op(at(toks, k + 1), ".") and at(toks, k + 2)[0] == "id" and at(toks, k + 2)[1] in ("Add", "AddRange"):
            parts = call_parts(toks, k + 2)
            if parts:
                for arg in parts[0]:
                    out.extend(t[1].raw for t in arg if is_str(t) and not t[1].interp)
    return out


def string_after_call(toks, name):
    """Literal first arguments of every `name("...")` call."""
    out = []
    for k in range(len(toks)):
        if is_id(toks[k], name) and is_op(at(toks, k + 1), "(") and is_str(at(toks, k + 2)):
            out.append(at(toks, k + 2)[1].raw)
    return out


def lit_value(tokens):
    return tokens[0][1].raw if len(tokens) == 1 and is_str(tokens[0]) and not tokens[0][1].interp else None


def lint_tags(lib, everything, report):
    reg = read_registry(lib)
    if reg is None:
        return
    fam, only, implied, ladders, natural = reg
    exclusive = {n for n, v in fam.items() if v[0]}
    for p in everything:
        if not p.startswith(TAG_SCOPES) or p == "/secure/tags" or p.startswith("/domains/town/") or p.split("/")[-1] == "area":
            continue
        src = lib.source(p)
        toks = src.toks
        # 1. Every tag a file writes is registered; a reserved one is an error; a derived one is declared with its flag.
        declared = tag_literals(toks)
        for tag in declared + string_after_call(toks, "HasTag"):
            if tag in DERIVED_TAGS and tag in declared:
                report(p, 17, ERROR, f"Tags.Add(\"{tag}\"): declare it with {DERIVED_TAGS[tag]} (the tag is derived from the flag)")
                continue
            f = family_of(reg, tag)
            if f is None:
                report(p, 17, ERROR, f"tag '{tag}' is not registered in /secure/tags")
            elif not fam[f][1]:
                report(p, 17, ERROR, f"tag '{tag}' is reserved: no rule reads it yet")
        for f in string_after_call(toks, "QueryTagIn"):
            if f not in fam:
                report(p, 17, ERROR, f"QueryTagIn(\"{f}\"): '{f}' is not a registered family")
        # 3. Two tags of one exclusive family in one file.
        seen = {}
        for tag in declared:
            f = family_of(reg, tag)
            if f in exclusive:
                if f in seen and seen[f] != tag:
                    report(p, 17, ERROR, f"tags '{seen[f]}' and '{tag}' are both of the exclusive family '{f}'")
                seen.setdefault(f, tag)
        if "Boss = true" in re.sub(r"\s+", " ", src.text) and "Elite = true" in re.sub(r"\s+", " ", src.text):
            report(p, 17, ERROR, "Boss = true and Elite = true: a creature has one rank")
        if not p.startswith("/domains/"):
            continue
        # 4. Weapons, armour and creatures state what they are.
        carrier = {"/std/weapon": "weapon", "/std/armor": "armour", "/std/monster": "creature"}.get(src.inherit or "")
        if carrier is None:
            continue
        for tag in declared:
            f = family_of(reg, tag)
            if f and fam[f][1] and carrier not in fam[f][2]:
                report(p, 17, ERROR, f"tag '{tag}' ({f}) is not for a {carrier}")
            if f and (carrier, f) in only and tag not in only[(carrier, f)]:
                report(p, 17, ERROR, f"tag '{tag}' is not a {f} of a {carrier}")
        have = {f: seen.get(f) for f in ("reach", "damage", "kind", "armour")}
        text_one = re.sub(r"\s+", " ", src.text)
        if carrier == "weapon":
            for f in ("reach", "damage", "kind"):
                if not have[f]:
                    report(p, 17, ERROR, f"weapon states no {f} tag")
            cat = re.search(r'\bCategory = "(\w*)";', text_one)
            if have["kind"] and cat and cat.group(1) != have["kind"]:
                report(p, 17, ERROR, f"weapon kind '{have['kind']}' is not its Category '{cat.group(1)}'")
        elif carrier == "armour":
            if not have["armour"]:
                report(p, 17, ERROR, "armour states no armour tag")
            else:
                slot = re.search(r'\bSlot = "(\w*)";', text_one)
                cat = re.search(r'\bCategory = "(\w*)";', text_one)
                want = slot.group(1) if slot and slot.group(1) in ("shield", "trinket") else (cat.group(1) if cat else None)
                if want is not None and want != have["armour"]:
                    report(p, 17, ERROR, f"armour tag '{have['armour']}' is not its {'Slot' if want == (slot.group(1) if slot else None) else 'Category'} '{want}'")
        else:
            for f in ("reach", "damage", "kind"):
                if not have[f]:
                    report(p, 17, ERROR, f"creature states no {f} tag for its basic attack")
        # 5. Every (reach, damage) pair has a ladder, or its kind has its own.
        pairs = [(have["reach"], have["damage"], have["kind"], "its basic attack")] if carrier in ("weapon", "creature") else []
        if carrier == "creature":
            for k in range(len(toks)):
                if is_id(toks[k], "AddCombatAction"):
                    parts = call_parts(toks, k)
                    if not parts:
                        continue
                    pos, named = parts
                    label = lit_value(pos[1]) if len(pos) > 1 else "an action"
                    for key, f in (("reach", "reach"), ("damage", "damage"), ("tagKind", "kind")):
                        if key in named:
                            v = lit_value(named[key])
                            if v is None:
                                continue
                            fm = family_of(reg, v)
                            if fm != f:
                                report(p, 17, ERROR, f"action {label}: {key} '{v}' is not a {f} tag")
                    if any(k2 in named for k2 in ("reach", "damage", "tagKind")):
                        r = lit_value(named.get("reach", [])) or have["reach"]
                        d = lit_value(named.get("damage", [])) or have["damage"]
                        kd = lit_value(named.get("tagKind", [])) or have["kind"]
                        pairs.append((r, d, kd, f"action {label}"))
        for r, d, kd, label in pairs:
            if r and d and (r, d) not in ladders and kd not in natural:
                report(p, 17, ERROR, f"{label}: no hit-wording ladder for ({r}, {d}); write one in /secure/blows and a Ladder line in /secure/tags")


def lint_words(lib, everything, report):
    """Check 18: a weapon's name and a creature's action names agree with the tags; a creature's description names only weapons it can use (a warning)."""
    reg = read_registry(lib)
    if reg is None:
        return
    for p in everything:
        if not p.startswith("/domains/") or p.startswith("/domains/town/") or p.split("/")[-1] == "area":
            continue
        src = lib.source(p)
        if src.inherit not in ("/std/weapon", "/std/monster"):
            continue
        toks = src.toks
        declared = tag_literals(toks)
        have = {}
        for tag in declared:
            f = family_of(reg, tag)
            if f in ("reach", "damage", "kind"):
                have.setdefault(f, tag)
        if src.inherit == "/std/weapon":
            names = assignments(toks, "Name")
            words = re.findall(r"[a-z]+", " ".join(l.raw for lits, _ in names for l in lits).lower())
            for word, kind in WEAPON_NAME_WORDS:
                if word in words and have.get("kind") and have["kind"] != kind:
                    report(p, 18, ERROR, f"the name says '{word}' but the weapon's kind is '{have['kind']}'")
            continue
        # A creature: each action's name against its effective tags.
        effective = []
        for k in range(len(toks)):
            if not is_id(toks[k], "AddCombatAction"):
                continue
            parts = call_parts(toks, k)
            if not parts or len(parts[0]) < 2:
                continue
            pos, named = parts
            label = lit_value(pos[1]) or ""
            kind_literal = lit_value(named.get("kind", []))
            if kind_literal not in (None, "attack"):
                continue  # a cry, a summon, a buff: not a blow
            damage = lit_value(named.get("damage", [])) or have.get("damage")
            kind = lit_value(named.get("tagKind", [])) or have.get("kind")
            effective.append(kind)
            for word in re.findall(r"[a-z]+", label.lower()):
                for stem, want_kind, want_damage in NAME_WORDS:
                    if word.startswith(stem):
                        if want_damage and damage and damage != want_damage:
                            report(p, 18, ERROR, f"action '{label}' says '{word}' ({want_damage}) but its damage type is '{damage}'")
                        elif want_kind and kind and kind != want_kind:
                            report(p, 18, ERROR, f"action '{label}' says '{word}' ({want_kind}) but its kind is '{kind}'")
                        break
        if not re.search(r"lint-ok: weapon-words", src.text):
            desc = " ".join(l.raw for lits, _ in assignments(toks, "LongDesc") for l in lits).lower()
            named_kinds = set()
            for word, kind in CARRIED_WORDS:
                if re.search(r"\b" + word + r"\b(?!\s+(?:wound|scar|mark))", desc):
                    named_kinds.add((word, kind))
            for word, kind in sorted(named_kinds):
                if kind not in effective:
                    report(p, 18, WARN, f"the description names a {word} but no attack of the creature is of kind '{kind}'")


def lint_hidden(lib, p, src, exits, report):
    calls = hidden_calls(src.toks)
    spawned = spawn_paths(src.toks)
    finds = []
    for path in spawned:
        if lib.is_file(path) and (number_assign(lib.source(path).toks, "SearchDifficulty") or 0) > 0:
            finds.append(path)
    if not calls and not finds:
        return
    details = [{k.strip().lower() for k in raw.split(",") if k.strip()} for raw in detail_keys(src.toks)]
    detail_set = set().union(*details) if details else set()
    long_found = assignments(src.toks, "LongDesc")
    long_text = "".join(lit.raw for lits, _ in long_found for lit in lits).lower() if long_found and all(ok for _, ok in long_found) else None
    seen = {}

    def names_check(label, names):
        names = [n.strip().lower() for n in names.split(",") if n.strip()]
        if not names:
            return
        if not any(n in detail_set for n in names):
            report(p, 15, ERROR, f"{label}: none of its names ({', '.join(names)}) is an AddDetail key of the room")
        elif long_text is not None and names[0] not in long_text:
            report(p, 15, WARN, f"{label}: its first name '{names[0]}' does not appear in the room's LongDesc (a player cannot read their way to it)")

    for name, args in calls:
        if not args or literal(args[0]) is None:
            continue
        ident = literal(args[0])
        label = f"{name} {ident}"
        if ident in seen:
            report(p, 15, ERROR, f"{label}: id '{ident}' is used twice in the room")
        seen[ident] = name
        if name == "HiddenExit":
            if len(args) >= 3 and literal(args[2]):
                names_check(label, literal(args[2]))
            continue
        index = {"HideExit": 3, "HideItem": 2, "HideNode": 2, "HideClue": 1}[name]
        if len(args) <= index:
            report(p, 15, ERROR, f"{label}: no difficulty given")
            continue
        difficulty = difficulty_of(args[index])
        if difficulty is None or not (6 <= difficulty <= 25 or (name == "HideExit" and difficulty == 0)):
            report(p, 15, ERROR, f"{label}: the difficulty is not a constant (SearchEasy, SearchModerate, SearchHard, SearchVeryHard) or a number from 6 to 25")
        if name == "HideExit":
            far = literal(args[1]) if len(args) > 1 else None
            if far and lib.is_file(far):
                other = lib.source(far)
                back = [d for _, d in exits_in(other.toks)] + [d for _, d in hidden_exits_in(other.toks)]
                if p not in back:
                    report(p, 15, ERROR, f"{label}: the far room {far} has no exit back to {p}")
            elif far is None:
                report(p, 15, ERROR, f"{label}: the far room is not a plain path")
            if len(args) > 2 and literal(args[2]):
                names_check(label, literal(args[2]))
        elif name in ("HideItem", "HideNode"):
            if len(args) > 3 and literal(args[3]) is not None:
                names_check(label, literal(args[3]))
            path = literal(args[1]) if len(args) > 1 else None
            if name == "HideItem" and path and lib.is_file(path):
                value = number_assign(lib.source(path).toks, "Value")
                if value is not None and value > 5:
                    report(p, 15, WARN, f"{label}: the item is worth {value} gold; a once-per-reset item should be worth 5 or less (a hidden find is once per character)")
        else:  # HideClue
            if len(args) > 2 and literal(args[2]) is not None:
                names_check(label, literal(args[2]))
            if len(args) < 5 or not (literal(args[4]) or "").strip():
                report(p, 15, ERROR, f"{label}: a clue needs readNames (what `read` takes)")
    for path in finds:
        fsrc = lib.source(path)
        difficulty = number_assign(fsrc.toks, "SearchDifficulty")
        label = f"hidden find {path}"
        if difficulty is None or not 6 <= difficulty <= 25:
            report(p, 15, ERROR, f"{label}: SearchDifficulty {difficulty} is not a number from 6 to 25")
        for lits, ok in assignments(fsrc.toks, "SearchNames"):
            if ok:
                names_check(label, "".join(lit.raw for lit in lits))
        gold = number_assign(fsrc.toks, "Gold")
        if gold is not None and gold > 20:
            report(p, 15, WARN, f"{label}: Gold {gold} is more than 20 for a hidden find")
    # a shop is not a search puzzle
    for path in spawned:
        if lib.is_file(path) and stock_calls(lib.source(path).toks):
            report(p, 15, WARN, f"hidden things in a room that has a shopkeeper ({path})")
            break


# ---- Check 20: hidden classes (docs/specs/hidden-classes.md section 10) ----

def norm_words(text):
    """Lower case, every run of non-letters and non-digits one space, with a space at each end, for whole-word matching."""
    return " " + re.sub(r"[^a-z0-9]+", " ", text.lower()).strip() + " "


def string_values(toks, name):
    """Every string literal in the statements that mention `name` (e.g. Skills.Add("x"), Skills.AddRange(new[] { "x", "y" }))."""
    out = []
    k = 0
    while k < len(toks):
        if is_id(toks[k], name) and is_op(at(toks, k + 1), "."):
            j = k
            while j < len(toks) and not is_op(toks[j], ";"):
                if is_str(toks[j]):
                    out.append(toks[j][1].raw)
                j += 1
            k = j
        k += 1
    return out


class HiddenClassInfo:
    """What the lint needs to know of one hidden class, read from its class file and skill files."""

    def __init__(self, lib, path):
        src = lib.source(path)
        t = src.toks
        self.path = path
        self.id = path.rsplit("/", 1)[1]

        def first(name):
            for lits, ok in assignments(t, name):
                if ok:
                    return "".join(l.raw for l in lits)
            return ""
        self.title, self.name, self.tagline = first("Title"), first("ClassName"), first("Tagline")
        self.quest, self.gate = first("QuestId"), first("GateKey")
        self.own = [x for x in string_values(t, "OwnPaths") if x]
        self.skill_ids = string_values(t, "Skills")
        self.skill_paths = ["/skills/" + sid for sid in self.skill_ids]
        self.skill_titles = []
        for sp in self.skill_paths:
            if lib.is_file(sp):
                title = ""
                for lits, ok in assignments(lib.source(sp).toks, "Title"):
                    if ok:
                        title = "".join(l.raw for l in lits)
                        break
                if title:
                    self.skill_titles.append(title)
        # Words that may not appear in public text. The quest id, hyphen as a space, only counts outside the class's own files.
        self.words = [w for w in {self.id, self.title, self.name, *self.skill_ids, *self.skill_titles, self.tagline.split("\n")[0].split(". ")[0]} if len(w.strip()) >= 3]
        self.quest_words = [self.quest.replace("-", " ")] if self.quest else []

    def is_own(self, mudpath):
        if mudpath == self.path or mudpath in self.skill_paths:
            return True
        return any(mudpath.startswith(o) for o in self.own)

    def hits(self, text, with_quest):
        n = norm_words(text)
        found = [w for w in self.words if norm_words(w) in n]
        if with_quest:
            found += [w for w in self.quest_words if norm_words(w) in n]
        return found


def repo_root_of(lib):
    return os.path.dirname(lib.root)


PUBLIC_TEXT_EXT = (".md", ".json", ".html", ".htm", ".js", ".css", ".txt", ".lua", ".xml", ".sh", ".py", ".razor", ".cshtml", ".yml", ".yaml", ".toml", ".service", ".timer", ".conf")
SKIP_DIRS = {"bin", "obj", ".git", "node_modules", "__pycache__", "wwwroot-lib"}


def public_files(root):
    """(relative path, absolute path) of every file a visitor or a player could be shown something from, outside the mudlib: web, clients, deploy, README and docs/*.md except docs/specs."""
    for top in ("web", "clients", "deploy"):
        base = os.path.join(root, top)
        for dirpath, dirnames, filenames in os.walk(base):
            dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
            for fn in filenames:
                yield os.path.relpath(os.path.join(dirpath, fn), root).replace(os.sep, "/"), os.path.join(dirpath, fn)
    for fn in sorted(os.listdir(root)) if os.path.isdir(root) else []:
        if fn.endswith(".md"):
            yield fn, os.path.join(root, fn)
    docs = os.path.join(root, "docs")
    for fn in sorted(os.listdir(docs)) if os.path.isdir(docs) else []:
        if fn.endswith(".md"):
            yield "docs/" + fn, os.path.join(docs, fn)


def lint_secret(lib, everything, report):
    classes = []
    for p in everything:
        if not p.startswith("/classes/"):
            continue
        text = lib.source(p).text
        if re.search(r"\bHidden\s*=\s*true\s*;", text):
            classes.append(HiddenClassInfo(lib, p))
    if not classes:
        return
    root = repo_root_of(lib)
    flt_path = os.path.join(root, "web", "LesMudOS.Web", "publicfilter.json")
    flt_text = open(flt_path, encoding="utf-8", errors="replace").read() if os.path.isfile(flt_path) else ""
    flt_clean = re.sub(r"(?m)^\s*//.*$", "", flt_text)
    for c in classes:
        here = c.path
        if not c.own:
            report(here, 20, WARN, "a hidden class has no OwnPaths (the files that may speak of it)")
        if c.gate and (norm_words(c.id) in norm_words(c.gate) or (c.title and norm_words(c.title) in norm_words(c.gate)) or norm_words(c.gate).strip() in norm_words(c.id)):
            report(here, 20, ERROR, f'the gate key "{c.gate}" contains the class id or title (it appears in a public room file)')
        if re.search(r'"class\.hidden"', flt_clean) is None:
            report(here, 20, ERROR, 'class.hidden is missing from hiddenEventPrefixes in publicfilter.json')
        m = re.search(r'"publicClasses"\s*:\s*\[(.*?)\]', flt_clean, re.S)
        if m and re.search(r'"' + re.escape(c.id) + r'"', m.group(1)):
            report(here, 20, ERROR, f'the class id "{c.id}" is in publicClasses')
        # Strings in the mudlib: every literal (comments are dropped by the lexer), except in the class's own files.
        for p in everything:
            if c.is_own(p):
                continue
            src = lib.source(p)
            for kind, val in src.toks:
                if kind != "str":
                    continue
                raw = val.raw
                if raw.startswith(PATH_PREFIXES):
                    continue
                for w in c.hits(raw, True):
                    report(p, 20, ERROR, f'a string shows the hidden class ({w!r}): {raw[:60]!r}')
        for p in everything:
            if any(p.startswith(o) for o in c.own) and re.search(r'"class\.advanced"', lib.source(p).text):
                report(p, 20, ERROR, 'a file of the hidden class reports class.advanced (use class.hidden)')
        for rel, absp in public_files(root):
            try:
                text = open(absp, encoding="utf-8", errors="replace").read()
            except OSError:
                continue
            if rel.endswith(".cs"):
                toks = lex(text)
                strings = [v.raw for k, v in toks if k == "str"]
                text = "\n".join(strings)
            elif not rel.endswith(PUBLIC_TEXT_EXT):
                continue
            elif rel.endswith("publicfilter.json"):
                text = flt_clean
            for w in c.hits(text, True):
                report("/" + rel, 20, ERROR, f'public text shows the hidden class ({w!r})')


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
        gated = gated_exits_in(src.toks)

        # Check 4: the inherit line.
        if src.inherit is None:
            report(p, 4, ERROR, f"first non-blank line is not '// @inherit /std/<name>': {src.first_line[:60]!r}")
        elif not lib.is_file(src.inherit):
            report(p, 4, ERROR, f"inherits {src.inherit}, which does not exist")

        # Check 1: every exit names an existing file.
        for direction, dest in exits + gated:
            if not lib.is_file(dest):
                report(p, 1, ERROR, f'exit "{direction}" leads to {dest}, which does not exist')

        # Check 2: every exit has a way back (a warning: one-way exits are sometimes deliberate).
        for direction, dest in exits + gated:
            if lib.is_file(dest):
                back = [d for _, d in exits_in(lib.source(dest).toks) + gated_exits_in(lib.source(dest).toks)]
                if p not in back:
                    report(p, 2, WARN, f'exit "{direction}" to {dest} has no exit back to {p}')

        # Check 14: a door is declared on both sides (docs/BUILDING.md, "A room with a door"): the room it leads to must declare a door on an exit that leads back.
        exit_map = dict(hidden_exits_in(src.toks))
        exit_map.update(exits)  # a door may be on a hidden exit (a secret door, docs/specs/search.md section 5.4)
        for direction in doors_in(src.toks):
            dest = exit_map.get(direction)
            if dest is None:
                report(p, 14, ERROR, f'AddDoor("{direction}") but the room has no exit "{direction}" (set Exits first)')
            elif lib.is_file(dest):
                other = lib.source(dest)
                back = dict(hidden_exits_in(other.toks))
                back.update(exits_in(other.toks))
                if not any(back.get(d) == p for d in doors_in(other.toks)):
                    report(p, 14, ERROR, f'door "{direction}" to {dest}: that room declares no door on an exit back to {p}')


        # Check 15: hidden things (docs/specs/search.md section 11), for a room's own HideExit, HideItem, HideNode and HideClue and for the hidden finds it spawns.
        if is_room:
            lint_hidden(lib, p, src, exits, report)

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

        # Check 19: HidesFromWho (docs/specs/who-and-visibility.md section 5) belongs to armour and is `true` or `false`.
        for m in re.finditer(r"\bHidesFromWho\s*(=|=>)\s*([^;]*);", src.text):
            if src.inherit != "/std/armor":
                report(p, 17, ERROR, "HidesFromWho is set on something that does not inherit /std/armor")
            if m.group(2).strip() not in ("true", "false"):
                report(p, 17, ERROR, f"HidesFromWho is {m.group(2).strip()!r}, not true or false")

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

            # Check 16: light (docs/specs/light.md, section 16): Light is an integer from -3 to 1; a dark room (below 0) has a DarkDesc; a wet room
            # is not lit by a wall light in reach (a warning); a room with a shopkeeper is not below 1 (a warning).
            m = re.search(r"\bLight\s*=\s*(-?\d+|LightLit|LightDim|LightDark|LightBlack)\s*;", src.text)
            if m:
                word = m.group(1)
                light = {"LightLit": 1, "LightDim": 0, "LightDark": -1, "LightBlack": -2}.get(word)
                if light is None:
                    light = int(word)
                if light < -3 or light > 1:
                    report(p, 16, ERROR, f"Light {light} is not an integer from -3 to 1")
                if light < 0 and not re.search(r"\bDarkDesc\s*=", src.text):
                    report(p, 16, ERROR, "a dark room (Light below 0) has no DarkDesc")
                spawned = re.findall(r'AddSpawn\(\s*"([^"]+)"', src.text)
                if light < 1 and any(lib.is_file(sp) and "AddStock(" in lib.source(sp).text for sp in spawned):
                    report(p, 16, WARN, "a room with a shopkeeper is below Light 1")
                if re.search(r"\bWet\s*=\s*true", src.text) and any(lib.is_file(sp) and lib.source(sp).inherit in ("/std/wall_light",) for sp in spawned):
                    report(p, 16, WARN, "a wet room has an unquenchable lamp in reach")
            elif re.search(r"\bLight\s*=", src.text):
                report(p, 16, ERROR, "Light is not an integer from -3 to 1 (use a number or LightLit, LightDim, LightDark, LightBlack)")

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

    # Checks 17 and 18: tags, and what names and descriptions say about weapons (they read the registry's text; a mudlib without one skips them).
    lint_tags(lib, everything, report)
    lint_words(lib, everything, report)
    # Check 20: a hidden class is named nowhere a player without it, or a visitor to the site, can read.
    lint_secret(lib, everything, report)

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
