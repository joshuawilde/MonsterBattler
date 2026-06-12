#!/usr/bin/env python3
"""Mesh-deform rig v2: BODY and TAIL are skinned meshes on bone chains; head/jaw/
limbs are rigid (grown for seam overlap) parented onto the body spine chain so
they move cohesively. Writes Spine 4.2 JSON (rotate uses 'value')."""
import os, json, shutil, math
import numpy as np, cv2
from collections import deque
from skimage.morphology import skeletonize
from scipy.interpolate import splprep, splev
from scipy.spatial import Delaunay
from scipy.ndimage import distance_transform_edt, binary_fill_holes

HERE=os.path.dirname(__file__)
PAL=[(255,0,0),(255,128,0),(0,0,255),(0,255,0),(255,255,0),(255,0,255),(0,255,255),(140,0,255),(255,255,255)]
NAMES=["head","jaw","body","arm-R","arm-L","leg-R","leg-L","tail","bg"]; D=70
seg=cv2.cvtColor(cv2.imread(os.path.join(HERE,"segmap.png")),cv2.COLOR_BGR2RGB); H,W=seg.shape[:2]
orig=cv2.resize(cv2.imread(os.path.join(HERE,"reference_src.jpeg")),(W,H))
lab=np.linalg.norm(seg.reshape(-1,1,3).astype(int)-np.array(PAL)[None],axis=2).argmin(1).reshape(H,W)
m=lambda n:(lab==NAMES.index(n)).astype(np.uint8)
creature=(lab!=NAMES.index("bg")).astype(np.uint8)   # clean segmap silhouette (NOT a brightness threshold -> no bg)
creature=binary_fill_holes(cv2.morphologyEx(creature,cv2.MORPH_CLOSE,np.ones((7,7),np.uint8))).astype(np.uint8)
def cleanmask(mk):  # largest connected component -> drops stray mislabeled pixels
    mk=mk.astype(np.uint8); n,l,st,_=cv2.connectedComponentsWithStats(mk,8)
    if n<=1: return mk
    return (l==1+int(np.argmax(st[1:,cv2.CC_STAT_AREA]))).astype(np.uint8)
def s_x(ix): return float(ix-W/2)
def s_y(iy): return float(H-iy)

# ---- body (optionally socket-filled) ----
FILL_BODY=True    # True = fill behind limbs (socket fill); False = raw cut body
GROW=0            # px to expand each part so seams overlap (0 = none; ~16 hides rotation gaps)
body=m("body"); limbs=np.clip(sum(m(n) for n in ["arm-R","arm-L","leg-R","leg-L"]),0,1)
if FILL_BODY:
    near=(limbs & cv2.dilate(body,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(2*D+1,2*D+1)))).astype(np.uint8)
    btar=cv2.morphologyEx(np.clip(body+near,0,1).astype(np.uint8),cv2.MORPH_CLOSE,np.ones((11,11),np.uint8))
    btar=((btar&creature)|body).astype(np.uint8); bband=((btar==1)&(body==0)).astype(np.uint8)
    src=cv2.erode(body,np.ones((13,13),np.uint8)); idx=distance_transform_edt((src if src.sum()>100 else body)==0,return_indices=True)[1]
    bodyfill=orig.copy(); bodyfill[bband==1]=cv2.medianBlur(orig[idx[0],idx[1]],21)[bband==1]
else:
    btar=body.copy(); bodyfill=orig.copy()
bys,bxs=np.where(btar>0); bcx,bcy=bxs.mean(),bys.mean()
hys,hxs=np.where(m("head")>0); hcx,hcy=hxs.mean(),hys.mean()
def nearest(mask,tx,ty): ys,xs=np.where(mask>0); i=((xs-tx)**2+(ys-ty)**2).argmin(); return (float(xs[i]),float(ys[i]))
JFILE=os.path.join(HERE,"joints_extracted.json")
GJ=json.load(open(JFILE)) if os.path.exists(JFILE) else {}
def gpiv(name,fallback): return tuple(GJ[name]) if name in GJ else fallback   # Gemini-drawn joint or fallback

RIG=os.path.join(HERE,"rig_mesh"); shutil.rmtree(RIG,ignore_errors=True); os.makedirs(RIG)
def save_part(name,mask,img,grow=GROW):
    if grow: mask=(cv2.dilate(mask,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(2*grow+1,2*grow+1)))&creature).astype(np.uint8)
    ys,xs=np.where(mask>0); y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
    cv2.imwrite(os.path.join(RIG,name+".png"),np.dstack([img,(mask*255).astype(np.uint8)])[y0:y1,x0:x1])
    return (x0,y0,x1,y1),mask
# TORSO = whole creature MINUS the 4 limbs, socket-filled -> ONE continuous mesh
# (body+head+neck+jaw+tail+spikes). No internal seams = no cracking. Limbs stay separate.
nonlimb=((creature==1)&(limbs==0)).astype(np.uint8)
tnear=(limbs & cv2.dilate(nonlimb,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(2*D+1,2*D+1)))).astype(np.uint8)
ttar=cv2.morphologyEx(np.clip(nonlimb+tnear,0,1).astype(np.uint8),cv2.MORPH_CLOSE,np.ones((11,11),np.uint8))
ttar=((ttar&creature)|nonlimb).astype(np.uint8); tband=((ttar==1)&(nonlimb==0)).astype(np.uint8)
tsrc=cv2.erode(nonlimb,np.ones((13,13),np.uint8)); tidx=distance_transform_edt((tsrc if tsrc.sum()>100 else nonlimb)==0,return_indices=True)[1]
torso_img=orig.copy(); torso_img[tband==1]=cv2.medianBlur(orig[tidx[0],tidx[1]],21)[tband==1]
bbox={}; gmask={}
bbox["torso"],gmask["torso"]=save_part("torso",ttar,torso_img,grow=GROW)
for n in ["arm-R","arm-L","leg-R","leg-L"]:
    bbox[n],gmask[n]=save_part(n,m(n),orig,grow=GROW)
def center(n): x0,y0,x1,y1=bbox[n]; return ((x0+x1)/2,(y0+y1)/2)

# ---- centerlines ----
def neighbors(p,sk):
    y,x=p
    for dy in(-1,0,1):
        for dx in(-1,0,1):
            if dy or dx:
                ny,nx=y+dy,x+dx
                if 0<=ny<sk.shape[0] and 0<=nx<sk.shape[1] and sk[ny,nx]: yield(ny,nx)
def longest(sk):
    pts=np.argwhere(sk)
    def bfs(s):
        seen={s:None};q=deque([s]);last=s
        while q:
            c=q.popleft();last=c
            for nb in neighbors(c,sk):
                if nb not in seen: seen[nb]=c;q.append(nb)
        return last,seen
    a,_=bfs(tuple(pts[0]));b,seen=bfs(a);path=[];c=b
    while c is not None: path.append(c);c=seen[c]
    return path
def tail_joints(nb):
    sk=skeletonize(cleanmask(m("tail"))>0); cl=np.array(longest(sk),float)
    if (cl[0,1]-bcx)**2+(cl[0,0]-bcy)**2>(cl[-1,1]-bcx)**2+(cl[-1,0]-bcy)**2: cl=cl[::-1]
    tck,u=splprep([cl[:,1],cl[:,0]],s=len(cl)*6,k=3); bx,by=splev(np.linspace(0,1,nb),tck)
    return np.stack([bx,by],1)
hpiv=gpiv("head",nearest(m("head"),bcx,bcy))         # Gemini neck dot (fallback heuristic)
lr=nearest(m("leg-R"),bcx,bcy); ll=nearest(m("leg-L"),bcx,bcy)
hip_img=nearest(btar,(lr[0]+ll[0])/2,(lr[1]+ll[1])/2) # hip = body point between the legs
def body_joints(nb):  # spine along the torso's PCA long axis, hip(base)->neck
    ys,xs=np.where(btar>0); P=np.stack([xs,ys],1).astype(float); mu=P.mean(0)
    _,_,vt=np.linalg.svd(P-mu,full_matrices=False); axis=vt[0]
    proj=(P-mu)@axis; lo,hi=np.percentile(proj,[3,97]); band=(hi-lo)/(2*nb)+10
    pts=[]
    for t in np.linspace(lo,hi,nb):
        sel=np.abs(proj-t)<band; pts.append(P[sel].mean(0) if sel.any() else mu+axis*t)
    pts=np.array(pts)
    if (pts[0,0]-hip_img[0])**2+(pts[0,1]-hip_img[1])**2 > (pts[-1,0]-hip_img[0])**2+(pts[-1,1]-hip_img[1])**2:
        pts=pts[::-1]
    return pts

NBODY=4; NTAIL=6
bj=body_joints(NBODY)   # hip..neck (image xy)
tj=tail_joints(NTAIL)   # base..tip

# ---- bones ----
bones=[{"name":"root"}]
def addbone(name,parent,wxy,pwxy):
    bones.append({"name":name,"parent":parent,"x":round(wxy[0]-pwxy[0],1),"y":round(wxy[1]-pwxy[1],1)})
bw={"root":(0,0)}
bodychain=["hip","spine","chest","neck"]
for i,nm in enumerate(bodychain):
    wx,wy=s_x(bj[i,0]),s_y(bj[i,1]); par="root" if i==0 else bodychain[i-1]
    addbone(nm,par,(wx,wy),bw[par]); bw[nm]=(wx,wy)
# head at its neck-pivot (== top body joint), parented to body 'neck'
wx,wy=s_x(hpiv[0]),s_y(hpiv[1]); addbone("head","neck",(wx,wy),bw["neck"]); bw["head"]=(wx,wy)
jpiv=gpiv("jaw",nearest(m("jaw"),hcx,hcy)); wx,wy=s_x(jpiv[0]),s_y(jpiv[1]); addbone("jaw","head",(wx,wy),bw["head"]); bw["jaw"]=(wx,wy)
LIMBPAR={"arm-R":"chest","arm-L":"chest","leg-R":"hip","leg-L":"hip"}
piv={"head":hpiv,"jaw":jpiv}
for n,par in LIMBPAR.items():
    p=gpiv(n,nearest(m(n),bcx,bcy)); piv[n]=p; wx,wy=s_x(p[0]),s_y(p[1]); addbone(n,par,(wx,wy),bw[par]); bw[n]=(wx,wy)
tailbones=[]
for i in range(NTAIL):
    nm="tail%d"%(i+1); wx,wy=s_x(tj[i,0]),s_y(tj[i,1]); par="hip" if i==0 else tailbones[-1]
    addbone(nm,par,(wx,wy),bw[par]); bw[nm]=(wx,wy); tailbones.append(nm)
bidx={b["name"]:i for i,b in enumerate(bones)}

# ---- skinned mesh builder ----
def build_mesh(mask, bb, jts_img, bone_names):
    x0,y0,x1,y1=bb; w,h=x1-x0,y1-y0
    cnts,_=cv2.findContours(mask.astype(np.uint8),cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
    cnt=max(cnts,key=cv2.contourArea); approx=cv2.approxPolyDP(cnt,3.0,True).reshape(-1,2).astype(float)
    gx,gy=np.meshgrid(np.arange(x0+10,x1,28),np.arange(y0+10,y1,28))
    inside=[p for p in np.stack([gx.ravel(),gy.ravel()],1) if mask[int(min(p[1],H-1)),int(min(p[0],W-1))]>0]
    verts=np.vstack([approx]+([np.array(inside)] if inside else []))
    hull_n=len(approx); tri=Delaunay(verts); keep=[]
    for t in tri.simplices:
        c=verts[t].mean(0)
        if mask[int(min(c[1],H-1)),int(min(c[0],W-1))]>0: keep.append(t)
    keep=np.array(keep)
    uvs=[]; vertices=[]
    jw=[ (s_x(jx),s_y(jy)) for jx,jy in jts_img ]
    for vx,vy in verts:
        uvs+=[round((vx-x0)/w,5),round((vy-y0)/h,5)]
        d=np.array([(vx-jts_img[i][0])**2+(vy-jts_img[i][1])**2 for i in range(len(jts_img))])
        o=np.argsort(d)[:2]; wt=1.0/(np.sqrt(d[o])+1e-3); wt=wt/wt.sum()
        e=[2]
        for k,bi in enumerate(o):
            e+=[bidx[bone_names[bi]],round(s_x(vx)-jw[bi][0],2),round(s_y(vy)-jw[bi][1],2),round(float(wt[k]),4)]
        vertices+=e
    triangles=[int(i) for t in keep for i in t]
    edges=[];
    for i in range(hull_n): edges+=[2*i,2*((i+1)%hull_n)]
    return {"type":"mesh","uvs":uvs,"triangles":triangles,"vertices":vertices,"hull":hull_n,"edges":edges,"width":w,"height":h}

def _triangulate(mask,bb):
    x0,y0,x1,y1=bb
    cnts,_=cv2.findContours(mask.astype(np.uint8),cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
    cnt=max(cnts,key=cv2.contourArea); approx=cv2.approxPolyDP(cnt,3.0,True).reshape(-1,2).astype(float)
    gx,gy=np.meshgrid(np.arange(x0+10,x1,24),np.arange(y0+10,y1,24))
    inside=[p for p in np.stack([gx.ravel(),gy.ravel()],1) if mask[int(min(p[1],H-1)),int(min(p[0],W-1))]>0]
    verts=np.vstack([approx]+([np.array(inside)] if inside else []))
    hull_n=len(approx); tri=Delaunay(verts); keep=[t for t in tri.simplices
        if mask[int(min(verts[t].mean(0)[1],H-1)),int(min(verts[t].mean(0)[0],W-1))]>0]
    return verts,np.array(keep),hull_n

def build_part_mesh(mask, bb, own, parent, joint_img):
    # CROSS-WEIGHT: vertices near the joint ride the PARENT (body) bone (welds the
    # seam to the body), far vertices ride the part's OWN bone. Smooth blend between.
    x0,y0,x1,y1=bb; w,h=x1-x0,y1-y0
    verts,keep,hull_n=_triangulate(mask,bb)
    ow,pw=bw[own],bw[parent]; oi,pi=bidx[own],bidx[parent]
    far=max(np.hypot(v[0]-joint_img[0],v[1]-joint_img[1]) for v in verts); fall=far*0.8+1
    uvs=[]; vertices=[]
    for vx,vy in verts:
        uvs+=[round((vx-x0)/w,5),round((vy-y0)/h,5)]
        t=float(np.clip(np.hypot(vx-joint_img[0],vy-joint_img[1])/fall,0,1)); wo=t*t*(3-2*t); wp=1-wo
        vertices+=[2, oi, round(s_x(vx)-ow[0],2), round(s_y(vy)-ow[1],2), round(wo,4),
                      pi, round(s_x(vx)-pw[0],2), round(s_y(vy)-pw[1],2), round(wp,4)]
    tris=[int(i) for t in keep for i in t]; edges=[x for i in range(hull_n) for x in (2*i,2*((i+1)%hull_n))]
    return {"type":"mesh","uvs":uvs,"triangles":tris,"vertices":vertices,"hull":hull_n,"edges":edges,"width":w,"height":h}

# ---- UNIFIED skinning: every vertex (any part) weighted to nearest bone SEGMENTS
# using ONE rule -> coincident seam vertices on adjacent parts get identical weights -> weld ----
def far_tip(mask_name,jt):
    ys,xs=np.where(cleanmask(m(mask_name))>0); i=((xs-jt[0])**2+(ys-jt[1])**2).argmax(); return np.array([float(xs[i]),float(ys[i])])
SEG={}
for i,nm in enumerate(bodychain):
    SEG[nm]=(np.array(bj[i],float), np.array(bj[i+1] if i+1<len(bj) else hpiv,float))
SEG["neck"]=(np.array(bj[3],float), np.array(hpiv,float))
SEG["head"]=(np.array(hpiv,float), far_tip("head",hpiv))
SEG["jaw"]=(np.array(jpiv,float), far_tip("jaw",jpiv))
for n in LIMBPAR: SEG[n]=(np.array(piv[n],float), far_tip(n,piv[n]))
for i in range(NTAIL):
    SEG[tailbones[i]]=(np.array(tj[i],float), np.array(tj[i+1] if i+1<NTAIL else far_tip("tail",tj[i]),float))
SEGNAMES=list(SEG); SEGS=[SEG[n] for n in SEGNAMES]
def dseg(px,py,a,b):
    abx,aby=b[0]-a[0],b[1]-a[1]; den=abx*abx+aby*aby+1e-9
    t=max(0,min(1,((px-a[0])*abx+(py-a[1])*aby)/den)); return math.hypot(px-(a[0]+t*abx),py-(a[1]+t*aby))
ALLOWED={"body":bodychain,"head":["neck","head"],"jaw":["head","jaw"],
         "arm-R":["chest","arm-R"],"arm-L":["chest","arm-L"],
         "leg-R":["hip","leg-R"],"leg-L":["hip","leg-L"],"tail":["hip"]+tailbones}
# WELD zones: any vertex (in ANY part) within radius of a joint is forced 100% to the
# SHARED bone there -> both sides of the seam ride one identical bone -> cannot crack.
WELDS=[(np.array(hpiv),"neck",60),(np.array(jpiv),"head",48),
       (np.array(piv["arm-R"]),"chest",55),(np.array(piv["arm-L"]),"chest",55),
       (np.array(piv["leg-R"]),"hip",55),(np.array(piv["leg-L"]),"hip",55),
       (np.array(tj[0]),"hip",55)]
def build_unified_mesh(mask,bb,allowed,welds):
    x0,y0,x1,y1=bb; w,h=x1-x0,y1-y0
    verts,keep,hull_n=_triangulate(mask,bb)
    idxs=[i for i,n in enumerate(SEGNAMES) if n in allowed]
    uvs=[]; vertices=[]
    for vx,vy in verts:
        uvs+=[round((vx-x0)/w,5),round((vy-y0)/h,5)]
        fb=None; fd=1e9
        for wp,bn,r in welds:
            dd=math.hypot(vx-wp[0],vy-wp[1])
            if dd<r and dd<fd: fb=bn; fd=dd
        if fb is not None:
            bx,by=bw[fb]; vertices+=[1,bidx[fb],round(s_x(vx)-bx,2),round(s_y(vy)-by,2),1.0]; continue
        d=np.array([dseg(vx,vy,*SEGS[i]) for i in idxs]); o=np.argsort(d)[:2]
        wt=1.0/(d[o]+2.0); wt=wt/wt.sum(); e=[2]
        for k,oi in enumerate(o):
            nm=SEGNAMES[idxs[oi]]; bx,by=bw[nm]
            e+=[bidx[nm],round(s_x(vx)-bx,2),round(s_y(vy)-by,2),round(float(wt[k]),4)]
        vertices+=e
    tris=[int(i) for t in keep for i in t]; edges=[x for i in range(hull_n) for x in (2*i,2*((i+1)%hull_n))]
    return {"type":"mesh","uvs":uvs,"triangles":tris,"vertices":vertices,"hull":hull_n,"edges":edges,"width":w,"height":h}

# torso = one mesh skinned to ALL non-limb bones (no internal welds); limbs weld at their socket
TORSO_BONES=bodychain+["head","jaw"]+tailbones
PARTCFG={
 "torso":(TORSO_BONES, []),
 "arm-R":(["chest","arm-R"], [(piv["arm-R"],"chest",55)]),
 "arm-L":(["chest","arm-L"], [(piv["arm-L"],"chest",55)]),
 "leg-R":(["hip","leg-R"],   [(piv["leg-R"],"hip",55)]),
 "leg-L":(["hip","leg-L"],   [(piv["leg-L"],"hip",55)]),
}
Z=["arm-L","leg-L","torso","leg-R","arm-R"]   # 2 limbs behind torso, 2 in front
meshes={n:build_unified_mesh(gmask[n],bbox[n],*PARTCFG[n]) for n in Z}
slots=[{"name":n,"bone":"root","attachment":n} for n in Z]
skin={"name":"default","attachments":{n:{n:meshes[n]} for n in Z}}

# ---- animations (rotate uses 'value') ----
def kf(t,a): return {"time":round(t,3),"value":round(a,2)}
def kft(t,x,y): return {"time":round(t,3),"x":round(x,2),"y":round(y,2)}
def wave(D,amp,phase,n=9): return [kf(D*i/(n-1), amp*math.sin(2*math.pi*i/(n-1)+phase)) for i in range(n)]
DI=2.6
# GENTLE + isolated: no spine-chain compounding. jaw breathes, tail tip drifts a little.
idle={"bones":{
  "jaw":{"rotate":[kf(0,0),kf(DI/2,5),kf(DI,0)]},
  "neck":{"rotate":[kf(0,0),kf(DI/2,1.5),kf(DI,0)]},
}}
for i,nm in enumerate(tailbones[2:],2): idle["bones"][nm]={"rotate":wave(DI,1.5*i,-0.4*i)}  # only the far tail, small
DR=1.5
roar={"bones":{
  "jaw":{"rotate":[kf(0,0),kf(0.2,-30),kf(1.1,-30),kf(DR,0)]},
  "head":{"rotate":[kf(0,0),kf(0.3,10),kf(1.1,9),kf(DR,0)]},
  "neck":{"rotate":[kf(0,0),kf(0.3,8),kf(1.1,7),kf(DR,0)]},
}}
for i,nm in enumerate(tailbones[2:],2): roar["bones"][nm]={"rotate":[kf(0,0),kf(0.4,6+2*i),kf(0.9,-5),kf(DR,0)]}

spine={"skeleton":{"hash":"meshrig2","spine":"4.2.0","x":-W//2,"y":0,"width":W,"height":H,"images":"./images/"},
  "bones":bones,"slots":slots,"skins":[skin],"animations":{"idle":idle,"roar":roar}}
def _nat(o):
    if isinstance(o,np.integer): return int(o)
    if isinstance(o,np.floating): return float(o)
    if isinstance(o,np.ndarray): return o.tolist()
    raise TypeError(o)
json.dump(spine,open(os.path.join(HERE,"mesh_dragon.json"),"w"),default=_nat)
print("bones:",len(bones),"| body+tail meshes | wrote mesh_dragon.json")
