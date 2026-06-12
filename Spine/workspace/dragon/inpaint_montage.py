#!/usr/bin/env python3
"""Before/after montage of inpainting for all parts."""
import os, cv2, numpy as np
HERE = os.path.dirname(__file__)
parts = ["head","jaw","body","arm-R","arm-L","leg-R","leg-L","tail"]
cell = 230
rows = []
for p in parts:
    a = cv2.imread(os.path.join(HERE, f"inpaint_in_{p}.png"))
    b = cv2.imread(os.path.join(HERE, f"inpaint_out_{p}.png"))
    if a is None or b is None:
        continue
    def fit(im):
        s = min((cell-20)/im.shape[1], (cell-20)/im.shape[0])
        im = cv2.resize(im, (int(im.shape[1]*s), int(im.shape[0]*s)))
        c = np.full((cell, cell, 3), 250, np.uint8)
        y, x = (cell-im.shape[0])//2, (cell-im.shape[1])//2
        c[y:y+im.shape[0], x:x+im.shape[1]] = im
        return c
    pair = np.hstack([fit(a), fit(b)])
    cv2.putText(pair, f"{p}: in -> out", (8, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (0,0,200), 2)
    rows.append(pair)
# 2 columns of pairs
out = []
for i in range(0, len(rows), 2):
    r = rows[i:i+2]
    if len(r) == 1: r.append(np.full_like(r[0], 250))
    out.append(np.hstack([r[0], np.full((cell,12,3),255,np.uint8), r[1]]))
cv2.imwrite(os.path.join(HERE, "inpaint_all.png"), np.vstack(out))
print("wrote inpaint_all.png")
