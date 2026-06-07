#!/usr/bin/env python3
"""Maps each randbats species to a distinct fakemon creature (by primary type) from the itch pack and
copies ONLY the chosen front/back PNGs into Assets/StreamingAssets/mons/ (named by species id), so
Unity never imports the 111k-file pack. Deterministic (stable across re-runs). Writes _manifest.json."""
import json, os, re, shutil, glob

PACK = "/Users/joshuawilde/Downloads/itch_assets"
DEX = "MonsterBattler/Assets/StreamingAssets/dex"
OUT = "MonsterBattler/Assets/StreamingAssets/mons"

species = json.load(open(f"{DEX}/species.json"))
randbats = json.load(open(f"{DEX}/randbats.json"))

# Build available creatures per pack type: prefer fully-evolved (stage_2) then earlier stages.
type_creatures = {}
for d in sorted(os.listdir(PACK)):
    m = re.match(r"(.+)_fakemon_line_(\d+)$", d)
    if not m:
        continue
    t = m.group(1)
    for stage in ("stage_2", "stage_1", "base"):
        sd = os.path.join(PACK, d, stage)
        if os.path.exists(os.path.join(sd, "front_nobg.png")) and os.path.exists(os.path.join(sd, "back_nobg.png")):
            type_creatures.setdefault(t, []).append((d, stage))
for t in type_creatures:
    type_creatures[t].sort()
print("pack types:", {t: len(v) for t, v in sorted(type_creatures.items())})

# Fairy isn't in the pack — fall back to a thematically-close type.
FALLBACK = {"fairy": "psychic"}

# Group our species by mapped primary type.
by_type = {}
for sid in randbats:
    types = species.get(sid, {}).get("types", ["Normal"])
    pt = (types[0] if types else "Normal").lower()
    pt = pt if pt in type_creatures else FALLBACK.get(pt, "normal")
    by_type.setdefault(pt, []).append(sid)

# Fresh output dir.
if os.path.isdir(OUT):
    for f in glob.glob(f"{OUT}/*.png"):
        os.remove(f)
os.makedirs(OUT, exist_ok=True)

manifest = {}
copied = 0
for t, sids in by_type.items():
    sids.sort()
    creatures = type_creatures.get(t) or type_creatures["normal"]
    for i, sid in enumerate(sids):
        folder, stage = creatures[i % len(creatures)]
        src = os.path.join(PACK, folder, stage)
        shutil.copy(os.path.join(src, "front_nobg.png"), f"{OUT}/{sid}_front.png")
        shutil.copy(os.path.join(src, "back_nobg.png"), f"{OUT}/{sid}_back.png")
        manifest[sid] = {"type": t, "line": folder, "stage": stage}
        copied += 1

json.dump(manifest, open(f"{OUT}/_manifest.json", "w"), indent=0)
print(f"mapped {copied} species -> {copied*2} sprites in {OUT}")
