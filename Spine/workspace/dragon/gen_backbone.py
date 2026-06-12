#!/usr/bin/env python3
"""Ask Gemini to draw the BACKBONE spline (snout->tail tip) so we sample a clean
bone chain from it instead of noisy computed centerlines."""
import os
from google import genai
from google.genai import types
from PIL import Image
HERE=os.path.dirname(__file__)
client=genai.Client(api_key=os.environ["GEMINI_API_KEY"])
ref=Image.open(os.path.join(HERE,"reference_src.jpeg"))
PROMPT=(
 "On this dragon, draw an animation skeleton as colored lines (constant ~7px width, smooth), keeping "
 "the dragon artwork itself completely unchanged. Three lines that all MEET at ONE point at the base "
 "of the head (where the neck meets the back of the skull / the jaw hinge): "
 "(1) BRIGHT ORANGE (#FF7A00) = the BACKBONE: from that head-base point, go down the NECK, along the "
 "centerline of the back/body to the hips, then down the CENTER of the entire tail to the very tip. "
 "Do NOT extend this orange line into the head. "
 "(2) BRIGHT YELLOW (#FFFF00) = the UPPER HEAD: from the head-base point forward through the middle of "
 "the upper skull to the tip of the snout. "
 "(3) BRIGHT MAGENTA (#FF00FF) = the LOWER JAW: from the head-base point forward along the middle of "
 "the lower jaw to the chin tip. "
 "Draw ONLY these three lines, all starting from the same head-base point. No dots or arrows, do not "
 "recolor the dragon."
)
resp=client.models.generate_content(model="gemini-3-pro-image",contents=[PROMPT,ref],
     config=types.GenerateContentConfig(response_modalities=["IMAGE","TEXT"]))
for p in resp.candidates[0].content.parts:
    if p.inline_data: open(os.path.join(HERE,"backbone.png"),"wb").write(p.inline_data.data); print("saved backbone.png"); break
