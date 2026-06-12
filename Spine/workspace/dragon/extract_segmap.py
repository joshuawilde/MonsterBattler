#!/usr/bin/env python3
"""Cut real part art from the original image using Gemini's flat-color segmap.
Nearest-color labeling -> per-part mask -> apply to original pixels."""
import os
import numpy as np
import cv2

HERE = os.path.dirname(__file__)
COLORS = {
    "head":      (255, 0, 0),
    "jaw":       (255, 128, 0),
    "body":      (0, 0, 255),
    "arm-R":     (0, 255, 0),
    "arm-L":     (255, 255, 0),
    "leg-R":     (255, 0, 255),
    "leg-L":     (0, 255, 255),
    "tail":      (140, 0, 255),
}
BG = (255, 255, 255)

seg = cv2.cvtColor(cv2.imread(os.path.join(HERE, "segmap.png")), cv2.COLOR_BGR2RGB)
orig = cv2.imread(os.path.join(HERE, "reference_src.jpeg"))           # BGR
orig = cv2.resize(orig, (seg.shape[1], seg.shape[0]))                  # align to segmap
H, W = seg.shape[:2]

# nearest-color label for every pixel
names = list(COLORS) + ["_bg"]
palette = np.array([COLORS[n] for n in COLORS] + [BG], float)          # (9,3)
flat = seg.reshape(-1, 3).astype(float)
dist = np.linalg.norm(flat[:, None, :] - palette[None, :, :], axis=2)  # (N,9)
label = dist.argmin(1).reshape(H, W)

os.makedirs(os.path.join(HERE, "parts_seg"), exist_ok=True)
canvas = np.full((H, W, 3), 255, np.uint8)
sheet_cells = []
for idx, name in enumerate(COLORS):
    m = (label == idx).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    if m.sum() < 200:
        print("  (empty)", name); continue
    bgra = np.dstack([orig, (m * 255).astype(np.uint8)])
    ys, xs = np.where(m)
    crop = bgra[ys.min():ys.max()+1, xs.min():xs.max()+1]
    cv2.imwrite(os.path.join(HERE, "parts_seg", f"{name}.png"), crop)
    canvas[m > 0] = orig[m > 0]
    print(f"  {name}: {int(m.sum())} px  bbox=({xs.min()},{ys.min()})-({xs.max()},{ys.max()})")

cv2.imwrite(os.path.join(HERE, "segmap_recon.png"),
            np.hstack([orig, np.full((H, 16, 3), 245, np.uint8), canvas]))
print("wrote segmap_recon.png (original | reconstructed-from-parts)")
