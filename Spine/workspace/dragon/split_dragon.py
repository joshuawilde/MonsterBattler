#!/usr/bin/env python3
"""One-off: dragon-tuned split prompt -> atlas -> segmented parts."""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "spine-animation-ai", "scripts"))
from google import genai
from google.genai import types
from PIL import Image
import split_character as sc

POSITIVE = (
    "Split the creature in this image into its separate body parts, laid out on a plain white "
    "background with space between each part. Keep every part at its original orientation — do not "
    "rotate or re-pose anything — and keep the exact same art style and colors. "
    "Separate it into: one part for each limb (each arm and each leg); one part for the body; two "
    "parts for the head, split at the jaw (upper head and lower jaw); and if it has a tail, split the "
    "tail into a few sections. Keep small details like spikes and scales attached to their part."
)

INPUT  = sys.argv[1] if len(sys.argv) > 1 else "reference_src.jpeg"
ATLAS  = sys.argv[2] if len(sys.argv) > 2 else "atlas_v2.png"
PARTS  = sys.argv[3] if len(sys.argv) > 3 else "parts_v2"
client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])
ref = Image.open(os.path.join(os.path.dirname(__file__), INPUT))
print(f"[1/2] generating dragon-tuned atlas from {INPUT} (gemini-3-pro-image)...")
resp = client.models.generate_content(
    model="gemini-3-pro-image",
    contents=[POSITIVE, ref],
    config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]),
)
atlas_out = os.path.join(os.path.dirname(__file__), ATLAS)
for part in resp.candidates[0].content.parts:
    if part.inline_data is not None:
        open(atlas_out, "wb").write(part.inline_data.data)
        print("  saved", atlas_out)
        break
else:
    print("no image returned"); sys.exit(1)

print("[2/2] segmenting...")
out_dir = os.path.join(os.path.dirname(__file__), PARTS)
parts = sc.segment_parts(atlas_out, out_dir, min_area=500, padding=12, bg_threshold=240)
print(f"  {len(parts)} parts -> {out_dir}")
