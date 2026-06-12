#!/usr/bin/env python3
"""Build a layered Spine rig from the segmap parts at their exact positions.
Stage 1: static assembly (one bone per part at its center) + simple animations.
Uses the socket-filled body so limbs have body behind them."""
import os, json, shutil
import numpy as np, cv2
from scipy.ndimage import distance_transform_edt, binary_fill_holes

HERE=os.path.dirname(__file__)
PAL=[(255,0,0),(255,128,0),(0,0,255),(0,255,0),(255,255,0),(255,0,255),(0,255,255),(140,0,255),(255,255,255)]
NAMES=["head","jaw","body","arm-R","arm-L","leg-R","leg-L","tail","bg"]; D=70

seg=cv2.cvtColor(cv2.imread(os.path.join(HERE,"segmap.png")),cv2.COLOR_BGR2RGB); H,W=seg.shape[:2]
orig=cv2.resize(cv2.imread(os.path.join(HERE,"reference_src.jpeg")),(W,H))
lab=np.linalg.norm(seg.reshape(-1,1,3).astype(int)-np.array(PAL)[None],axis=2).argmin(1).reshape(H,W)
m=lambda n:(lab==NAMES.index(n)).astype(np.uint8)

# socket-filled body
body=m("body"); limbs=np.clip(sum(m(n) for n in ["arm-R","arm-L","leg-R","leg-L"]),0,1)
gray=cv2.cvtColor(orig,cv2.COLOR_BGR2GRAY)
creature=binary_fill_holes(cv2.morphologyEx((gray<238).astype(np.uint8),cv2.MORPH_CLOSE,np.ones((7,7),np.uint8))).astype(np.uint8)
near=(limbs & cv2.dilate(body,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(2*D+1,2*D+1)))).astype(np.uint8)
btar=cv2.morphologyEx(np.clip(body+near,0,1).astype(np.uint8),cv2.MORPH_CLOSE,np.ones((11,11),np.uint8))
btar=((btar&creature)|body).astype(np.uint8)
bband=((btar==1)&(body==0)).astype(np.uint8)
src=cv2.erode(body,np.ones((13,13),np.uint8)); idx=distance_transform_edt((src if src.sum()>100 else body)==0,return_indices=True)[1]
bodyfill=orig.copy(); bodyfill[bband==1]=cv2.medianBlur(orig[idx[0],idx[1]],21)[bband==1]

RIG=os.path.join(HERE,"rig_seg"); shutil.rmtree(RIG,ignore_errors=True); os.makedirs(RIG)
def save_part(name, mask, img):
    ys,xs=np.where(mask>0); y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
    bgra=np.dstack([img,(mask*255).astype(np.uint8)])[y0:y1,x0:x1]
    cv2.imwrite(os.path.join(RIG,name+".png"),bgra)
    return ((x0+x1)/2, (y0+y1)/2)   # center in image coords

centers={}
centers["body"]=save_part("body", btar, bodyfill)
for n in ["head","jaw","arm-R","arm-L","leg-R","leg-L","tail"]:
    centers[n]=save_part(n, m(n), orig)

# joint pivot for each part = the part pixel nearest the body centroid
bys,bxs=np.where(btar>0); bcx,bcy=bxs.mean(),bys.mean()
def joint(name):
    mk=cv2.imread(os.path.join(RIG,name+".png"),cv2.IMREAD_UNCHANGED)[:,:,3]
    # joint in full-image coords: nearest opaque pixel to body centroid
    ys,xs=np.where(btar>0) if name=="body" else (None,None)
    full=m(name) if name!="body" else btar
    ys,xs=np.where(full>0); d=(xs-bcx)**2+(ys-bcy)**2; i=d.argmin()
    return (xs[i],ys[i])
pivots={n:joint(n) for n in centers}
pivots["body"]=(bcx,bcy); pivots["head"]=centers["head"]   # head rotates ~ its center

# hierarchy
PARENT={"body":"root","head":"body","jaw":"head","tail":"body",
        "arm-R":"body","arm-L":"body","leg-R":"body","leg-L":"body"}
Z=["tail","arm-L","leg-L","body","leg-R","arm-R","jaw","head"]  # draw order back->front

def world(name):  # bone world pos in spine coords (y up, origin = image center-x, bottom)
    px,py=pivots[name]; return (px-W/2, H-py)
bones=[{"name":"root"}]
for n in ["body","head","jaw","tail","arm-R","arm-L","leg-R","leg-L"]:
    wx,wy=world(n); pwx,pwy=(0,0) if PARENT[n]=="root" else world(PARENT[n])
    bones.append({"name":n,"parent":PARENT[n],"x":round(wx-pwx,1),"y":round(wy-pwy,1)})
slots=[]; atts={}
for n in Z:
    slots.append({"name":n,"bone":n,"attachment":n})
    im=cv2.imread(os.path.join(RIG,n+".png"),cv2.IMREAD_UNCHANGED)
    cx,cy=centers[n]; px,py=pivots[n]
    atts[n]={"width":im.shape[1],"height":im.shape[0],"x":round(cx-px,1),"y":round(py-cy,1)}

def kf(t,a): return {"time":round(t,3),"angle":a}
def kft(t,x,y): return {"time":round(t,3),"x":x,"y":y}
idle={"bones":{
  "body":{"translate":[kft(0,0,0),kft(1.2,0,7),kft(2.4,0,0)]},
  "head":{"rotate":[kf(0,0),kf(1.2,-2.5),kf(2.4,0)]},
  "jaw":{"rotate":[kf(0,0),kf(1.2,4),kf(2.4,0)]},
  "tail":{"rotate":[kf(0,0),kf(0.8,7),kf(1.6,-7),kf(2.4,0)]},
  "arm-R":{"rotate":[kf(0,0),kf(1.2,3),kf(2.4,0)]},
}}
roar={"bones":{
  "jaw":{"rotate":[kf(0,0),kf(0.18,28),kf(0.9,28),kf(1.2,0)]},
  "head":{"rotate":[kf(0,0),kf(0.18,-10),kf(0.9,-10),kf(1.2,0)]},
  "body":{"translate":[kft(0,0,0),kft(0.18,0,5),kft(1.2,0,0)]},
  "tail":{"rotate":[kf(0,0),kf(0.3,-12),kf(0.7,10),kf(1.2,0)]},
}}
config={"skeleton":{"name":"dragon","width":W,"height":H},"bones":bones,"slots":slots,
        "attachments":atts,"animations":[],"custom_animations":{"idle":idle,"roar":roar}}
json.dump(config,open(os.path.join(HERE,"seg_config.json"),"w"),indent=2)
print("bones:",len(bones),"| anims: idle, roar | wrote seg_config.json")
