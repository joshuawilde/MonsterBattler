#!/usr/bin/env python3
"""Auto-bone demo: for each part, compute the medial-axis centerline and place
bone joints along it. This is the 'add points along the tail' step, automated."""
import os, glob
import numpy as np
import cv2
from collections import deque
from skimage.morphology import skeletonize

HERE = os.path.dirname(__file__)

def neighbors(p, skel):
    y, x = p
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dy or dx:
                ny, nx = y+dy, x+dx
                if 0 <= ny < skel.shape[0] and 0 <= nx < skel.shape[1] and skel[ny, nx]:
                    yield (ny, nx)

def longest_path(skel):
    pts = np.argwhere(skel)
    if len(pts) == 0:
        return []
    start = tuple(pts[0])
    def bfs(s):
        seen = {s: None}; q = deque([s]); last = s
        while q:
            c = q.popleft(); last = c
            for n in neighbors(c, skel):
                if n not in seen:
                    seen[n] = c; q.append(n)
        return last, seen
    a, _ = bfs(start)
    b, seen = bfs(a)
    path = []; c = b
    while c is not None:
        path.append(c); c = seen[c]
    return path  # ordered (y,x) from one tip to the other

def resample(path, n):
    if len(path) < 2:
        return path
    pts = np.array(path, float)
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
    if d[-1] == 0:
        return [path[0]]
    xs = np.linspace(0, d[-1], n)
    out = []
    for x in xs:
        i = np.searchsorted(d, x)
        i = min(max(i, 1), len(pts)-1)
        t = (x - d[i-1]) / max(d[i]-d[i-1], 1e-6)
        out.append(tuple(pts[i-1]*(1-t) + pts[i]*t))
    return out

def process(path_png):
    raw = cv2.imread(path_png, cv2.IMREAD_UNCHANGED)
    a = raw[:, :, 3]
    mask = (a > 60)
    skel = skeletonize(mask)
    cl = longest_path(skel)
    # bone count from centerline length: ~1 bone per 90px, clamp 1..5
    length = 0
    if len(cl) > 1:
        p = np.array(cl, float); length = np.sum(np.linalg.norm(np.diff(p, axis=0), axis=1))
    nb = int(np.clip(round(length/90)+1, 1, 5))
    bones = resample(cl, nb+1) if cl else []
    # visualize
    rgb = (raw[:, :, :3] * (a[..., None]/255.0) + 245*(1-a[..., None]/255.0)).astype(np.uint8)
    for (y, x) in cl:
        rgb[int(y), int(x)] = (0, 180, 0)
    for j, (y, x) in enumerate(bones):
        cv2.circle(rgb, (int(x), int(y)), 6, (0, 0, 255), -1)
        cv2.circle(rgb, (int(x), int(y)), 6, (255, 255, 255), 1)
    return rgb, nb, length

def main():
    files = sorted(glob.glob(os.path.join(HERE, "parts_v8", "part_*.png")))
    cell = 240; cols = 5; rows = (len(files)+cols-1)//cols
    sheet = np.full((rows*cell, cols*cell, 3), 250, np.uint8)
    for i, f in enumerate(files):
        vis, nb, length = process(f)
        h, w = vis.shape[:2]; s = min((cell-30)/w, (cell-30)/h)
        vis = cv2.resize(vis, (int(w*s), int(h*s)))
        r, c = divmod(i, cols)
        sheet[r*cell+5:r*cell+5+vis.shape[0], c*cell+10:c*cell+10+vis.shape[1]] = vis
        cv2.putText(sheet, f"{os.path.basename(f)[5:7]}: {nb}b L{int(length)}",
                    (c*cell+10, r*cell+cell-8), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 200), 1)
        print(f"{os.path.basename(f)}: centerline_len={int(length)} -> {nb} bones")
    cv2.imwrite(os.path.join(HERE, "skeleton_demo.png"), sheet)
    print("wrote skeleton_demo.png")

if __name__ == "__main__":
    main()
