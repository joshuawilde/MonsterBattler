#!/usr/bin/env python3
"""Occlusion fill via the user's CSG idea:
  target = convexHull(body) INTERSECT full-creature-silhouette
  band   = target minus body        (the occluded area inside the creature)
  fill   = nearest body pixel color  (non-AI; no hallucination, no background bleed)
Works in full-image coords using the segmap, so geometry is exact."""
import os
import numpy as np
import cv2
from scipy.ndimage import distance_transform_edt

HERE = os.path.dirname(__file__)
COLORS = [("head",(255,0,0)),("jaw",(255,128,0)),("body",(0,0,255)),("arm-R",(0,255,0)),
          ("arm-L",(255,255,0)),("leg-R",(255,0,255)),("leg-L",(0,255,255)),("tail",(140,0,255))]
BG = (255,255,255)

seg = cv2.cvtColor(cv2.imread(os.path.join(HERE,"segmap.png")), cv2.COLOR_BGR2RGB)
orig = cv2.resize(cv2.imread(os.path.join(HERE,"reference_src.jpeg")), (seg.shape[1], seg.shape[0]))
H,W = seg.shape[:2]
palette = np.array([c for _,c in COLORS]+[BG], float)
label = np.linalg.norm(seg.reshape(-1,1,3).astype(float)-palette[None],axis=2).argmin(1).reshape(H,W)

body = (label==2).astype(np.uint8)               # body index
creature = (label!=len(COLORS)).astype(np.uint8) # not background

pts = np.column_stack(np.where(body.T>0))
hull = cv2.convexHull(pts)
hullmask = np.zeros((H,W),np.uint8); cv2.fillConvexPoly(hullmask, hull, 1)

target = (hullmask & creature)                   # CSG: hull ∩ creature
target = (target | body).astype(np.uint8)
band = ((target==1)&(body==0)).astype(np.uint8)  # occluded region to fill

# nearest body pixel color, but from the INTERIOR only (erode away the black
# outline so we extend scale colors, not the outline), then smooth.
src = cv2.erode(body, cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(13,13)))
if src.sum() < 100: src = body
idx = distance_transform_edt(src==0, return_indices=True)[1]
filled = orig[idx[0], idx[1]]
filled = cv2.medianBlur(filled, 21)

out = np.full((H,W,3),255,np.uint8)
out[body==1] = orig[body==1]
out[band==1] = filled[band==1]
alpha = (target*255).astype(np.uint8)
ys,xs = np.where(target>0)
y0,y1,x0,x1 = ys.min(),ys.max()+1,xs.min(),xs.max()+1
bgra = np.dstack([out, alpha])[y0:y1, x0:x1]
cv2.imwrite(os.path.join(HERE,"body_filled.png"), bgra)

# before/after viz
before = np.full((H,W,3),255,np.uint8); before[body==1]=orig[body==1]
viz_t = np.full((H,W,3),255,np.uint8); viz_t[target==1]=(0,255,0); viz_t[body==1]=orig[body==1]
def crop(im): return im[y0:y1, x0:x1]
trip = [crop(before), crop(viz_t), crop(out)]
h=max(t.shape[0] for t in trip)
pad=lambda i:np.vstack([i,np.full((h-i.shape[0],i.shape[1],3),250,np.uint8)])
cv2.imwrite(os.path.join(HERE,"fill_csg_compare.png"),
            np.hstack([pad(trip[0]),np.full((h,12,3),255,np.uint8),pad(trip[1]),
                       np.full((h,12,3),255,np.uint8),pad(trip[2])]))
print("band px:", int(band.sum()), "-> wrote body_filled.png + fill_csg_compare.png")
