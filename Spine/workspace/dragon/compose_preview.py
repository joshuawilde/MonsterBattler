#!/usr/bin/env python3
"""Fast offline static compositor: place rig parts at their world coords exactly
as Spine would (setup pose), so alignment can be iterated without browser/Spine."""
import os
from PIL import Image
from build_rig import PART_SRC, BONES, PLACE, DRAW

HERE = os.path.dirname(__file__)
RIG  = os.path.join(HERE, "rig_parts")

# world->image: y-up world to y-down image. Pick origin so dragon is centered.
CANVAS_W, CANVAS_H = 1400, 1000
OX, OY = 700, 720   # world (0,0) maps here

def main(out="compose.png", guides=True):
    canvas = Image.new("RGBA", (CANVAS_W, CANVAS_H), (223, 228, 236, 255))
    for name in DRAW:
        im = Image.open(os.path.join(RIG, name + ".png")).convert("RGBA")
        cx, cy, rot = PLACE[name]
        if rot:
            im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
        ix = int(OX + cx - im.width / 2)
        iy = int(OY - cy - im.height / 2)
        canvas.alpha_composite(im, (ix, iy))
    if guides:
        from PIL import ImageDraw
        d = ImageDraw.Draw(canvas)
        # ground line at world y=0 and vertical at world x=0
        d.line([(0, OY), (CANVAS_W, OY)], fill=(255, 0, 0, 60), width=1)
        d.line([(OX, 0), (OX, CANVAS_H)], fill=(255, 0, 0, 60), width=1)
    canvas.convert("RGB").save(os.path.join(HERE, out))
    print("wrote", out)

if __name__ == "__main__":
    main()
