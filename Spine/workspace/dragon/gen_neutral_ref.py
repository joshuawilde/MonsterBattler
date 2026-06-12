#!/usr/bin/env python3
"""Generate an ASSEMBLED neutral side-profile reference of the dragon, matching
the rest-pose orientation of the split parts, to use as a SIFT positioning target."""
import os, sys
from google import genai
from google.genai import types
from PIL import Image

HERE = os.path.dirname(__file__)

POSITIVE = (
    "A single full-body 2D game character of the EXACT dragon/kaiju creature in the reference image, "
    "redrawn in a clean NEUTRAL SIDE-PROFILE standing rest pose (like a character-sheet T-pose for a "
    "quadruped): body horizontal and facing left, head forward, neck extended straight, all four legs "
    "straight and visible and slightly spread, tail extended out behind in a gentle relaxed curve, the "
    "tail fin and dorsal back-spikes clearly shown. Limbs uncrossed, no foreshortening, no dynamic action. "
    "Keep the EXACT same art style, bold black outlines, cel shading, and color palette as the reference. "
    "The whole creature assembled as one piece, centered, on a clean solid white background. "
    "Full body fully inside frame with margin."
)
NEGATIVE = (
    "deconstructed, separated parts, sprite sheet, multiple poses, dynamic action pose, rearing up, "
    "open roaring mouth, foreshortening, crossed limbs, 3D, realistic, different style, different colors, "
    "redesign, background scenery, shadows, text, watermark, cropped, cut off."
)

client = genai.Client(api_key=os.environ["GEMINI_IMAGE_MODEL_KEY"] if "GEMINI_IMAGE_MODEL_KEY" in os.environ else os.environ["GEMINI_API_KEY"])
ref = Image.open(os.path.join(HERE, "reference_src.jpeg"))
print("generating neutral assembled reference (gemini-3-pro-image)...")
resp = client.models.generate_content(
    model="gemini-3-pro-image",
    contents=[POSITIVE, f"Negative prompt: {NEGATIVE}", ref],
    config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]),
)
out = os.path.join(HERE, "neutral_ref.png")
for part in resp.candidates[0].content.parts:
    if part.inline_data is not None:
        open(out, "wb").write(part.inline_data.data)
        print("saved", out); break
else:
    print("no image returned"); sys.exit(1)
