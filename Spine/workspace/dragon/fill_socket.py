#!/usr/bin/env python3
"""Socket fill, clipped to the original silhouette.
  nearby   = limb pixels within D px of the body (the sockets, not whole limbs)
  target   = (body UNION nearby) INTERSECT creature-silhouette   # no spill outside outline
  fill band = target - body, filled with nearest interior body color (non-AI)
"""
import os
import numpy as np
import cv2
from scipy.ndimage import distance_transform_edt, binary_fill_holes

HERE = os.path.dirname(__file__)
PAL=[(255,0,0),(255,128,0),(0,0,255),(0,255,0),(255,255,0),(255,0,255),(0,255,255),(140,0,255),(255,255,255)]
NAMES=["head","jaw","body","arm-R","arm-L","leg-R","leg-L","tail","bg"]
D = 70   # how far a limb counts as "near the body" (socket reach)

seg=cv2.cvtColor(cv2.imread(os.path.join(HERE,"segmap.png")),cv2.COLOR_BGR2RGB); H,W=seg.shape[:2]
orig=cv2.resize(cv2.imread(os.path.join(HERE,"reference_src.jpeg")),(W,H))
lab=np.linalg.norm(seg.reshape(-1,1,3).astype(int)-np.array(PAL)[None],axis=2).argmin(1).reshape(H,W)
m=lambda n:(lab==NAMES.index(n)).astype(np.uint8)

body=m("body")
limbs=np.clip(m("arm-R")+m("arm-L")+m("leg-R")+m("leg-L"),0,1)
# creature silhouette from the ORIGINAL image (clip mask)
gray=cv2.cvtColor(orig,cv2.COLOR_BGR2GRAY)
creature=binary_fill_holes(cv2.morphologyEx((gray<238).astype(np.uint8),cv2.MORPH_CLOSE,np.ones((7,7),np.uint8))).astype(np.uint8)

near = (limbs & cv2.dilate(body, cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(2*D+1,2*D+1)))).astype(np.uint8)
target = np.clip(body+near,0,1).astype(np.uint8)
target = cv2.morphologyEx(target, cv2.MORPH_CLOSE, np.ones((11,11),np.uint8))
target = (target & creature)                 # CLIP to original outline -> no spill
target = (target | body).astype(np.uint8)
band = ((target==1)&(body==0)).astype(np.uint8)

src=cv2.erode(body,np.ones((13,13),np.uint8)); src=src if src.sum()>100 else body
idx=distance_transform_edt(src==0,return_indices=True)[1]
filled=cv2.medianBlur(orig[idx[0],idx[1]],21)
out=np.full((H,W,3),255,np.uint8); out[body==1]=orig[body==1]; out[band==1]=filled[band==1]

ys,xs=np.where(target>0); y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
cv2.imwrite(os.path.join(HERE,"body_socket.png"),
            np.dstack([out,(target*255).astype(np.uint8)])[y0:y1,x0:x1])

reg=(orig*0.4+255*0.6).astype(np.uint8); reg[band==1]=(0,200,0); reg[body==1]=orig[body==1]
crop=lambda im:im[y0:y1,x0:x1]
a,b=crop(reg).copy(),crop(out).copy()
cv2.putText(a,"socket region (green)",(6,22),cv2.FONT_HERSHEY_SIMPLEX,0.55,(0,0,200),2)
cv2.putText(b,"filled body",(6,22),cv2.FONT_HERSHEY_SIMPLEX,0.55,(0,0,200),2)
cv2.imwrite(os.path.join(HERE,"fill_socket.png"),np.hstack([a,np.full((a.shape[0],12,3),255,np.uint8),b]))
print("D=%d  band px=%d -> wrote fill_socket.png + body_socket.png"%(D,int(band.sum())))
