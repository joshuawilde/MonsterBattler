#!/usr/bin/env python3
"""Stack per-part Gemini-placed canvases (white->transparent) to check if they
align into a coherent dragon. Also report each part's alpha bbox/centroid."""
import os, glob, sys
import cv2, numpy as np

HERE = os.path.dirname(__file__)
order = ["tail-fin", "torso", "neck", "hindleg-near", "head", "foreleg-near"]  # back->front
ref = cv2.imread(os.path.join(HERE, "neutral_ref.png"))
H, W = ref.shape[:2]
canvas = np.full((H, W, 3), 255, np.uint8)
print(f"ref {W}x{H}")
for name in order:
    p = os.path.join(HERE, f"place_{name}.png")
    if not os.path.exists(p): print("  missing", name); continue
    im = cv2.imread(p)
    im = cv2.resize(im, (W, H)) if im.shape[:2] != (H, W) else im
    fg = (np.abs(im.astype(int) - 255).sum(2) > 45)
    ys, xs = np.where(fg)
    if len(xs): print(f"  {name:<14} bbox=({xs.min()},{ys.min()})-({xs.max()},{ys.max()}) center=({(xs.min()+xs.max())//2},{(ys.min()+ys.max())//2})")
    canvas[fg] = im[fg]
cv2.imwrite(os.path.join(HERE, "stack.png"), np.hstack([ref, canvas]))
print("wrote stack.png (ref | stacked)")
