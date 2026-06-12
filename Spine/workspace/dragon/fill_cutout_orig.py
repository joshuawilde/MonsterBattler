#!/usr/bin/env python3
"""User's approach: give Gemini the SEGMENTED CUTOUT + the ORIGINAL image, ask
it to output the complete body part with occluded areas filled in."""
import os
from google import genai
from google.genai import types
from PIL import Image

HERE = os.path.dirname(__file__)
client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])

orig = Image.open(os.path.join(HERE, "reference_src.jpeg")).convert("RGB")
body = Image.open(os.path.join(HERE, "parts_seg", "body.png")).convert("RGBA")
bg = Image.new("RGBA", body.size, (255, 255, 255, 255))
cutout = Image.alpha_composite(bg, body).convert("RGB")   # body on white, gaps = white
cutout.save(os.path.join(HERE, "cutout_in_body.png"))

PROMPT = (
    "IMAGE 1 is a full cartoon dragon. IMAGE 2 is the dragon's BODY/TORSO cut out of it — parts of it "
    "are missing (white gaps) where the arms, legs, head and tail were overlapping in front of it. "
    "Using IMAGE 1 to understand the body's shape, redraw IMAGE 2 as the COMPLETE body/torso: fill the "
    "white gaps with the body scales/skin that would be BEHIND those parts, so the torso is one solid "
    "continuous piece. Output ONLY the torso — do NOT include any arm, leg, claw, foot, head, jaw, eye, "
    "tail, or fin (those are separate layers). Keep the existing body pixels and the exact art style, "
    "outlines and colors. White background, same framing as IMAGE 2."
)
resp = client.models.generate_content(
    model="gemini-3-pro-image",
    contents=[PROMPT, orig, cutout],
    config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]),
)
for p in resp.candidates[0].content.parts:
    if p.inline_data:
        open(os.path.join(HERE, "cutout_out_body.png"), "wb").write(p.inline_data.data); break
import cv2, numpy as np
a = cv2.imread(os.path.join(HERE,"cutout_in_body.png")); b = cv2.imread(os.path.join(HERE,"cutout_out_body.png"))
h=max(a.shape[0],b.shape[0]); pad=lambda i:(lambda s:cv2.resize(i,(int(i.shape[1]*s),h)))(h/i.shape[0])
cv2.imwrite(os.path.join(HERE,"cutout_compare.png"), np.hstack([pad(a),np.full((h,12,3),255,np.uint8),pad(b)]))
print("wrote cutout_compare.png")
