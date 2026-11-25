"""Shared bits for the second review's proofs: reuses tests/redteam/rtlib.py (a private driver, chief 10, pat 0, wren 1)."""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(os.path.dirname(HERE), "redteam"))
os.environ.setdefault("LESMUDOS_TEST_PORT", "5081")
import rtlib  # noqa: E402,F401

check = rtlib.check
finish = rtlib.finish
Game = rtlib.Game


def item(cls, verbs, extra=""):
    """A sandbox item binding verbs; verbs is {verb: body-of-a-block-lambda}."""
    binds = "\n".join(f'        AddAction("{v}", a => {{ {b} }});' for v, b in verbs.items())
    return f'''// @inherit /std/item
public class {cls} : Item
{{
    public override void Create() {{ Name = "{cls.lower()}"; Ids.Add("{cls.lower()}"); ShortDesc = "a {cls.lower()}"; }}
    public override void Init()
    {{
        base.Init();
{binds}
    }}
{extra}
}}
'''
