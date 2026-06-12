#!/usr/bin/env python3
"""
Gemini (Nano Banana Pro) places joint dots ON the ortho renders; we detect the
dots by color and write markers.json for the rig step. Mirrors the Spine
gen_joints.py / extract pattern.

Passes are grouped so each render carries only a few well-separated colors
(reliable extraction). front.png (profile) -> X,Z ; right.png (head-on) -> Y.

Run:  python3 gemini_rig_markers.py
"""
import os, json, sys
import numpy as np
import cv2
from google import genai
from google.genai import types
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
WORKDIR = sys.argv[1] if len(sys.argv) > 1 else HERE
RENDERS = os.path.join(WORKDIR, "renders")
ENV = "/Users/joshuawilde/MonsterBattler/Spine/.env"

# --- api key from Spine/.env ---
key = None
for line in open(ENV):
    if line.startswith("GEMINI_API_KEY"):
        key = line.split("=", 1)[1].strip()
client = genai.Client(api_key=key)
MODEL = "gemini-3-pro-image"

HEX = {"red": "#FF0000", "orange": "#FF8000", "yellow": "#FFFF00",
       "green": "#00FF00", "cyan": "#00FFFF", "blue": "#0000FF", "magenta": "#FF00FF"}

# OpenCV HSV hue ranges (0-180) per color
HUE = {"red": [(0, 10), (170, 180)], "orange": [(10, 22)], "yellow": [(24, 36)],
       "green": [(45, 80)], "cyan": [(85, 100)], "blue": [(105, 135)], "magenta": [(140, 168)]}

# Each pass: which render, output name, and color->(description, expected_count)
PASSES = [
    ("front", "mark_front_axis", {
        "red":    ("the JAW HINGE — the back corner of the mouth where the upper and lower jaw meet", 1),
        "orange": ("the NECK base, where the head connects to the body", 1),
        "green":  ("the SHOULDER, where a front leg meets the body", 1),
        "cyan":   ("the ELBOW of the front leg", 1),
        "blue":   ("the WRIST, where the front foot meets the front leg", 1),
    }),
    ("front", "mark_front_lower", {
        "red":     ("the HIP, where a back leg meets the body", 1),
        "green":   ("the KNEE of the back leg", 1),
        "blue":    ("the ANKLE, where the back foot meets the back leg", 1),
        "orange":  ("the TAIL BASE, where the tail meets the body", 1),
        "yellow":  ("the MIDDLE of the tail", 1),
        "magenta": ("the very TIP of the tail", 1),
    }),
    ("front", "mark_front_head", {
        "red":  ("the tip of the UPPER JAW / nose (the snout tip)", 1),
        "blue": ("the tip of the LOWER JAW (the chin)", 1),
    }),
    ("right", "mark_right_arms", {
        "green": ("each SHOULDER where a front leg meets the body — TWO dots, one per side", 2),
        "cyan":  ("each ELBOW of the front legs — TWO dots, one per side", 2),
        "blue":  ("each WRIST of the front legs — TWO dots, one per side", 2),
    }),
    ("right", "mark_right_legs", {
        "red":     ("each HIP where a back leg meets the body — TWO dots, one per side", 2),
        "yellow":  ("each KNEE of the back legs — TWO dots, one per side", 2),
        "magenta": ("each ANKLE/foot of the back legs — TWO dots, one per side", 2),
    }),
]


def build_prompt(view, dots):
    lines = [f"This image shows a creature seen from the {view}. Draw small SOLID FILLED "
             "circles about 18 pixels wide marking these exact anatomical points, placing each "
             "dot precisely on the described point. Use these EXACT bright colors:"]
    for c, (desc, n) in dots.items():
        lines.append(f"  {c.upper()} ({HEX[c]}) = {desc}.")
    lines.append("Draw ONLY these solid colored dots on top of the existing artwork. Keep the rest "
                 "of the image completely identical — do not recolor, crop, resize, or redraw anything else.")
    return "\n".join(lines)


def gen(view, out, dots):
    img = Image.open(os.path.join(RENDERS, view + ".png"))
    resp = client.models.generate_content(
        model=MODEL, contents=[build_prompt(view, dots), img],
        config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]))
    for p in resp.candidates[0].content.parts:
        if p.inline_data:
            path = os.path.join(RENDERS, out + ".png")
            open(path, "wb").write(p.inline_data.data)
            return path
    return None


def detect(path, color, expected):
    bgr = cv2.imread(path)
    H, W = bgr.shape[:2]
    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    mask = np.zeros((H, W), np.uint8)
    for lo, hi in HUE[color]:
        mask |= cv2.inRange(hsv, (lo, 110, 110), (hi, 255, 255))
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    n, lab, stats, cent = cv2.connectedComponentsWithStats(mask, 8)
    blobs = [(cent[i][0] / W, cent[i][1] / H, stats[i, cv2.CC_STAT_AREA])
             for i in range(1, n) if stats[i, cv2.CC_STAT_AREA] >= 20]
    blobs.sort(key=lambda b: -b[2])          # biggest first
    blobs = blobs[:expected]
    blobs.sort(key=lambda b: b[0])            # then left->right by x
    return [(round(x, 4), round(y, 4)) for x, y, _ in blobs]


# ---- run passes, collect raw detections ----
raw = {}
for view, out, dots in PASSES:
    path = gen(view, out, dots)
    print(f"[gemini] {out}: {'ok' if path else 'NO IMAGE'}")
    if not path:
        continue
    for color, (desc, n) in dots.items():
        pts = detect(path, color, n)
        raw[(out, color)] = pts
        flag = "" if len(pts) == n else f"  <-- expected {n}, got {len(pts)}"
        print(f"    {color:8s} {pts}{flag}")

# ---- fallbacks (my hand placement) if a detection is missing ----
FB = {  # joint: (front[u,v], side_uL, side_uR)
    "snoutJ": ((0.95, 0.30), 0.5, 0.5), "headJ": ((0.74, 0.30), 0.5, 0.5),
    "jawJ": ((0.90, 0.40), 0.5, 0.5), "neckJ": ((0.69, 0.33), 0.5, 0.5),
    "hips": ((0.46, 0.46), 0.5, 0.5), "tail1": ((0.40, 0.45), 0.5, 0.5),
    "tail2": ((0.26, 0.44), 0.5, 0.5), "tail3": ((0.08, 0.55), 0.5, 0.5),
    "shoulder": ((0.61, 0.47), 0.34, 0.66), "elbow": ((0.63, 0.57), 0.32, 0.68),
    "hand": ((0.65, 0.68), 0.31, 0.69), "thigh": ((0.45, 0.47), 0.30, 0.70),
    "knee": ((0.47, 0.59), 0.27, 0.73), "foot": ((0.49, 0.72), 0.27, 0.73),
}


def front_of(out, color, joint):
    pts = raw.get((out, color))
    return pts[0] if pts and len(pts) >= 1 else FB[joint][0]


def sides_of(out, color, joint):
    pts = raw.get((out, color))
    if pts and len(pts) == 2:
        return pts[0][0], pts[1][0]          # left x, right x  (already sorted)
    return FB[joint][1], FB[joint][2]


def lerp(a, b, t):
    return [round(a[0] + (b[0] - a[0]) * t, 4), round(a[1] + (b[1] - a[1]) * t, 4)]


# ---- assemble final joints ----
neck = front_of("mark_front_axis", "orange", "neckJ")
hip = front_of("mark_front_lower", "red", "thigh")
shL, shR = sides_of("mark_right_arms", "green", "shoulder")
elL, elR = sides_of("mark_right_arms", "cyan", "elbow")
haL, haR = sides_of("mark_right_arms", "blue", "hand")
hpL, hpR = sides_of("mark_right_legs", "red", "thigh")
knL, knR = sides_of("mark_right_legs", "yellow", "knee")
ftL, ftR = sides_of("mark_right_legs", "magenta", "foot")

J = {
    "snoutJ": {"front": list(front_of("mark_front_head", "red", "snoutJ")), "side_u": 0.5},
    "headJ":  {"front": list(front_of("mark_front_axis", "red", "headJ")), "side_u": 0.5},
    "jawJ":   {"front": list(front_of("mark_front_head", "blue", "jawJ")), "side_u": 0.5},
    "neckJ":  {"front": list(neck), "side_u": 0.5},
    "chestJ": {"front": lerp(neck, hip, 0.30), "side_u": 0.5},
    "spineJ": {"front": lerp(neck, hip, 0.62), "side_u": 0.5},
    "hips":   {"front": list(hip), "side_u": 0.5},
    "tail1":  {"front": list(front_of("mark_front_lower", "orange", "tail1")), "side_u": 0.5},
    "tail2":  {"front": list(front_of("mark_front_lower", "yellow", "tail2")), "side_u": 0.5},
    "tail3":  {"front": list(front_of("mark_front_lower", "magenta", "tail3")), "side_u": 0.5},

    "shoulderL": {"front": list(front_of("mark_front_axis", "green", "shoulder")), "side_u": shL},
    "elbowL":    {"front": list(front_of("mark_front_axis", "cyan", "elbow")), "side_u": elL},
    "handL":     {"front": list(front_of("mark_front_axis", "blue", "hand")), "side_u": haL},
    "shoulderR": {"front": list(front_of("mark_front_axis", "green", "shoulder")), "side_u": shR},
    "elbowR":    {"front": list(front_of("mark_front_axis", "cyan", "elbow")), "side_u": elR},
    "handR":     {"front": list(front_of("mark_front_axis", "blue", "hand")), "side_u": haR},

    "thighL": {"front": list(hip), "side_u": hpL},
    "kneeL":  {"front": list(front_of("mark_front_lower", "green", "knee")), "side_u": knL},
    "footL":  {"front": list(front_of("mark_front_lower", "blue", "foot")), "side_u": ftL},
    "thighR": {"front": list(hip), "side_u": hpR},
    "kneeR":  {"front": list(front_of("mark_front_lower", "green", "knee")), "side_u": knR},
    "footR":  {"front": list(front_of("mark_front_lower", "blue", "foot")), "side_u": ftR},
}

json.dump({"joints": J}, open(os.path.join(WORKDIR, "markers.json"), "w"), indent=2)
print("\n[done] wrote markers.json with", len(J), "joints")
