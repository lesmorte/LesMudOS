#!/usr/bin/env python3
"""Build clients/mudlet/Hollowmere.mpackage from the Lua in clients/mudlet/src.

An .mpackage is a zip file that Mudlet installs (Toolbox, Package Manager, Install): config.lua (the package's name and
description) and the package XML, here a script group with one script per Lua file, in the order they must load. The build is
reproducible: the same sources give the same bytes.

    python3 clients/mudlet/build.py            write Hollowmere.mpackage
    python3 clients/mudlet/build.py --check    fail if the committed package is not what the sources build
"""
import os
import sys
import zipfile
from xml.sax.saxutils import escape

HERE = os.path.dirname(os.path.abspath(__file__))
PACKAGE = "Hollowmere"
VERSION = "1.2.0"
# In the order they load: the map needs the layout.
SCRIPTS = [("layout", "layout.lua"), ("map", "map.lua"), ("gauges", "gauges.lua"), ("quests", "quests.lua")]
CONFIG = f'''mpackage = "{PACKAGE}"
author = "LesMudOS"
title = "Hollowmere map, gauges and quests"
description = [[Builds the Mudlet map of Legends of Hollowmere from the game's GMCP Room.Info and draws Vitality, Endurance and Focus gauges from Char.Vitals, and adds a Quests panel (Active, Completed, Abandoned) from Char.Quests. Open the mapper (Toggle Map) to see it.]]
version = "{VERSION}"
created = "2026-11-22T00:00:00Z"
'''


def xml():
    parts = ['<?xml version="1.0" encoding="UTF-8"?>\n<!DOCTYPE MudletPackage>\n<MudletPackage version="1.001">\n    <ScriptPackage>\n',
             f'        <ScriptGroup isActive="yes" isFolder="yes">\n            <name>{PACKAGE}</name>\n            <packageName>{PACKAGE}</packageName>\n'
             '            <script></script>\n            <eventHandlerList />\n']
    for name, filename in SCRIPTS:
        with open(os.path.join(HERE, "src", filename), encoding="utf-8") as f:
            source = f.read()
        parts.append(f'            <Script isActive="yes" isFolder="no">\n                <name>{name}</name>\n                <packageName></packageName>\n'
                     f'                <script>{escape(source)}</script>\n                <eventHandlerList />\n            </Script>\n')
    parts.append('        </ScriptGroup>\n    </ScriptPackage>\n</MudletPackage>\n')
    return "".join(parts)


def build(path):
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        for name, data in ((f"{PACKAGE}.xml", xml()), ("config.lua", CONFIG)):
            info = zipfile.ZipInfo(name, date_time=(2026, 11, 22, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            z.writestr(info, data.encode("utf-8"))


if __name__ == "__main__":
    target = os.path.join(HERE, f"{PACKAGE}.mpackage")
    if "--check" in sys.argv:
        built = target + ".check"
        build(built)
        same = open(built, "rb").read() == open(target, "rb").read()
        os.remove(built)
        print("up to date" if same else "OUT OF DATE: run python3 clients/mudlet/build.py")
        sys.exit(0 if same else 1)
    build(target)
    print("wrote", target)
