#!/usr/bin/env python3
"""Improved auto-bone: medial axis -> longest path -> SMOOTHING SPLINE -> bones.
Cleaner centerline that flows down the middle instead of wobbling into claws."""
import os, sys
import numpy as np
import cv2
from collections import deque
from skimage.morphology import skeletonize
from scipy.interpolate import splprep, splev

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
    def bfs(s):
        seen = {s: None}; q = deque([s]); last = s
        while q:
            c = q.popleft(); last = c
            for n in neighbors(c, skel):
                if n not in seen:
                    seen[n] = c; q.append(n)
        return last, seen
    a, _ = bfs(tuple(pts[0]))
    b, seen = bfs(a)
    path = []; c = b
    while c is not None:
        path.append(c); c = seen[c]
    return path

def smooth(path, dist):
    """Fit a smoothing spline; trim ends that dive into a claw/protrusion by
    pulling the endpoints in toward where the shape is still thick."""
    pts = np.array(path, float)
    if len(pts) < 6:
        return pts
    # trim 6% off each end (claw/tip spurs) before fitting
    k = max(1, int(len(pts)*0.06)); pts = pts[k:-k]
    # distance-transform value along path = local half-thickness; weight the fit
    w = np.clip(dist[pts[:,0].astype(int), pts[:,1].astype(int)], 1, None)
    try:
        tck, u = splprep([pts[:,1], pts[:,0]], w=w, s=len(pts)*8.0, k=3)
    except Exception:
        return pts
    uu = np.linspace(0, 1, 240)
    x, y = splev(uu, tck)
    return np.stack([y, x], 1)

def resample(cl, n):
    p = np.array(cl, float)
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(p, axis=0), axis=1))]
    xs = np.linspace(0, d[-1], n)
    out = []
    for x in xs:
        i = min(max(np.searchsorted(d, x), 1), len(p)-1)
        t = (x-d[i-1])/max(d[i]-d[i-1], 1e-6)
        out.append(p[i-1]*(1-t)+p[i]*t)
    return out

# role-based bone counts
def bone_count(length, name):
    return int(np.clip(round(length/170)+1, 1, 4))

def process(path_png, scale=1.0):
    raw = cv2.imread(path_png, cv2.IMREAD_UNCHANGED)
    a = raw[:, :, 3]
    mask = (a > 60)
    skel = skeletonize(mask)
    dist = cv2.distanceTransform(mask.astype(np.uint8), cv2.DIST_L2, 5)
    cl_raw = longest_path(skel)
    cl = smooth(cl_raw, dist) if cl_raw else []
    length = np.sum(np.linalg.norm(np.diff(np.array(cl), axis=0), axis=1)) if len(cl) > 1 else 0
    nb = bone_count(length, path_png)
    bones = resample(cl, nb+1) if len(cl) > 1 else []
    rgb = (raw[:, :, :3]*(a[..., None]/255.0) + 245*(1-a[..., None]/255.0)).astype(np.uint8)
    # smooth centerline as polyline
    for i in range(len(cl)-1):
        cv2.line(rgb, (int(cl[i][1]), int(cl[i][0])), (int(cl[i+1][1]), int(cl[i+1][0])), (0,170,0), 3)
    for j in range(len(bones)-1):
        cv2.line(rgb, (int(bones[j][1]), int(bones[j][0])), (int(bones[j+1][1]), int(bones[j+1][0])), (255,120,0), 2)
    for (y, x) in bones:
        cv2.circle(rgb, (int(x), int(y)), 9, (0,0,255), -1)
        cv2.circle(rgb, (int(x), int(y)), 9, (255,255,255), 2)
    if scale != 1.0:
        rgb = cv2.resize(rgb, None, fx=scale, fy=scale)
    return rgb, nb

# big side-by-side of the tail and a leg
tail = process(os.path.join(HERE, "parts_v8", "part_01.png"))[0]   # tail
leg  = process(os.path.join(HERE, "parts_v8", "part_07.png"))[0]   # leg
h = max(tail.shape[0], leg.shape[0])
def pad(im):
    out = np.full((h, im.shape[1], 3), 250, np.uint8); out[:im.shape[0]] = im; return out
cv2.imwrite(os.path.join(HERE, "skeleton_demo2.png"), np.hstack([pad(tail), np.full((h,20,3),250,np.uint8), pad(leg)]))
print("wrote skeleton_demo2.png")
