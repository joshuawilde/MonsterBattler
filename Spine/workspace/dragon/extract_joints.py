#!/usr/bin/env python3
"""Extract Gemini's joint dots by color -> pivot positions (image coords).
Real dots are VERY saturated pure colors (S>190); the desaturated blue body is
rejected. Limb dots are assigned to the nearest matching limb part (avg if many)."""
import os, json
import numpy as np, cv2

HERE=os.path.dirname(__file__)
joints=cv2.cvtColor(cv2.imread(os.path.join(HERE,"joints.png")),cv2.COLOR_BGR2RGB); H,W=joints.shape[:2]
hsv=cv2.cvtColor(joints,cv2.COLOR_RGB2HSV)
seg=cv2.resize(cv2.cvtColor(cv2.imread(os.path.join(HERE,"segmap.png")),cv2.COLOR_BGR2RGB),(W,H))
PAL=[(255,0,0),(255,128,0),(0,0,255),(0,255,0),(255,255,0),(255,0,255),(0,255,255),(140,0,255),(255,255,255)]
NAMES=["head","jaw","body","arm-R","arm-L","leg-R","leg-L","tail","bg"]
lab=np.linalg.norm(seg.reshape(-1,1,3).astype(int)-np.array(PAL)[None],axis=2).argmin(1).reshape(H,W)
pc=lambda n:(np.where(lab==NAMES.index(n))[1].mean(),np.where(lab==NAMES.index(n))[0].mean())

def dots(target,dist=80):
    d=np.linalg.norm(joints.astype(int)-np.array(target),axis=2)
    mask=((d<dist)&(hsv[:,:,1]>190)&(hsv[:,:,2]>110)).astype(np.uint8)
    mask=cv2.morphologyEx(mask,cv2.MORPH_OPEN,np.ones((3,3),np.uint8))
    n,l,st,ct=cv2.connectedComponentsWithStats(mask,8)
    return [(float(ct[i][0]),float(ct[i][1])) for i in range(1,n) if 150<st[i,cv2.CC_STAT_AREA]<3500]

def best(target):
    ds=dots(target); return min(ds,key=lambda p:0) if ds else None
def one(target):
    ds=dots(target); return ds[0] if ds else None

piv={}
piv["jaw"]=one((232,7,6))
piv["head"]=one((238,143,14))     # neck base = head pivot
piv["tail"]=one((155,32,196))
# shoulders (green) -> arms, hips (blue) -> legs; assign each dot to nearest limb, average
def assign(targets_dots, parts):
    acc={p:[] for p in parts}
    for dot in targets_dots:
        p=min(parts,key=lambda pr:(dot[0]-pc(pr)[0])**2+(dot[1]-pc(pr)[1])**2); acc[p].append(dot)
    return {p:(np.mean([d[0] for d in v]),np.mean([d[1] for d in v])) for p,v in acc.items() if v}
piv.update(assign(dots((43,189,37)), ["arm-R","arm-L"]))
piv.update(assign(dots((10,53,234)), ["leg-R","leg-L"]))

piv={k:[float(v[0]),float(v[1])] for k,v in piv.items() if v}
json.dump(piv,open(os.path.join(HERE,"joints_extracted.json"),"w"),indent=2)
print("extracted",len(piv),"joints:",{k:(round(v[0]),round(v[1])) for k,v in piv.items()})
vis=cv2.cvtColor(joints,cv2.COLOR_RGB2BGR).copy()
for k,v in piv.items():
    cv2.drawMarker(vis,(int(v[0]),int(v[1])),(0,0,0),cv2.MARKER_TILTED_CROSS,34,4)
    cv2.putText(vis,k,(int(v[0])+10,int(v[1])-6),cv2.FONT_HERSHEY_SIMPLEX,0.7,(0,0,0),2)
cv2.imwrite(os.path.join(HERE,"joints_detected.png"),vis)
print("wrote joints_detected.png")
