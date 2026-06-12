#!/usr/bin/env python3
"""Best of both: socket region (geometric, correct) defines WHERE to fill;
Gemini fills WHAT (real body scales). Green = socket band only (small, body-
surrounded) so Gemini can't turn it into a limb."""
import os
import numpy as np
import cv2
from scipy.ndimage import binary_fill_holes
from google import genai
from google.genai import types
from PIL import Image

HERE=os.path.dirname(__file__)
PAL=[(255,0,0),(255,128,0),(0,0,255),(0,255,0),(255,255,0),(255,0,255),(0,255,255),(140,0,255),(255,255,255)]
NAMES=["head","jaw","body","arm-R","arm-L","leg-R","leg-L","tail","bg"]; D=70

seg=cv2.cvtColor(cv2.imread(os.path.join(HERE,"segmap.png")),cv2.COLOR_BGR2RGB); H,W=seg.shape[:2]
orig=cv2.resize(cv2.imread(os.path.join(HERE,"reference_src.jpeg")),(W,H))
lab=np.linalg.norm(seg.reshape(-1,1,3).astype(int)-np.array(PAL)[None],axis=2).argmin(1).reshape(H,W)
m=lambda n:(lab==NAMES.index(n)).astype(np.uint8)
body=m("body"); limbs=np.clip(m("arm-R")+m("arm-L")+m("leg-R")+m("leg-L"),0,1)
gray=cv2.cvtColor(orig,cv2.COLOR_BGR2GRAY)
creature=binary_fill_holes(cv2.morphologyEx((gray<238).astype(np.uint8),cv2.MORPH_CLOSE,np.ones((7,7),np.uint8))).astype(np.uint8)
near=(limbs & cv2.dilate(body,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(2*D+1,2*D+1)))).astype(np.uint8)
target=np.clip(body+near,0,1).astype(np.uint8)
target=cv2.morphologyEx(target,cv2.MORPH_CLOSE,np.ones((11,11),np.uint8))
target=((target&creature)|body).astype(np.uint8)
band=((target==1)&(body==0)).astype(np.uint8)

canvas=np.full((H,W,3),255,np.uint8); canvas[body==1]=orig[body==1]; canvas[band==1]=(0,255,0)
ys,xs=np.where(target>0); y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1
crop=canvas[y0:y1,x0:x1]
cv2.imwrite(os.path.join(HERE,"socket_green_in.png"),crop)
flat=Image.fromarray(cv2.cvtColor(crop,cv2.COLOR_BGR2RGB))

PROMPT=(
 "This is the body/torso cutout layer of a cartoon dragon. The BRIGHT GREEN areas are small gaps "
 "where the arms and legs attach to and overlap the body. Fill ONLY the green with continuous dragon "
 "body scales and skin that match and continue the body right next to them. Do NOT draw any arm, leg, "
 "claw, foot, hand, head, jaw, eye, tail, or fin — only plain body surface. Keep every non-green pixel "
 "exactly the same. Output on a white background.")
client=genai.Client(api_key=os.environ["GEMINI_API_KEY"])
resp=client.models.generate_content(model="gemini-3-pro-image",contents=[PROMPT,flat],
     config=types.GenerateContentConfig(response_modalities=["IMAGE","TEXT"]))
for p in resp.candidates[0].content.parts:
    if p.inline_data: open(os.path.join(HERE,"socket_green_out.png"),"wb").write(p.inline_data.data); break
o=cv2.imread(os.path.join(HERE,"socket_green_out.png"))
h=max(crop.shape[0],o.shape[0]); pad=lambda i:(lambda s:cv2.resize(i,(int(i.shape[1]*s),h)))(h/i.shape[0])
cv2.imwrite(os.path.join(HERE,"socket_gemini_compare.png"),np.hstack([pad(crop),np.full((h,12,3),255,np.uint8),pad(o)]))
print("wrote socket_gemini_compare.png")
