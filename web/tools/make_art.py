#!/usr/bin/env python3
"""Generates the site's two illustrations as SVG: the hero (a forest road from a village to a walled city) and the map of Greenvale.
Original artwork, drawn in code. Usage: python3 make_art.py OUTDIR  (writes hero.svg and map.svg)."""
import sys, math, random, os

INK = "#33231a"; OAK = "#3b2a1a"; PARCH = "#f0dfb5"; PARCH2 = "#e6d09a"; CREAM = "#f8efd6"
GREEN_D = "#2f4a2b"; GREEN_M = "#4e6b3a"; GREEN_L = "#8aa06a"; HILL1 = "#b4b88c"; HILL2 = "#8fa377"; HILL3 = "#6a8758"
COPPER = "#a4572a"; COPPER_L = "#d98a43"; ROOF = "#8a4318"; STONE = "#d8c08c"; STONE_D = "#b99a63"; ROAD = "#e8d3a0"; WATER = "#a9bfb6"

def hero():
    r = random.Random(7)
    o = []
    a = o.append
    a('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 960 400" role="img" aria-labelledby="hero-t hero-d" class="art" preserveAspectRatio="xMidYMid slice">')
    a('<title id="hero-t">A forest road from a village to a walled city</title>')
    a('<desc id="hero-d">An engraved view in warm colours: a road runs from a small village among pines, across fields and a river, toward the walled city of Oakenfell under a wide morning sky, with two travelers on the road.</desc>')
    a('<defs>')
    a(f'<symbol id="pine" viewBox="0 0 40 80"><path d="M20 0 L33 26 H26 L38 50 H28 L40 72 H0 L12 50 H2 L14 26 H7Z" fill="{GREEN_D}"/><path d="M20 6 L20 70 M12 36 L20 30 M28 36 L20 30 M8 58 L20 52 M32 58 L20 52" stroke="{GREEN_M}" stroke-width="1.5" fill="none"/><rect x="18" y="72" width="4" height="8" fill="{OAK}"/></symbol>')
    a(f'<symbol id="oak" viewBox="0 0 60 70"><circle cx="30" cy="24" r="22" fill="{GREEN_M}"/><circle cx="16" cy="32" r="14" fill="{GREEN_M}"/><circle cx="44" cy="32" r="14" fill="{GREEN_M}"/><path d="M14 20 Q30 8 46 20 M10 32 Q30 22 50 32" stroke="{GREEN_D}" stroke-width="1.6" fill="none"/><rect x="27" y="40" width="6" height="30" fill="{OAK}"/></symbol>')
    a(f'<symbol id="home" viewBox="0 0 60 50"><rect x="6" y="22" width="48" height="26" fill="{STONE}" stroke="{INK}" stroke-width="1.5"/><path d="M0 24 L30 2 L60 24Z" fill="{ROOF}" stroke="{INK}" stroke-width="1.5"/><rect x="40" y="6" width="6" height="12" fill="{STONE_D}" stroke="{INK}" stroke-width="1.2"/><rect x="14" y="30" width="9" height="9" fill="{INK}"/><rect x="34" y="32" width="10" height="16" fill="{OAK}"/><path d="M6 36 H54 M6 30 H54" stroke="{STONE_D}" stroke-width="1"/></symbol>')
    a('</defs>')
    # sky
    a(f'<rect width="960" height="400" fill="{PARCH}"/>')
    sky = "".join(f"M0 {y}H960" for y in (14, 30, 44, 56, 66, 75, 83, 90, 96, 102, 107, 112))
    a(f'<path d="{sky}" stroke="{PARCH2}" stroke-width="1.4" fill="none"/>')
    # sun
    a(f'<g transform="translate(600 232)"><circle r="52" fill="{COPPER_L}"/><circle r="64" fill="none" stroke="{COPPER}" stroke-width="1.5" stroke-dasharray="3 5"/>')
    rays = "".join(f"M{math.cos(t)*78:.0f} {math.sin(t)*78:.0f}L{math.cos(t)*112:.0f} {math.sin(t)*112:.0f}" for t in [math.pi + i * math.pi / 14 for i in range(15)])
    a(f'<path d="{rays}" stroke="{COPPER}" stroke-width="2" fill="none"/></g>')
    # clouds
    def cloud(x, y, s):
        a(f'<g transform="translate({x} {y}) scale({s})" fill="{CREAM}" stroke="{STONE_D}" stroke-width="1.5"><path d="M0 24 Q4 8 22 12 Q30 -4 52 8 Q72 0 80 18 Q100 16 98 28 Q96 34 80 34 H12 Q-2 34 0 24Z"/><path d="M14 28 H84 M24 21 H70" fill="none"/></g>')
    cloud(70, 50, 1.2); cloud(330, 30, 0.9); cloud(790, 58, 1.1); cloud(470, 92, 0.7)
    # far hills
    a(f'<path d="M0 238 Q90 196 190 226 T380 222 T560 240 T760 214 T960 232 V300 H0Z" fill="{HILL1}" stroke="{STONE_D}" stroke-width="1.2"/>')
    a(f'<path d="M0 262 Q120 224 250 256 T470 250 T700 262 T960 246 V320 H0Z" fill="{HILL2}"/>')
    hatch = "".join(f"M{x} {y}l6 10" for x in range(10, 950, 24) for y in (r.randint(262, 286),))
    a(f'<path d="{hatch}" stroke="{HILL3}" stroke-width="1.4" fill="none"/>')
    # city rise + river
    a(f'<path d="M640 266 Q720 238 800 236 T960 244 V300 H640Z" fill="{HILL3}"/>')
    a(f'<path d="M0 300 Q200 280 420 290 T700 290 T960 276 V296 Q820 302 700 306 T420 306 T0 322Z" fill="{WATER}" stroke="{STONE_D}" stroke-width="1"/>')
    a(f'<path d="M60 304 h40 M210 296 h50 M360 300 h36 M540 301 h54 M760 294 h48" stroke="{CREAM}" stroke-width="1.6" fill="none"/>')
    # city
    a(f'<g stroke="{INK}" stroke-width="1.5">')
    a(f'<rect x="742" y="222" width="190" height="34" fill="{STONE}"/>')
    cren = "".join(f"M{x} 222v-6h6v6" for x in range(742, 930, 12))
    a(f'<path d="{cren}" fill="{STONE}"/>')
    # roofs behind wall
    for x, h in ((752, 20), (782, 26), (860, 24), (894, 18)):
        a(f'<path d="M{x} 222 L{x+14} {222-h} L{x+28} 222Z" fill="{ROOF}"/>')
    # the hall
    a(f'<rect x="806" y="186" width="62" height="36" fill="{STONE}"/><path d="M796 188 L837 150 L878 188Z" fill="{ROOF}"/><path d="M806 200 H868" stroke="{STONE_D}"/>')
    a(f'<rect x="830" y="196" width="14" height="26" fill="{INK}"/>')
    a(f'<path d="M837 150 V132" /><path d="M837 132 l14 5 l-14 5Z" fill="{COPPER}"/>')
    # towers
    for x in (736, 922):
        a(f'<rect x="{x}" y="196" width="18" height="62" fill="{STONE}"/><path d="M{x-3} 196 L{x+9} 172 L{x+21} 196Z" fill="{ROOF}"/><rect x="{x+6}" y="206" width="6" height="10" fill="{INK}"/>')
    a(f'<path d="M762 256 v-18 a10 10 0 0 1 20 0 v18Z" fill="{INK}"/>')
    a('</g>')
    # fields
    fields = [(-20, 330, 130, 316, 230, 322, 70, 345, "#b5b97a"), (140, 322, 300, 312, 420, 318, 250, 336, "#c9b878"), (430, 318, 600, 306, 700, 304, 590, 326, "#a6b570"), (610, 322, 740, 296, 830, 290, 720, 320, "#cfc083"), (210, 340, 340, 334, 440, 344, 200, 366, "#9fae6a")]
    for x1, y1, x2, y2, x3, y3, x4, y4, c in fields:
        a(f'<path d="M{x1} {y1} L{x2} {y2} L{x3} {y3} L{x4} {y4}Z" fill="{c}" stroke="{STONE_D}" stroke-width="1"/>')
    a(f'<path d="M0 346 H140 M0 358 H190 M150 330 L280 322 M470 330 L590 316 M640 316 L760 300" stroke="{STONE_D}" stroke-width="1" fill="none"/>')
    # foreground meadow
    a(f'<path d="M0 340 Q160 320 300 350 T560 372 T960 330 V400 H0Z" fill="{GREEN_L}"/>')
    tuft = "".join(f"M{x} {y}l-3 -7m3 7l0 -9m0 9l3 -7" for x in range(18, 950, 38) for y in (r.randint(366, 392),))
    a(f'<path d="{tuft}" stroke="{GREEN_D}" stroke-width="1.4" fill="none"/>')
    # road: from the foreground to the city gate
    a(f'<path d="M352 400 C400 360 520 352 600 330 S740 288 770 258 L778 258 C770 292 690 322 650 340 S560 372 560 400Z" fill="{ROAD}" stroke="{INK}" stroke-width="1.6"/>')
    a(f'<path d="M430 396 C470 372 560 362 620 340 M530 392 C560 378 600 366 640 352" stroke="{STONE_D}" stroke-width="1.5" fill="none" stroke-dasharray="14 12"/>')
    # travelers
    def walker(x, y, s):
        a(f'<g transform="translate({x} {y}) scale({s})" stroke="{INK}" stroke-width="1.6"><path d="M0 0 L-3 -16 L3 -16Z" fill="{GREEN_M}"/><circle cy="-20" r="4" fill="{STONE}"/><path d="M-5 -24 L0 -32 L5 -24Z" fill="{COPPER}"/><path d="M7 2 V-26" fill="none"/><rect x="-9" y="-16" width="6" height="9" fill="{ROOF}"/></g>')
    walker(470, 378, 1.5); walker(498, 368, 1.2)
    # village (left)
    a(f'<path d="M0 338 Q100 320 200 338 Q300 352 340 400 H0Z" fill="{GREEN_M}"/>')
    a(f'<path d="M0 346 Q100 330 210 350" stroke="{INK}" stroke-width="2" fill="none" stroke-dasharray="2 6"/>')
    for x, y, s in ((18, 306, 1.0), (84, 300, 1.2), (150, 312, 0.9)):
        a(f'<use href="#home" x="{x}" y="{y}" width="{60*s:.0f}" height="{50*s:.0f}"/>')
    a(f'<path d="M110 292 q6 -12 -2 -22 q10 -8 2 -22" stroke="{STONE_D}" stroke-width="3" fill="none" stroke-linecap="round"/>')
    a(f'<path d="M2 342 v-18 M16 340 v-18 M30 338 v-18 M44 340 v-18" stroke="{OAK}" stroke-width="3" fill="none"/>')
    # forest, both sides
    for x, y, w in ((200, 276, 30), (232, 284, 26), (262, 292, 24), (300, 298, 22), (330, 306, 20)):
        a(f'<use href="#pine" x="{x}" y="{y}" width="{w}" height="{w*2}"/>')
    for x, y, w in ((670, 290, 22), (700, 296, 20), (440, 296, 20), (900, 270, 26), (930, 280, 22)):
        a(f'<use href="#oak" x="{x}" y="{y-10}" width="{w*1.3:.0f}" height="{w*1.5:.0f}"/>')
    for x, w in ((-10, 120), (96, 96), (610, 110), (700, 140), (826, 110), (890, 130)):
        pass
    for x, y, w in ((228, 204, 74), (290, 236, 62), (-20, 290, 0)):
        pass
    big = [(240, 214, 90), (310, 250, 70), (350, 284, 56), (590, 270, 70), (628, 300, 50)]
    for x, y, w in big:
        a(f'<use href="#pine" x="{x}" y="{y}" width="{w}" height="{w*2}"/>')
    # foreground trees framing
    for x, y, w in ((-24, 148, 110), (70, 196, 90), (836, 168, 120), (920, 220, 90), (736, 224, 78)):
        pass
    a(f'<use href="#pine" x="-30" y="176" width="124" height="248"/>')
    a(f'<use href="#pine" x="860" y="150" width="140" height="280"/>')
    a(f'<use href="#pine" x="800" y="262" width="80" height="160"/>')
    a(f'<use href="#oak" x="700" y="352" width="64" height="74"/>')
    # birds
    a(f'<path d="M270 130 q8 -9 16 0 q8 -9 16 0 M330 108 q6 -7 12 0 q6 -7 12 0 M120 120 q5 -6 10 0 q5 -6 10 0" stroke="{INK}" stroke-width="1.6" fill="none"/>')
    # frame
    a(f'<rect x="5" y="5" width="950" height="390" fill="none" stroke="{OAK}" stroke-width="3"/><rect x="11" y="11" width="938" height="378" fill="none" stroke="{OAK}" stroke-width="1"/>')
    a('</svg>')
    return "\n".join(o)

def world_map():
    r = random.Random(11)
    o = []; a = o.append
    W, H = 640, 820
    a(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" role="img" aria-labelledby="map-t map-d" class="art map">')
    a('<title id="map-t">Map of Greenvale</title>')
    a('<desc id="map-d">An old-style map. In the south lies the village of Vincent\'s Hollow. A road runs north through the Eldergreen Wood to Oakenfell, a walled city on the River Mere. Forest and hills continue beyond the edges of the map and fade into unknown country.</desc>')
    a('<defs>')
    a(f'<linearGradient id="fadeN" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{PARCH}" stop-opacity="1"/><stop offset="1" stop-color="{PARCH}" stop-opacity="0"/></linearGradient>')
    a(f'<linearGradient id="fadeS" x1="0" y1="1" x2="0" y2="0"><stop offset="0" stop-color="{PARCH}" stop-opacity="1"/><stop offset="1" stop-color="{PARCH}" stop-opacity="0"/></linearGradient>')
    a(f'<linearGradient id="fadeW" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="{PARCH}" stop-opacity="1"/><stop offset="1" stop-color="{PARCH}" stop-opacity="0"/></linearGradient>')
    a(f'<linearGradient id="fadeE" x1="1" y1="0" x2="0" y2="0"><stop offset="0" stop-color="{PARCH}" stop-opacity="1"/><stop offset="1" stop-color="{PARCH}" stop-opacity="0"/></linearGradient>')
    a(f'<symbol id="mt" viewBox="0 0 20 28"><path d="M10 0 L18 14 H13 L20 24 H0 L7 14 H2Z" fill="{GREEN_M}" stroke="{GREEN_D}" stroke-width="1"/><path d="M10 24 V28" stroke="{OAK}" stroke-width="2"/></symbol>')
    a(f'<symbol id="mh" viewBox="0 0 40 20"><path d="M0 20 Q10 0 20 2 Q30 0 40 20Z" fill="{HILL1}" stroke="{STONE_D}" stroke-width="1.2"/><path d="M10 16 L14 8 M18 18 L22 6 M26 16 L30 9" stroke="{STONE_D}" stroke-width="1" fill="none"/></symbol>')
    a(f'<symbol id="mb" viewBox="0 0 30 26"><rect x="3" y="12" width="24" height="12" fill="{STONE}" stroke="{INK}" stroke-width="1.3"/><path d="M0 13 L15 1 L30 13Z" fill="{ROOF}" stroke="{INK}" stroke-width="1.3"/></symbol>')
    a('</defs>')
    a(f'<rect width="{W}" height="{H}" fill="{PARCH}"/>')
    # land wash and ruled grid
    a(f'<path d="M-20 90 Q140 40 330 70 T660 60 V740 Q500 780 320 750 T-20 770Z" fill="{PARCH2}" opacity=".55"/>')
    grid = "".join(f"M{x} 0V{H}" for x in range(80, W, 80)) + "".join(f"M0 {y}H{W}" for y in range(80, H, 80))
    a(f'<path d="{grid}" stroke="{STONE_D}" stroke-width=".7" fill="none" stroke-dasharray="2 6" opacity=".7"/>')
    # the River Mere, passing Oakenfell on its west side then off to the north-east and south-west
    a(f'<path d="M640 20 C560 60 470 90 400 120 S330 190 300 230 S210 290 150 330 S40 400 -10 410" fill="none" stroke="{WATER}" stroke-width="16" stroke-linecap="round"/>')
    a(f'<path d="M640 20 C560 60 470 90 400 120 S330 190 300 230 S210 290 150 330 S40 400 -10 410" fill="none" stroke="{CREAM}" stroke-width="2" stroke-dasharray="10 14"/>')
    # hills on both flanks
    for (x, y) in ((40, 150), (96, 190), (24, 250), (84, 300), (30, 360), (120, 520), (48, 580), (470, 190), (540, 240), (590, 150), (500, 330), (566, 380), (520, 470), (590, 540), (60, 470)):
        a(f'<use href="#mh" x="{x}" y="{y}" width="52" height="26"/>')
    # eldergreen wood along the road: trees scattered, thick near the road
    def road_x(y):  # x of the road at height y
        return 300 + 70 * math.sin((y - 220) / 78.0) + (y - 220) * 0.04
    for y in range(238, 640, 16):
        for side in (-1, 1):
            for k in range(3):
                x = road_x(y) + side * (36 + k * 26 + r.randint(0, 14))
                a(f'<use href="#mt" x="{x:.0f}" y="{y + r.randint(-5, 5)}" width="17" height="24"/>')
    # sparse forest beyond (unlabeled): thinning toward the edges
    for _ in range(90):
        x = r.randint(10, W - 30); y = r.choice([r.randint(30, 200), r.randint(660, 800)])
        a(f'<use href="#mt" x="{x}" y="{y}" width="15" height="21" opacity=".8"/>')
    # the road
    pts = [(road_x(y), y) for y in range(640, 196, -10)]
    d = "M" + " L".join(f"{x:.0f} {y}" for x, y in pts) + f" L{round(road_x(200))} 196"
    a(f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="5" stroke-linecap="round"/>')
    a(f'<path d="{d}" fill="none" stroke="{ROAD}" stroke-width="2.6" stroke-linecap="round" stroke-dasharray="1 7"/>')
    # the road beyond Oakenfell fades north, and a track leaves south of the Hollow
    a(f'<path d="M{round(road_x(200))} 100 C{round(road_x(200))} 70 320 50 322 20" fill="none" stroke="{INK}" stroke-width="3" stroke-dasharray="2 9" stroke-linecap="round"/>')
    a(f'<path d="M{road_x(640):.0f} 640 C300 690 280 730 290 800" fill="none" stroke="{INK}" stroke-width="3" stroke-dasharray="2 9" stroke-linecap="round"/>')
    # Vincent's Hollow
    hx, hy = road_x(650), 650
    a(f'<g stroke="{OAK}" stroke-width="2.5"><path d="M{hx-52:.0f} {hy-6} v-10 M{hx-40:.0f} {hy-14} v-10 M{hx-28:.0f} {hy-20} v-10 M{hx+30:.0f} {hy-20} v-10 M{hx+42:.0f} {hy-14} v-10 M{hx+54:.0f} {hy-6} v-10" fill="none"/></g>')
    for dx, dy, s in ((-44, -12, 28), (-12, -30, 32), (20, -18, 28), (-26, 6, 26), (14, 8, 30)):
        a(f'<use href="#mb" x="{hx+dx:.0f}" y="{hy+dy}" width="{s}" height="{s*26/30:.0f}"/>')
    a(f'<text x="{hx:.0f}" y="{hy+60}" text-anchor="middle" class="m-town">Vincent\'s Hollow</text>')
    a(f'<text x="{hx:.0f}" y="{hy+84}" text-anchor="middle" class="m-note">a village among the pines</text>')
    # Oakenfell
    ox, oy = round(road_x(200)), 170
    a(f'<g stroke="{INK}" stroke-width="1.8">')
    a(f'<path d="M{ox-62} {oy+26} V{oy-6} L{ox-34} {oy-26} H{ox+34} L{ox+62} {oy-6} V{oy+26}Z" fill="{STONE}"/>')
    a(f'<path d="{"".join(f"M{x} {oy-26}v-5h5v5" for x in range(ox-34, ox+34, 10))}" fill="none"/>')
    a(f'<rect x="{ox-14}" y="{oy-30}" width="28" height="30" fill="{STONE}"/><path d="M{ox-20} {oy-30} L{ox} {oy-54} L{ox+20} {oy-30}Z" fill="{ROOF}"/><path d="M{ox} {oy-54} v-14 l12 4 l-12 4" fill="{COPPER}"/>')
    for x in (ox - 62, ox + 56):
        a(f'<rect x="{x}" y="{oy-20}" width="12" height="46" fill="{STONE}"/><path d="M{x-2} {oy-20} L{x+6} {oy-36} L{x+14} {oy-20}Z" fill="{ROOF}"/>')
    a(f'<path d="M{ox-8} {oy+26} v-12 a8 8 0 0 1 16 0 v12Z" fill="{INK}"/>')
    a('</g>')
    a(f'<text x="{ox+4}" y="{oy+62}" text-anchor="middle" class="m-city">Oakenfell</text>')
    a(f'<text x="{ox+4}" y="{oy+88}" text-anchor="middle" class="m-note">home of the Hall of Legends</text>')
    # a bridge over the river west of the city
    a(f'<path d="M292 238 q18 -16 40 -2" fill="none" stroke="{INK}" stroke-width="3.2"/><path d="M292 238 l-3 5 M332 236 l3 5" stroke="{INK}" stroke-width="2.4"/>')
    a(f'<text x="120" y="330" class="m-water" transform="rotate(-27 120 330)">River Mere</text>')
    # the wood's name along the road, and the road's
    a(f'<text x="{road_x(420)+78:.0f}" y="392" class="m-wood">The Eldergreen</text><text x="{road_x(420)+78:.0f}" y="420" class="m-wood">Wood</text>')
    a(f'<text x="{road_x(520)-100:.0f}" y="540" text-anchor="end" class="m-note">The North Road</text>')
    a(f'<text x="{road_x(520)-100:.0f}" y="566" text-anchor="end" class="m-note">two days on foot</text>')
    # compass
    a(f'<g transform="translate(560 740)" stroke="{OAK}" stroke-width="1.6"><circle r="26" fill="none"/><path d="M0 -40 L6 -6 L0 0 L-6 -6Z M0 40 L6 6 L0 0 L-6 6Z M-40 0 L-6 -6 L0 0 L-6 6Z M40 0 L6 -6 L0 0 L6 6Z" fill="{PARCH2}"/><path d="M0 -40 L6 -6 L0 0Z" fill="{COPPER}"/><text y="-48" text-anchor="middle" class="m-note" stroke="none">N</text></g>')
    # edges fade into the unknown
    a(f'<rect width="{W}" height="130" fill="url(#fadeN)"/><rect y="{H-130}" width="{W}" height="130" fill="url(#fadeS)"/><rect width="110" height="{H}" fill="url(#fadeW)"/><rect x="{W-110}" width="110" height="{H}" fill="url(#fadeE)"/>')
    # cartouche
    a(f'<g transform="translate(24 24)"><rect width="250" height="82" fill="{CREAM}" stroke="{OAK}" stroke-width="2.5"/><rect x="6" y="6" width="238" height="70" fill="none" stroke="{OAK}" stroke-width="1"/><text x="125" y="41" text-anchor="middle" class="m-title">Greenvale</text><text x="125" y="66" text-anchor="middle" class="m-note">in the land of Hollowmere</text></g>')
    a(f'<text x="{W/2+60}" y="30" text-anchor="middle" class="m-unknown">the road goes on</text>')
    a(f'<text x="{W/2}" y="{H-14}" text-anchor="middle" class="m-unknown">here the map ends; the world does not</text>')
    a(f'<rect x="3" y="3" width="{W-6}" height="{H-6}" fill="none" stroke="{OAK}" stroke-width="2" opacity=".55"/>')
    a('</svg>')
    return "\n".join(o)

if __name__ == "__main__":
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    open(os.path.join(out, "hero.svg"), "w").write(hero())
    open(os.path.join(out, "map.svg"), "w").write(world_map())
