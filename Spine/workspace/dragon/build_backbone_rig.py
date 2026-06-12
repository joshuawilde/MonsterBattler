#!/usr/bin/env python3
"""Rig from Gemini's drawn BACKBONE: extract the orange spine + magenta jaw line,
sample a hip-rooted bone chain, weight the torso mesh to it. Limbs separate.
Writes mesh_dragon.json (Spine 4.2, rotate uses 'value')."""
import os, json, shutil, math
import numpy as np, cv2
from collections import deque
from skimage.morphology import skeletonize
from scipy.spatial import Delaunay
from scipy.ndimage import distance_transform_edt, binary_fill_holes

HERE=os.path.dirname(__file__)
PAL=[(255,0,0),(255,128,0),(0,0,255),(0,255,0),(255,255,0),(255,0,255),(0,255,255),(140,0,255),(255,255,255)]
NM=["head","jaw","body","arm-R","arm-L","leg-R","leg-L","tail","bg"]; D=70
seg=cv2.cvtColor(cv2.imread(os.path.join(HERE,"segmap.png")),cv2.COLOR_BGR2RGB); H,W=seg.shape[:2]
orig=cv2.resize(cv2.imread(os.path.join(HERE,"reference_src.jpeg")),(W,H))
lab=np.linalg.norm(seg.reshape(-1,1,3).astype(int)-np.array(PAL)[None],axis=2).argmin(1).reshape(H,W)
m=lambda n:(lab==NM.index(n)).astype(np.uint8)
creature=binary_fill_holes(cv2.morphologyEx((lab!=8).astype(np.uint8),cv2.MORPH_CLOSE,np.ones((7,7),np.uint8))).astype(np.uint8)
GJ=json.load(open(os.path.join(HERE,"joints_extracted.json")))
s_x=lambda ix:float(ix-W/2); s_y=lambda iy:float(H-iy)

# ---- torso (creature minus limbs, socket-filled) ----
limbs=np.clip(sum(m(n) for n in ["arm-R","arm-L","leg-R","leg-L"]),0,1)
nonlimb=((creature==1)&(limbs==0)).astype(np.uint8)
tnear=(limbs & cv2.dilate(nonlimb,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(2*D+1,2*D+1)))).astype(np.uint8)
ttar=cv2.morphologyEx(np.clip(nonlimb+tnear,0,1).astype(np.uint8),cv2.MORPH_CLOSE,np.ones((11,11),np.uint8))
ttar=((ttar&creature)|nonlimb).astype(np.uint8); tband=((ttar==1)&(nonlimb==0)).astype(np.uint8)
tsrc=cv2.erode(nonlimb,np.ones((13,13),np.uint8)); tidx=distance_transform_edt((tsrc if tsrc.sum()>100 else nonlimb)==0,return_indices=True)[1]
torso_img=orig.copy(); torso_img[tband==1]=cv2.medianBlur(orig[tidx[0],tidx[1]],21)[tband==1]

RIG=os.path.join(HERE,"rig_mesh"); shutil.rmtree(RIG,ignore_errors=True); os.makedirs(RIG)
def save_part(name,mask,img):
    ys,xs=np.where(mask>0); y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
    cv2.imwrite(os.path.join(RIG,name+".png"),np.dstack([img,(mask*255).astype(np.uint8)])[y0:y1,x0:x1])
    return (x0,y0,x1,y1),mask
bbox={}; gmask={}
bbox["torso"],gmask["torso"]=save_part("torso",ttar,torso_img)
for n in ["arm-R","arm-L","leg-R","leg-L"]: bbox[n],gmask[n]=save_part(n,m(n),orig)
def cleanmask(mk):
    mk=mk.astype(np.uint8); n,l,st,_=cv2.connectedComponentsWithStats(mk,8)
    return mk if n<=1 else (l==1+int(np.argmax(st[1:,cv2.CC_STAT_AREA]))).astype(np.uint8)

# ---- extract backbone + jaw lines from backbone.png ----
bbimg=cv2.cvtColor(cv2.imread(os.path.join(HERE,"backbone.png")),cv2.COLOR_BGR2RGB)
bbimg=cv2.resize(bbimg,(W,H)); hsv=cv2.cvtColor(bbimg,cv2.COLOR_RGB2HSV)
def neighbors(p,sk):
    y,x=p
    for dy in(-1,0,1):
        for dx in(-1,0,1):
            if (dy or dx) and 0<=y+dy<sk.shape[0] and 0<=x+dx<sk.shape[1] and sk[y+dy,x+dx]: yield(y+dy,x+dx)
def longest(sk):
    pts=np.argwhere(sk)
    def bfs(s):
        seen={s:None};q=deque([s]);last=s
        while q:
            c=q.popleft();last=c
            for nb in neighbors(c,sk):
                if nb not in seen: seen[nb]=c;q.append(nb)
        return last,seen
    a,_=bfs(tuple(pts[0]));b,sn=bfs(a);path=[];c=b
    while c is not None: path.append(c);c=sn[c]
    return np.array(path,float)  # (y,x)
def extract_line(target,dist=90):
    d=np.linalg.norm(bbimg.astype(int)-np.array(target),axis=2)
    mask=((d<dist)&(hsv[:,:,1]>120)&(hsv[:,:,2]>120)).astype(np.uint8)
    mask=cv2.morphologyEx(mask,cv2.MORPH_CLOSE,np.ones((7,7),np.uint8))
    mask=cleanmask(mask); return longest(skeletonize(mask>0))
def resample(cl,n):
    p=cl[:, ::-1]  # ->(x,y)
    d=np.r_[0,np.cumsum(np.linalg.norm(np.diff(p,axis=0),axis=1))]
    return np.array([p[min(np.searchsorted(d,t),len(p)-1)] for t in np.linspace(0,d[-1],n)])

NB=14
back=resample(extract_line((255,122,0)),NB)   # (x,y) snout..tail OR tail..snout
jawl=resample(extract_line((255,0,255)),3)
headl=resample(extract_line((255,255,0)),3)
# orient: index 0 = head end (nearest head centroid)
hcx,hcy=np.where(m("head"))[1].mean(),np.where(m("head"))[0].mean()
if (back[0,0]-hcx)**2+(back[0,1]-hcy)**2 > (back[-1,0]-hcx)**2+(back[-1,1]-hcy)**2: back=back[::-1]
# hip index = nearest backbone point to leg midpoint
lr=GJ["leg-R"]; ll=GJ["leg-L"]; hipxy=((lr[0]+ll[0])/2,(lr[1]+ll[1])/2)
hip_idx=int(np.argmin([(p[0]-hipxy[0])**2+(p[1]-hipxy[1])**2 for p in back]))
def _orient(L,t): return L if (L[0,0]-t[0])**2+(L[0,1]-t[1])**2<=(L[-1,0]-t[0])**2+(L[-1,1]-t[1])**2 else L[::-1]
headl=_orient(headl,back[0]); jawl=_orient(jawl,back[0])

# ---- bones ----
bones=[{"name":"root"}]; bw={"root":(0,0)}
def addbone(nm,par,wxy):
    pw=bw[par]; bones.append({"name":nm,"parent":par,"x":round(wxy[0]-pw[0],1),"y":round(wxy[1]-pw[1],1)}); bw[nm]=wxy
bbn=lambda i:"bb%d"%i
addbone(bbn(hip_idx),"root",(s_x(back[hip_idx,0]),s_y(back[hip_idx,1])))
for i in range(hip_idx-1,-1,-1):   addbone(bbn(i),bbn(i+1),(s_x(back[i,0]),s_y(back[i,1])))   # head side
for i in range(hip_idx+1,NB):      addbone(bbn(i),bbn(i-1),(s_x(back[i,0]),s_y(back[i,1])))   # tail side
def nearest_bb(pt): return int(np.argmin([(back[i,0]-pt[0])**2+(back[i,1]-pt[1])**2 for i in range(NB)]))
# head + jaw branch off the neck-base backbone bone bb0 (bifurcation)
addbone("head",bbn(0),(s_x(headl[0,0]),s_y(headl[0,1])))
addbone("jaw","head",(s_x(jawl[0,0]),s_y(jawl[0,1])))
# limbs
def far_tip(mask_name,jt):
    ys,xs=np.where(cleanmask(m(mask_name))>0); i=((xs-jt[0])**2+(ys-jt[1])**2).argmax(); return (float(xs[i]),float(ys[i]))
piv={}
for n in ["arm-R","arm-L","leg-R","leg-L"]:
    p=GJ[n]; piv[n]=p; addbone(n,bbn(nearest_bb(p)),(s_x(p[0]),s_y(p[1])))
bidx={b["name"]:i for i,b in enumerate(bones)}

# ---- skinning ----
def _tri(mask,bb):
    x0,y0,x1,y1=bb
    cnts,_=cv2.findContours(mask.astype(np.uint8),cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
    ap=cv2.approxPolyDP(max(cnts,key=cv2.contourArea),3.0,True).reshape(-1,2).astype(float)
    gx,gy=np.meshgrid(np.arange(x0+10,x1,24),np.arange(y0+10,y1,24))
    ins=[p for p in np.stack([gx.ravel(),gy.ravel()],1) if mask[int(min(p[1],H-1)),int(min(p[0],W-1))]>0]
    v=np.vstack([ap]+([np.array(ins)] if ins else [])); hn=len(ap)
    keep=[t for t in Delaunay(v).simplices if mask[int(min(v[t].mean(0)[1],H-1)),int(min(v[t].mean(0)[0],W-1))]>0]
    return v,np.array(keep),hn
# candidates for torso = backbone points (each its bone) + jaw line points (->jaw)
CAND=[(back[i],bbn(i)) for i in range(NB)]+[(p,"head") for p in headl]+[(p,"jaw") for p in jawl]
def torso_mesh():
    x0,y0,x1,y1=bbox["torso"]; w,h=x1-x0,y1-y0; v,keep,hn=_tri(gmask["torso"],bbox["torso"])
    uvs=[]; vert=[]
    for vx,vy in v:
        uvs+=[round((vx-x0)/w,5),round((vy-y0)/h,5)]
        d=np.array([(vx-c[0][0])**2+(vy-c[0][1])**2 for c in CAND]); o=np.argsort(d)[:2]
        wt=1.0/(np.sqrt(d[o])+4); wt=wt/wt.sum(); e=[2]
        for k,ci in enumerate(o):
            bn=CAND[ci][1]; bx,by=bw[bn]; e+=[bidx[bn],round(s_x(vx)-bx,2),round(s_y(vy)-by,2),round(float(wt[k]),4)]
        vert+=e
    return {"type":"mesh","uvs":uvs,"triangles":[int(i) for t in keep for i in t],"vertices":vert,
            "hull":hn,"edges":[x for i in range(hn) for x in (2*i,2*((i+1)%hn))],"width":w,"height":h}
def limb_mesh(n):
    par=bbn(nearest_bb(piv[n])); x0,y0,x1,y1=bbox[n]; w,h=x1-x0,y1-y0; v,keep,hn=_tri(gmask[n],bbox[n])
    sock=np.array(piv[n]); tip=np.array(far_tip(n,piv[n])); fall=np.linalg.norm(tip-sock)*0.85+1
    ow,pw=bw[n],bw[par]; uvs=[]; vert=[]
    for vx,vy in v:
        uvs+=[round((vx-x0)/w,5),round((vy-y0)/h,5)]
        t=float(np.clip(math.hypot(vx-sock[0],vy-sock[1])/fall,0,1)); wo=t*t*(3-2*t)
        vert+=[2,bidx[n],round(s_x(vx)-ow[0],2),round(s_y(vy)-ow[1],2),round(wo,4),
                 bidx[par],round(s_x(vx)-pw[0],2),round(s_y(vy)-pw[1],2),round(1-wo,4)]
    return {"type":"mesh","uvs":uvs,"triangles":[int(i) for t in keep for i in t],"vertices":vert,
            "hull":hn,"edges":[x for i in range(hn) for x in (2*i,2*((i+1)%hn))],"width":w,"height":h}

Z=["arm-L","leg-L","torso","leg-R","arm-R"]
att={"torso":torso_mesh()};
for n in ["arm-R","arm-L","leg-R","leg-L"]: att[n]=limb_mesh(n)
slots=[{"name":n,"bone":"root","attachment":n} for n in Z]
skin={"name":"default","attachments":{n:{n:att[n]} for n in Z}}

# ---- animations (gentle) ----
def kf(t,a): return {"time":round(t,3),"value":round(a,2)}
def wave(Dd,amp,ph,n=9): return [kf(Dd*i/(n-1),amp*math.sin(2*math.pi*i/(n-1)+ph)) for i in range(n)]
tailside=[bbn(i) for i in range(hip_idx+1,NB)]; headside=[bbn(i) for i in range(hip_idx-1,-1,-1)]
DI=2.6
idle={"bones":{"jaw":{"rotate":[kf(0,0),kf(DI/2,5),kf(DI,0)]},"head":{"rotate":[kf(0,0),kf(DI/2,2),kf(DI,0)]}}}
for k,nm in enumerate(tailside): idle["bones"][nm]={"rotate":wave(DI,1.6+1.1*k,-0.5*k)}
if headside: idle["bones"][headside[0]]={"rotate":[kf(0,0),kf(DI/2,1.2),kf(DI,0)]}
DR=1.5
roar={"bones":{"jaw":{"rotate":[kf(0,0),kf(0.2,-32),kf(1.1,-32),kf(DR,0)]},"head":{"rotate":[kf(0,0),kf(0.3,12),kf(1.1,11),kf(DR,0)]}}}
if headside: roar["bones"][headside[0]]={"rotate":[kf(0,0),kf(0.3,6),kf(1.1,5),kf(DR,0)]}
for k,nm in enumerate(tailside): roar["bones"][nm]={"rotate":[kf(0,0),kf(0.4,7+1.5*k),kf(0.9,-5),kf(DR,0)]}

spine={"skeleton":{"hash":"bbrig","spine":"4.2.0","x":-W//2,"y":0,"width":W,"height":H,"images":"./images/"},
  "bones":bones,"slots":slots,"skins":[skin],"animations":{"idle":idle,"roar":roar}}
def _nat(o):
    if isinstance(o,np.integer): return int(o)
    if isinstance(o,np.floating): return float(o)
    raise TypeError(o)
json.dump(spine,open(os.path.join(HERE,"mesh_dragon.json"),"w"),default=_nat)
print("backbone bones:",NB,"hip_idx",hip_idx,"| total bones",len(bones),"| wrote mesh_dragon.json")
