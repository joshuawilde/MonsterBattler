#!/usr/bin/env python3
"""Test: can Gemini place a single complete part onto a blank canvas at the exact
spot it belongs in the reference? If yes, alpha/bbox gives us its position for free."""
import os, sys
from google import genai
from google.genai import types
from PIL import Image

HERE = os.path.dirname(__file__)
client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])
ref  = Image.open(os.path.join(HERE, "neutral_ref.png"))
part = Image.open(os.path.join(HERE, "rig_parts", sys.argv[1] + ".png"))

PROMPT = (
    "IMAGE 1 is a full reference drawing of a dragon. IMAGE 2 is ONE isolated body part "
    f"of that same dragon (its {sys.argv[1].replace('-', ' ')}). "
    "Produce an output image with the SAME dimensions as IMAGE 1, on a pure solid white "
    "background, containing ONLY this one part — copied as-is — positioned, scaled, and "
    "rotated to EXACTLY match where that part sits in the reference drawing. Do NOT draw any "
    "other body part. Do NOT redraw or restyle the part; keep it identical. Everything except "
    "that single part must be plain white."
)
resp = client.models.generate_content(
    model="gemini-3-pro-image",
    contents=[PROMPT, ref, part],
    config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]),
)
out = os.path.join(HERE, f"place_{sys.argv[1]}.png")
for p in resp.candidates[0].content.parts:
    if p.inline_data:
        open(out, "wb").write(p.inline_data.data); print("saved", out); break
else:
    print("no image"); sys.exit(1)
