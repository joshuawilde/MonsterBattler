#!/usr/bin/env python3
"""Exactly the user's recipe, shown step by step:
  1. convex hull of the body part
  2. intersect with the ORIGINAL IMAGE silhouette (clip away anything outside the creature)
  3. infill the remaining area (clipped - body)
"""
import os
import numpy as np
import cv2
from scipy.ndimage import distance_transform_edt, binary_fill_holes

HERE = os.path.dirname(__file__)

# --- body mask (from segmap blue region), in full-image coords ---
seg = cv2.cvtColor(cv2.imread(os.path.join(HERE, "segmap.png")), cv2.COLOR_BGR2RGB)
H, W = seg.shape[:2]
dist_blue = np.linalg.norm(seg.astype(int) - np.array([0, 0, 255]), axis=2)
dist_white = np.linalg.norm(seg.astype(int) - np.array([255, 255, 255]), axis=2)
# nearest-of-all so the body mask is exactly the blue region
PAL = np.array([[255,0,0],[255,128,0],[0,0,255],[0,255,0],[255,255,0],
                [255,0,255],[0,255,255],[140,0,255],[255,255,255]])
lab = np.linalg.norm(seg.reshape(-1,1,3).astype(int)-PAL[None],axis=2).argmin(1).reshape(H,W)
body = (lab == 2).astype(np.uint8)

# --- STEP 2 mask: silhouette from the ORIGINAL IMAGE ---
orig = cv2.resize(cv2.imread(os.path.join(HERE, "reference_src.jpeg")), (W, H))
gray = cv2.cvtColor(orig, cv2.COLOR_BGR2GRAY)
creature = (gray < 238).astype(np.uint8)
creature = cv2.morphologyEx(creature, cv2.MORPH_CLOSE, np.ones((7,7),np.uint8))
creature = binary_fill_holes(creature).astype(np.uint8)

# --- STEP 1: convex hull of the body ---
pts = np.column_stack(np.where(body.T > 0))
hull = cv2.convexHull(pts)
hullmask = np.zeros((H,W),np.uint8); cv2.fillConvexPoly(hullmask, hull, 1)

# --- STEP 2: clip hull by the original silhouette ---
clipped = (hullmask & creature)
clipped = (clipped | body).astype(np.uint8)

# --- STEP 3: infill the remaining area (clipped - body) ---
band = ((clipped==1)&(body==0)).astype(np.uint8)
src = cv2.erode(body, np.ones((13,13),np.uint8)); src = src if src.sum()>100 else body
idx = distance_transform_edt(src==0, return_indices=True)[1]
filled = cv2.medianBlur(orig[idx[0],idx[1]], 21)
out = np.full((H,W,3),255,np.uint8); out[body==1]=orig[body==1]; out[band==1]=filled[band==1]

# crop to clipped bbox + save part
ys,xs=np.where(clipped>0); y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
cv2.imwrite(os.path.join(HERE,"body_filled2.png"),
            np.dstack([out,(clipped*255).astype(np.uint8)])[y0:y1,x0:x1])

# step panels
def vis(m, base=None):
    img = np.full((H,W,3),255,np.uint8)
    if base is not None: img[base==1]=orig[base==1]
    img[m==1]=(0,200,0) if base is not None else (60,60,60)
    return img
panels = {
    "1 body": vis(body),
    "2a hull": vis(hullmask),
    "2b hull AND orig-silhouette": vis((clipped&(body==0)).astype(np.uint8), body),
    "3 infilled": out,
}
crop=lambda im:im[y0:y1,x0:x1]
cells=[]
for name,im in panels.items():
    c=crop(im).copy(); cv2.putText(c,name,(6,22),cv2.FONT_HERSHEY_SIMPLEX,0.55,(0,0,200),2); cells.append(c)
h=max(c.shape[0] for c in cells)
pad=lambda i:np.vstack([i,np.full((h-i.shape[0],i.shape[1],3),250,np.uint8)])
cv2.imwrite(os.path.join(HERE,"fill_steps.png"),
            np.hstack([x for c in cells for x in (pad(c),np.full((h,10,3),255,np.uint8))][:-1]))
print("wrote fill_steps.png + body_filled2.png")
