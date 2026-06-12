#!/usr/bin/env python3
"""Inpaint a cut part into a complete piece WITHOUT redrawing neighbours.
Per-part identity + explicit 'do not draw X' + original image as shape context."""
import os, sys
from google import genai
from google.genai import types
from PIL import Image

HERE = os.path.dirname(__file__)
client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])

# part -> (what it IS, what must NOT be drawn)
DESC = {
    "head":  ("upper head and skull (with horns, eye, and the upper jaw + upper teeth)",
              "the lower jaw, tongue, open-mouth/throat interior, neck, or body"),
    "jaw":   ("lower jaw (with lower teeth and tongue)",
              "the upper head, skull, horns, eye, neck, or body"),
    "body":  ("torso/body/neck (with belly and back scales)",
              "ANY arm, leg, claw, hand, foot, head, jaw, or tail"),
    "arm-R": ("dragon's right foreleg/arm (with clawed hand)",
              "the body/torso, the other limbs, the head, or the tail"),
    "arm-L": ("dragon's left foreleg/arm (with clawed hand)",
              "the body/torso, the other limbs, the head, or the tail"),
    "leg-R": ("dragon's right hind leg (with clawed foot)",
              "the body/torso, the other limbs, the head, or the tail"),
    "leg-L": ("dragon's left hind leg (with clawed foot)",
              "the body/torso, the other limbs, the head, or the tail"),
    "tail":  ("tail (with fin and tail-spikes)",
              "the body/torso, any leg, or the head"),
}

stem = sys.argv[1]
thing, avoid = DESC[stem]

p = Image.open(os.path.join(HERE, "parts_seg", f"{stem}.png")).convert("RGBA")
bg = Image.new("RGBA", p.size, (255, 255, 255, 255))
flat = Image.alpha_composite(bg, p).convert("RGB")
flat.save(os.path.join(HERE, f"inpaint_in_{stem}.png"))
orig = Image.open(os.path.join(HERE, "reference_src.jpeg"))

PROMPT = (
    f"IMAGE 1 is the full original dragon (reference for shapes/colors). IMAGE 2 is just the {thing}, "
    f"cut out of that dragon. Some areas of IMAGE 2 are MISSING (white gaps/notches) where OTHER body "
    f"parts were overlapping in front of it. "
    f"Redraw IMAGE 2 as ONE complete {thing}: fill the white gaps ONLY with more of the {thing}'s own "
    f"scales/skin/color, extending its natural silhouette smoothly and plausibly. "
    f"CRITICAL: do NOT draw {avoid} — those are SEPARATE pieces and must NOT appear anywhere in the "
    f"output, even partially. Where another part used to overlap, just continue the {thing} behind it. "
    f"Keep every existing (non-white) pixel and the exact art style, outlines and colors identical. "
    f"Output ONLY the finished {thing} on a plain white background, same size as IMAGE 2."
)
resp = client.models.generate_content(
    model="gemini-3-pro-image",
    contents=[PROMPT, orig, flat],
    config=types.GenerateContentConfig(response_modalities=["IMAGE", "TEXT"]),
)
out = os.path.join(HERE, f"inpaint_out_{stem}.png")
for part in resp.candidates[0].content.parts:
    if part.inline_data:
        open(out, "wb").write(part.inline_data.data); print("saved", out); break
