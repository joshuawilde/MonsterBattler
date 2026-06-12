#!/usr/bin/env python3
"""Constrained inpaint: WE define the fill region (geometric close of the part
silhouette), paint it flag-green, and ask Gemini to texture ONLY the green with
plain scales. No free-form completion -> can't hallucinate neighbour parts."""
import os, sys
import numpy as np
import cv2
from google import genai
from google.genai import types
from PIL import Image

HERE = os.path.dirname(__file__)
stem = sys.argv[1] if len(sys.argv) > 1 else "body"
thing = {"body": "torso/body", "head": "upper head/skull"}.get(stem, stem)

raw = cv2.imread(os.path.join(HERE, "parts_seg", f"{stem}.png"), cv2.IMREAD_UNCHANGED)
bgr, a = raw[:, :, :3], raw[:, :, 3]
M = (a > 60).astype(np.uint8)
# target silhouette: convex hull of the part bridges even wide limb-bites,
# then pull it in slightly so we don't balloon far past the true body.
pts = np.column_stack(np.where(M.T > 0))            # (x,y)
hull = cv2.convexHull(pts)
T = np.zeros_like(M); cv2.fillConvexPoly(T, hull, 1)
T = cv2.erode(T, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (25, 25)))
T = (T | M).astype(np.uint8)                        # never smaller than the real part
band = ((T == 1) & (M == 0)).astype(np.uint8)       # region to fill (green)

canvas = np.full_like(bgr, 255)
canvas[M == 1] = bgr[M == 1]
canvas[band == 1] = (0, 255, 0)                              # flag green (BGR)
cv2.imwrite(os.path.join(HERE, f"inpaint2_in_{stem}.png"), canvas)
flat = Image.fromarray(cv2.cvtColor(canvas, cv2.COLOR_BGR2RGB))

PROMPT = (
    f"This image is a SINGLE cutout layer — the {thing} of a dragon — used as one separate piece in "
    f"Spine skeletal-animation software. The bright GREEN areas are empty cutout regions that need to "
    f"be filled in. "
    f"Fill the green areas using ONLY the {thing} material that already exists in THIS image — extend "
    f"its existing scales, skin and color to cover the green smoothly. "
    f"Because this is a separate cutout layer, do NOT add or draw ANY other body part that is not "
    f"already in this image (no arms, legs, claws, feet, head, jaw, eye, tail, or fin) — only continue "
    f"the {thing} surface that is already here. "
    f"Keep every non-green pixel exactly the same. Output on a white background."
)
client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])
resp = client.models.generate_content(
    model="gemini-3-pro-image",
    contents=[PROMPT, flat],
    config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]),
)
out = os.path.join(HERE, f"inpaint2_out_{stem}.png")
for part in resp.candidates[0].content.parts:
    if part.inline_data:
        open(out, "wb").write(part.inline_data.data); print("saved", out); break
