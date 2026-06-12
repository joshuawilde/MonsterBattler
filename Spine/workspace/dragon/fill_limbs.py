#!/usr/bin/env python3
"""Correct fill region: body needs completing ONLY behind the LIMBS (which cover
it and swing). Exclude head/jaw/tail/spikes. Region = body UNION the 4 limb masks.
Fill the added area with body color. Uses the segmap part masks directly."""
import os
import numpy as np
import cv2
from scipy.ndimage import distance_transform_edt

HERE = os.path.dirname(__file__)
PAL = [("head",(255,0,0)),("jaw",(255,128,0)),("body",(0,0,255)),("arm-R",(0,255,0)),
       ("arm-L",(255,255,0)),("leg-R",(255,0,255)),("leg-L",(0,255,255)),("tail",(140,0,255)),("_bg",(255,255,255))]
seg = cv2.cvtColor(cv2.imread(os.path.join(HERE,"segmap.png")), cv2.COLOR_BGR2RGB)
H,W = seg.shape[:2]
orig = cv2.resize(cv2.imread(os.path.join(HERE,"reference_src.jpeg")),(W,H))
arr = np.array([c for _,c in PAL])
lab = np.linalg.norm(seg.reshape(-1,1,3).astype(int)-arr[None],axis=2).argmin(1).reshape(H,W)
mask = lambda name: (lab==[n for n,_ in PAL].index(name)).astype(np.uint8)

body = mask("body")
limbs = np.clip(mask("arm-R")+mask("arm-L")+mask("leg-R")+mask("leg-L"),0,1)
target = np.clip(body + limbs, 0, 1).astype(np.uint8)
# close small seams between body and limb so it's one solid silhouette
target = cv2.morphologyEx(target, cv2.MORPH_CLOSE, np.ones((9,9),np.uint8))
band = ((target==1)&(body==0)).astype(np.uint8)

src = cv2.erode(body, np.ones((13,13),np.uint8)); src = src if src.sum()>100 else body
idx = distance_transform_edt(src==0, return_indices=True)[1]
filled = cv2.medianBlur(orig[idx[0],idx[1]],21)
out = np.full((H,W,3),255,np.uint8); out[body==1]=orig[body==1]; out[band==1]=filled[band==1]

ys,xs=np.where(target>0); y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
cv2.imwrite(os.path.join(HERE,"body_filled3.png"),
            np.dstack([out,(target*255).astype(np.uint8)])[y0:y1,x0:x1])

# viz: region (green over body) | filled
reg=np.full((H,W,3),255,np.uint8); reg[band==1]=(0,200,0); reg[body==1]=orig[body==1]
crop=lambda im:im[y0:y1,x0:x1]
a,b=crop(reg),crop(out)
cv2.putText(a,"region = body + limbs only",(6,22),cv2.FONT_HERSHEY_SIMPLEX,0.55,(0,0,200),2)
cv2.putText(b,"filled body",(6,22),cv2.FONT_HERSHEY_SIMPLEX,0.55,(0,0,200),2)
cv2.imwrite(os.path.join(HERE,"fill_limbs.png"),np.hstack([a,np.full((a.shape[0],12,3),255,np.uint8),b]))
print("wrote fill_limbs.png + body_filled3.png")
