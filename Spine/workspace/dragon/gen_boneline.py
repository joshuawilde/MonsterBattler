#!/usr/bin/env python3
"""Test: ask Nano Banana Pro to PAINT the bone centerline (orange) onto a part,
then we extract the orange line as the bone chain. sys.argv[1] = part file stem."""
import os, sys
from google import genai
from google.genai import types
from PIL import Image

HERE = os.path.dirname(__file__)
client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])
part = Image.open(os.path.join(HERE, "parts_v8", sys.argv[1] + ".png"))

PROMPT = (
    "This image shows ONE isolated body part of a cartoon creature on a white background. "
    "Output the SAME image at the SAME size, completely unchanged, EXCEPT draw exactly ONE single "
    "continuous BRIGHT ORANGE line (color #FF6A00) of CONSTANT WIDTH — a uniform 6 pixels thick along "
    "its ENTIRE length, never tapering, thickening, or fading. The line runs down the central long "
    "axis of the part (like the bone inside it), staying in the MIDDLE of the shape from one end to "
    "the other, and follows any bend smoothly (a leg's knee/ankle, a curving tail). "
    "Draw ONLY this one line — NO branches, NO second line, NO dots or circles, NO arrowheads, no "
    "other marks. Do not change, recolor, crop, resize, or redraw anything else in the image."
)
resp = client.models.generate_content(
    model="gemini-3-pro-image",
    contents=[PROMPT, part],
    config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]),
)
out = os.path.join(HERE, f"boneline_{sys.argv[1]}.png")
for p in resp.candidates[0].content.parts:
    if p.inline_data:
        open(out, "wb").write(p.inline_data.data); print("saved", out); break
