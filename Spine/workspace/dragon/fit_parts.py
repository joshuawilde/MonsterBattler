#!/usr/bin/env python3
"""Analysis-by-synthesis part fitter v2 — matching pursuit.

Place parts big->small. Score each candidate (scale,rot,pos) by:
  + appearance: low color SSD vs reference under the part mask
  + coverage:  fraction of the part mask landing on STILL-UNEXPLAINED reference foreground
  - waste:     part mask landing on white background
After placing a part, subtract its footprint from the 'remaining foreground' so the
next part must explain new pixels (prevents the pile-up collapse).
"""
import os, json, glob
import cv2
import numpy as np

HERE = os.path.dirname(__file__)
REF  = os.path.join(HERE, "neutral_ref.png")
PARTS_DIR = os.path.join(HERE, "parts_v3")

SCALES = [0.45, 0.55, 0.65, 0.75, 0.85, 0.95, 1.05, 1.2]
ROTS   = [-40, -25, -15, -8, 0, 8, 15, 25, 40]

def rgb_on_white(path):
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    if im.ndim == 2:
        im = cv2.cvtColor(im, cv2.COLOR_GRAY2BGR)
    if im.shape[2] == 3:
        return im[:, :, :3].copy()
    a = im[:, :, 3:4].astype(np.float32) / 255.0
    return (im[:, :, :3].astype(np.float32) * a + 255.0 * (1 - a)).astype(np.uint8)

def alpha(path):
    return cv2.imread(path, cv2.IMREAD_UNCHANGED)[:, :, 3].astype(np.uint8)

def warp(bgr, a, scale, rot):
    h, w = a.shape
    M = cv2.getRotationMatrix2D((w/2, h/2), rot, scale)
    cs, sn = abs(M[0,0]), abs(M[0,1])
    nw, nh = int(w*cs + h*sn), int(h*cs + w*sn)
    M[0,2] += nw/2 - w/2; M[1,2] += nh/2 - h/2
    wb = cv2.warpAffine(bgr, M, (nw, nh), flags=cv2.INTER_AREA, borderValue=(255,255,255))
    wa = cv2.warpAffine(a,   M, (nw, nh), flags=cv2.INTER_AREA, borderValue=0)
    return wb, wa

def main():
    ref = rgb_on_white(REF).astype(np.float32)
    RH, RW = ref.shape[:2]
    ref_fg = (np.abs(ref - 255).sum(2) > 40).astype(np.float32)   # non-white
    remaining = ref_fg.copy()

    parts = sorted(glob.glob(os.path.join(PARTS_DIR, "part_*.png")))
    meta = []
    for p in parts:
        a = alpha(p); meta.append((p, int((a > 40).sum())))
    meta.sort(key=lambda x: -x[1])   # big parts first

    layout, order = {}, []
    for p, area in meta:
        name = os.path.splitext(os.path.basename(p))[0]
        bgr0, a0 = rgb_on_white(p), alpha(p)
        best = None
        for s in SCALES:
            for r in ROTS:
                wb, wa = warp(bgr0, a0, s, r)
                th, tw = wa.shape
                if th >= RH or tw >= RW: continue
                m = (wa > 40).astype(np.float32)
                marea = m.sum()
                if marea < 250: continue
                # appearance: SSD of color under mask (lower better) via matchTemplate
                ssd = cv2.matchTemplate(ref, wb.astype(np.float32), cv2.TM_SQDIFF, mask=m)
                ssd = np.nan_to_num(ssd, nan=1e18, posinf=1e18) / (marea*3*255.0*255.0)
                app = 1.0 - np.clip(ssd, 0, 1)               # 0..1, higher better
                # coverage of remaining fg, and background waste, via correlation of mask
                cov = cv2.matchTemplate(remaining, m, cv2.TM_CCORR) / marea
                bg  = cv2.matchTemplate(1.0-ref_fg, m, cv2.TM_CCORR) / marea
                score = 0.5*app[:cov.shape[0],:cov.shape[1]] + 0.8*cov - 0.6*bg
                _, mx, _, loc = cv2.minMaxLoc(score)
                if best is None or mx > best["v"]:
                    best = {"v": float(mx), "scale": s, "rot": r,
                            "x": loc[0]+tw/2, "y": loc[1]+th/2, "tw": tw, "th": th,
                            "footprint": (m, loc, tw, th)}
        if not best: continue
        m, loc, tw, th = best.pop("footprint")
        # subtract footprint from remaining
        x0, y0 = loc; x1, y1 = min(x0+tw, RW), min(y0+th, RH)
        remaining[y0:y1, x0:x1] = np.clip(remaining[y0:y1, x0:x1] - m[:y1-y0, :x1-x0], 0, 1)
        layout[name] = {"x": best["x"], "y": best["y"], "scale": best["scale"], "rot": best["rot"]}
        order.append(name)
        print(f"  {name}: v={best['v']:.3f} scale={best['scale']} rot={best['rot']} @({best['x']:.0f},{best['y']:.0f})")

    json.dump({"order": order, "parts": layout}, open(os.path.join(HERE, "fit_layout.json"), "w"), indent=2)

    # composite in placement order (big/back first)
    canvas = np.full((RH, RW, 3), 255, np.uint8)
    for name in order:
        b = layout[name]
        wb, wa = warp(rgb_on_white(os.path.join(PARTS_DIR, name+".png")),
                      alpha(os.path.join(PARTS_DIR, name+".png")), b["scale"], b["rot"])
        x0, y0 = int(b["x"]-wb.shape[1]/2), int(b["y"]-wb.shape[0]/2)
        ys, xs = max(0,y0), max(0,x0); ye, xe = min(RH,y0+wb.shape[0]), min(RW,x0+wb.shape[1])
        af = (wa[ys-y0:ye-y0, xs-x0:xe-x0].astype(np.float32)/255.0)[...,None]
        canvas[ys:ye, xs:xe] = (wb[ys-y0:ye-y0, xs-x0:xe-x0]*af + canvas[ys:ye, xs:xe]*(1-af)).astype(np.uint8)
    cv2.imwrite(os.path.join(HERE, "fit_compose.png"), np.hstack([ref.astype(np.uint8), canvas]))
    print("wrote fit_compose.png (ref | fitted)")

if __name__ == "__main__":
    main()
