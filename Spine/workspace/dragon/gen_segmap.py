#!/usr/bin/env python3
"""Test: ask Nano Banana Pro to paint each body part a FLAT solid color on the
original image (a segmentation map), so we can cut exact parts from the original."""
import os, sys
from google import genai
from google.genai import types
from PIL import Image

HERE = os.path.dirname(__file__)
INPUT = sys.argv[1] if len(sys.argv) > 1 else "reference_src.jpeg"
client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])
ref = Image.open(os.path.join(HERE, INPUT))

PROMPT = (
    "Here is a creature on a plain background. Output the SAME image at the SAME size and SAME pose, "
    "but turn it into a PART SEGMENTATION MAP: repaint each body part as a FLAT, SOLID, UNIFORM color "
    "region — no shading, no gradients, no outlines, no texture — keeping each part's exact silhouette "
    "and position. Use EXACTLY these colors, one per part: "
    "upper head (skull + upper jaw + horns) = pure RED (255,0,0); "
    "lower jaw = ORANGE (255,128,0); "
    "main body/torso/neck = BLUE (0,0,255); "
    "the creature's right arm/foreleg = GREEN (0,255,0); "
    "left arm/foreleg = YELLOW (255,255,0); "
    "right leg/hind leg = MAGENTA (255,0,255); "
    "left leg/hind leg = CYAN (0,255,255); "
    "tail (whole tail including fin) = PURPLE (140,0,255). "
    "Every pixel of the creature must be exactly one of these flat colors, with crisp boundaries "
    "between parts. Background stays pure white. Do not move or resize anything."
)
resp = client.models.generate_content(
    model="gemini-3-pro-image",
    contents=[PROMPT, ref],
    config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]),
)
out = os.path.join(HERE, "segmap.png")
for p in resp.candidates[0].content.parts:
    if p.inline_data:
        open(out, "wb").write(p.inline_data.data); print("saved", out); break
