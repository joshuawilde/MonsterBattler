#!/usr/bin/env python3
"""Assemble the dragon rig: rename parts, place bones/attachments, author
dragon-specific animations, emit config.json for build_spine_json.py."""
import os, sys, json, shutil
from PIL import Image

HERE = os.path.dirname(__file__)
SRC  = os.path.join(HERE, "parts_v2")
RIG  = os.path.join(HERE, "rig_parts")

# semantic name -> source part file
PART_SRC = {
    "head":        "part_03",
    "jaw":         "part_05",
    "neck":        "part_02",
    "torso":       "part_06",
    "back-spikes": "part_00",
    "foreleg-near":"part_07",
    "foreleg-far": "part_10",
    "hindleg-near":"part_13",
    "hindleg-far": "part_15",
    "tail-base":   "part_08",
    "tail-tip":    "part_16",
    "tail-fin":    "part_04",
    "tail-spikes": "part_01",
}

# bone -> (world_x, world_y, parent). Spine: y-up, x-right. Dragon faces LEFT.
BONES = {
    "root":         (   0,   0, None),
    "hip":          ( 130, 250, "root"),
    "torso":        ( -20, 270, "hip"),
    "neck":         (-180, 320, "torso"),
    "head":         (-310, 345, "neck"),
    "jaw":          (-295, 315, "head"),
    "back-spikes":  ( -20, 270, "torso"),
    "foreleg-near": ( -70, 265, "torso"),
    "foreleg-far":  ( -40, 270, "torso"),
    "hindleg-near": ( 150, 245, "hip"),
    "hindleg-far":  ( 175, 250, "hip"),
    "tail-base":    ( 200, 255, "hip"),
    "tail-tip":     ( 300, 300, "tail-base"),
    "tail-fin":     ( 400, 370, "tail-tip"),
    "tail-spikes":  ( 230, 270, "tail-base"),
}

# slot/attachment -> (center_world_x, center_world_y, rotation_deg)
PLACE = {
    "head":        (-330, 350,   0),
    "jaw":         (-330, 300,   0),
    "neck":        (-200, 315, -15),
    "torso":       (   0, 250,   0),
    "back-spikes": ( -25, 415, -90),
    "foreleg-near":( -85, 140,   0),
    "foreleg-far": ( -50, 150,   0),
    "hindleg-near":( 160, 120,   0),
    "hindleg-far": ( 185, 125,   0),
    "tail-base":   ( 240, 280,  25),
    "tail-tip":    ( 400, 360,  35),
    "tail-fin":    ( 540, 470,  20),
    "tail-spikes": ( 300, 360,  30),
}

# draw order back -> front
DRAW = ["hindleg-far","foreleg-far","tail-fin","tail-spikes","tail-tip","tail-base",
        "back-spikes","neck","torso","hindleg-near","jaw","head","foreleg-near"]

# far parts slightly darkened for depth
DARK = {"hindleg-far":"b8b8b8ff", "foreleg-far":"c0c0c0ff"}

def main():
    if os.path.isdir(RIG): shutil.rmtree(RIG)
    os.makedirs(RIG)
    sizes = {}
    for name, src in PART_SRC.items():
        im = Image.open(os.path.join(SRC, src + ".png")).convert("RGBA")
        im.save(os.path.join(RIG, name + ".png"))
        sizes[name] = im.size

    bones_out = [{"name": "root"}]
    for name, (wx, wy, parent) in BONES.items():
        if name == "root": continue
        pwx, pwy, _ = BONES[parent]
        bones_out.append({"name": name, "parent": parent,
                          "x": round(wx - pwx, 1), "y": round(wy - pwy, 1)})

    slots_out, attachments = [], {}
    for name in DRAW:
        slot = {"name": name, "bone": name, "attachment": name}
        if name in DARK: slot["color"] = DARK[name]
        slots_out.append(slot)
        w, h = sizes[name]
        cx, cy, rot = PLACE[name]
        bwx, bwy, _ = BONES[name]
        attachments[name] = {"width": w, "height": h,
                             "x": round(cx - bwx, 1), "y": round(cy - bwy, 1),
                             "rotation": rot}

    config = {
        "skeleton": {"name": "dragon", "width": 1100, "height": 900},
        "bones": bones_out,
        "slots": slots_out,
        "attachments": attachments,
        "animations": [],            # skip humanoid presets
        "custom_animations": build_anims(),
    }
    out = os.path.join(HERE, "dragon_config.json")
    json.dump(config, open(out, "w"), indent=2)
    print("wrote", out, "| bones:", len(bones_out), "slots:", len(slots_out))


# ---- dragon animations (Spine 4.2 'angle' format) ----
EASE = [0.25, 0, 0.75, 1]
def kf(t, a=None, x=None, y=None, curve=EASE):
    d = {"time": round(t, 3)}
    if a is not None: d["angle"] = a
    if x is not None: d["x"] = x
    if y is not None: d["y"] = y
    if curve: d["curve"] = curve
    return d

def build_anims():
    D = 2.4  # idle loop
    idle = {"bones": {
        "torso": {"translate": [kf(0,x=0,y=0,curve=None), kf(D/2,x=0,y=6), kf(D,x=0,y=0)]},
        "neck":  {"rotate": [kf(0,0,curve=None), kf(D/2,4), kf(D,0)]},
        "head":  {"rotate": [kf(0,0,curve=None), kf(D/2,-3), kf(D,0)]},
        "jaw":   {"rotate": [kf(0,0,curve=None), kf(D/2,-4), kf(D,0)]},
        "tail-base": {"rotate": [kf(0,0,curve=None), kf(D*0.33,6), kf(D*0.66,-6), kf(D,0)]},
        "tail-tip":  {"rotate": [kf(0,0,curve=None), kf(D*0.33,9), kf(D*0.66,-9), kf(D,0)]},
        "tail-fin":  {"rotate": [kf(0,0,curve=None), kf(D*0.33,7), kf(D*0.66,-7), kf(D,0)]},
        "foreleg-near": {"rotate": [kf(0,0,curve=None), kf(D/2,2), kf(D,0)]},
        "back-spikes":  {"rotate": [kf(0,0,curve=None), kf(D/2,1), kf(D,0)]},
    }}
    A = 0.7  # attack: lunge + jaw chomp
    attack = {"bones": {
        "hip":   {"translate": [kf(0,x=0,y=0,curve=None), kf(0.12,x=18,y=4,curve=[0.42,0,1,1]),
                                 kf(0.28,x=-40,y=0,curve=[0.4,0,0.2,1]), kf(A,x=0,y=0,curve=[0,0,0.58,1])]},
        "torso": {"rotate": [kf(0,0,curve=None), kf(0.12,-8), kf(0.28,10,curve=[0.4,0,0.2,1]), kf(0.45,5), kf(A,0)]},
        "neck":  {"rotate": [kf(0,0,curve=None), kf(0.12,10), kf(0.28,-14,curve=[0.4,0,0.2,1]), kf(A,0)]},
        "head":  {"rotate": [kf(0,0,curve=None), kf(0.12,8), kf(0.28,-12,curve=[0.4,0,0.2,1]), kf(A,0)]},
        "jaw":   {"rotate": [kf(0,0,curve=None), kf(0.1,-35,curve=[0.42,0,1,1]),
                              kf(0.26,8,curve=[0.4,0,0.2,1]), kf(0.4,-2), kf(A,0)]},
        "tail-base": {"rotate": [kf(0,0,curve=None), kf(0.12,-12), kf(0.3,8), kf(A,0)]},
        "tail-tip":  {"rotate": [kf(0,0,curve=None), kf(0.12,-16), kf(0.3,12), kf(A,0)]},
    }}
    return {"idle": idle, "attack": attack}

if __name__ == "__main__":
    main()
