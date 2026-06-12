#!/usr/bin/env python3
"""Extract the orange bone line Gemini drew: joint dots (erode-isolated) + the
centerline order. Renders clean bones onto the original part."""
import os, sys
import numpy as np
import cv2
from collections import deque
from skimage.morphology import skeletonize

HERE = os.path.dirname(__file__)

def orange_mask(bgr):
    b, g, r = bgr[:,:,0].astype(int), bgr[:,:,1].astype(int), bgr[:,:,2].astype(int)
    return ((r > 150) & (g > 30) & (g < 170) & (b < 100) & (r - b > 90) & (r - g > 60)).astype(np.uint8)

def neighbors(p, sk):
    y, x = p
    for dy in (-1,0,1):
        for dx in (-1,0,1):
            if dy or dx:
                ny,nx=y+dy,x+dx
                if 0<=ny<sk.shape[0] and 0<=nx<sk.shape[1] and sk[ny,nx]: yield (ny,nx)

def longest_path(sk):
    pts=np.argwhere(sk)
    if len(pts)==0: return []
    def bfs(s):
        seen={s:None}; q=deque([s]); last=s
        while q:
            c=q.popleft(); last=c
            for n in neighbors(c,sk):
                if n not in seen: seen[n]=c; q.append(n)
        return last,seen
    a,_=bfs(tuple(pts[0])); b,seen=bfs(a)
    path=[]; c=b
    while c is not None: path.append(c); c=seen[c]
    return path

def extract(stem):
    img = cv2.imread(os.path.join(HERE, f"boneline_{stem}.png"))
    om = orange_mask(img)
    # joints = dots: erode the thin line away, leaving the fatter dots
    er = cv2.erode(om, cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(7,7)))
    n, lab, stats, cent = cv2.connectedComponentsWithStats(er, 8)
    joints = [tuple(cent[i][::-1]) for i in range(1,n) if stats[i,cv2.CC_STAT_AREA] >= 6]  # (y,x)
    # centerline order from skeleton of full orange
    sk = skeletonize(om > 0)
    cl = longest_path(sk)
    if cl:
        clp = np.array(cl, float)
        d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(clp,axis=0),axis=1))]
        def arclen(pt):
            i = np.argmin(np.linalg.norm(clp - np.array(pt), axis=1)); return d[i]
        joints.sort(key=arclen)
    return joints, cl

def render(stem, joints, cl):
    # draw on the boneline image itself (same coord space), dim the orange so bones read
    img = cv2.imread(os.path.join(HERE, f"boneline_{stem}.png"))
    rgb = img.copy()
    om = orange_mask(img).astype(bool)
    rgb[om] = (rgb[om]*0.35 + np.array([245,245,245])*0.65).astype(np.uint8)  # fade the orange
    for j in range(len(joints)-1):
        cv2.line(rgb,(int(joints[j][1]),int(joints[j][0])),(int(joints[j+1][1]),int(joints[j+1][0])),(180,40,0),4)
    for (y,x) in joints:
        cv2.circle(rgb,(int(x),int(y)),11,(0,200,255),-1); cv2.circle(rgb,(int(x),int(y)),11,(20,20,20),2)
    return rgb

def resample(cl, n):
    p=np.array(cl,float)
    d=np.r_[0,np.cumsum(np.linalg.norm(np.diff(p,axis=0),axis=1))]
    if d[-1]==0: return [tuple(p[0])]
    xs=np.linspace(0,d[-1],n); out=[]
    for x in xs:
        i=min(max(np.searchsorted(d,x),1),len(p)-1)
        t=(x-d[i-1])/max(d[i]-d[i-1],1e-6); out.append(tuple(p[i-1]*(1-t)+p[i]*t))
    return out

outs=[]
for stem in ("part_01","part_07"):
    j,cl=extract(stem)
    length=np.sum(np.linalg.norm(np.diff(np.array(cl),axis=0),axis=1)) if len(cl)>1 else 0
    nb=int(np.clip(round(length/130)+1,2,5))
    bones=resample(cl,nb) if len(cl)>1 else j
    print(stem,"line_len=%d ->"%length, nb,"bones")
    outs.append(render(stem,bones,cl))
h=max(o.shape[0] for o in outs)
pad=lambda i:np.vstack([i,np.full((h-i.shape[0],i.shape[1],3),245,np.uint8)])
cv2.imwrite(os.path.join(HERE,"boneline_extracted.png"),np.hstack([pad(outs[0]),np.full((h,20,3),245,np.uint8),pad(outs[1])]))
print("wrote boneline_extracted.png")
