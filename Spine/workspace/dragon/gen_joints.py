#!/usr/bin/env python3
"""Ask Gemini to draw colored dots on the skeletal joints, so we can extract
exact bone pivots (anatomically correct) instead of heuristic guesses."""
import os
from google import genai
from google.genai import types
from PIL import Image

HERE=os.path.dirname(__file__)
client=genai.Client(api_key=os.environ["GEMINI_API_KEY"])
ref=Image.open(os.path.join(HERE,"reference_src.jpeg"))

PROMPT=(
 "On this dragon, draw small SOLID FILLED circles (about 16 pixels wide) marking the skeletal "
 "ANIMATION JOINTS, and keep everything else in the image exactly the same. Use these EXACT colors, "
 "placing each dot precisely on the joint: "
 "RED = the JAW HINGE (the back corner of the mouth where upper and lower jaw meet); "
 "ORANGE = the NECK base (where the head connects to the body); "
 "GREEN = each SHOULDER (where each front leg/arm meets the body) — TWO green dots, one per arm; "
 "BLUE = each HIP (where each back leg meets the body) — TWO blue dots, one per leg; "
 "PURPLE = the TAIL base (where the tail meets the body). "
 "Draw ONLY these solid colored dots on top of the existing artwork. Do not draw anything else, do "
 "not recolor or redraw the dragon."
)
resp=client.models.generate_content(model="gemini-3-pro-image",contents=[PROMPT,ref],
     config=types.GenerateContentConfig(response_modalities=["IMAGE","TEXT"]))
for p in resp.candidates[0].content.parts:
    if p.inline_data: open(os.path.join(HERE,"joints.png"),"wb").write(p.inline_data.data); print("saved joints.png"); break
