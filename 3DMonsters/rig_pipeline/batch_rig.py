#!/usr/bin/env python3
"""
Batch-rig a folder of monster FBXs. For each model it:
  1. renders neutral-grey ortho views
  2. asks Gemini to classify the body plan -> picks a rig template
       LIMBED    -> anatomical pipeline (Gemini joint dots + skeleton)   [dragon, warthog]
       SEGMENTED -> centerline-spine pipeline (no markers)               [pill bug, slug, blob]
  3. rigs + skins, exporting <model>_rigged.fbx

Resumable: skips a model whose rigged FBX already exists, and skips any stage whose
output exists. Re-running is cheap; failures can be retried in place.

Usage:
  python3 batch_rig.py                       # every model folder under 3DMonsters/
  python3 batch_rig.py <folderA> <model.fbx> # specific folders / FBX paths
"""
import os, sys, glob, subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
BLENDER = "/Applications/Blender.app/Contents/MacOS/Blender"
ENV = "/Users/joshuawilde/MonsterBattler/Spine/.env"


def find_fbx(target):
    if target.lower().endswith(".fbx"):
        return target
    hits = glob.glob(os.path.join(target, "*.fbx"))
    return hits[0] if hits else None


def model_targets(args):
    if args:
        return list(args)
    out = []
    for d in sorted(glob.glob(os.path.join(ROOT, "*"))):
        if os.path.isdir(d) and os.path.abspath(d) != HERE and glob.glob(os.path.join(d, "*.fbx")):
            out.append(d)
    return out


def run(cmd):
    r = subprocess.run(cmd, capture_output=True, text=True)
    return r.returncode == 0, (r.stdout + r.stderr)


def stage(label, cmd, produced):
    if os.path.exists(produced):
        print(f"    skip (exists): {label}")
        return True
    ok, log = run(cmd)
    ok = ok and os.path.exists(produced)
    print(f"    {'ok' if ok else 'FAIL'}: {label}")
    if not ok:
        print("      " + "\n      ".join(log.strip().splitlines()[-6:]))
    return ok


def classify(workdir):
    """Gemini body-plan classifier -> 'LIMBED' or 'SEGMENTED' (cached to template.txt)."""
    cache = os.path.join(workdir, "template.txt")
    if os.path.exists(cache):
        return open(cache).read().strip()
    try:
        from google import genai
        from PIL import Image
        key = next(l.split("=", 1)[1].strip() for l in open(ENV) if l.startswith("GEMINI_API_KEY"))
        client = genai.Client(api_key=key)
        img = Image.open(os.path.join(workdir, "renders", "front.png"))
        prompt = (
            "Classify this creature's body plan for skeletal rigging. Reply with EXACTLY one word:\n"
            "LIMBED = a vertebrate with a distinct head and roughly four legs (optionally a tail): "
            "dog, dragon, lizard, bear, horse.\n"
            "SEGMENTED = a segmented / blobby / many-legged / legless body with no clear arm-and-leg "
            "limbs: pill bug, beetle, slug, worm, blob, snail.")
        r = client.models.generate_content(model="gemini-2.5-flash", contents=[prompt, img])
        txt = (r.text or "").upper()
        tmpl = "SEGMENTED" if "SEGMENTED" in txt else "LIMBED"
    except Exception as e:
        print(f"    classify failed ({e}); defaulting LIMBED")
        tmpl = "LIMBED"
    open(cache, "w").write(tmpl)
    return tmpl


def main():
    targets = model_targets(sys.argv[1:])
    print(f"[batch] {len(targets)} model(s)\n")
    results = []
    for t in targets:
        fbx = find_fbx(t)
        name = os.path.basename(os.path.normpath(t if os.path.isdir(t) else os.path.dirname(t)))
        wd = os.path.join(HERE, name)
        print(f"[{name}]")
        if not fbx:
            print("    FAIL: no .fbx"); results.append((name, None, False)); continue
        os.makedirs(wd, exist_ok=True)
        rigged = os.path.join(wd, os.path.splitext(os.path.basename(fbx))[0] + "_rigged.fbx")
        if os.path.exists(rigged):
            print("    skip (already rigged)"); results.append((name, "-", True)); print(); continue

        ok = stage("render", [BLENDER, "--background", "--python", f"{HERE}/01_render.py", "--", fbx, wd],
                   os.path.join(wd, "renders", "frame.json"))
        tmpl = classify(wd) if ok else "LIMBED"
        print(f"    template: {tmpl}")

        if tmpl == "SEGMENTED":
            ok = ok and stage("rig (axis spine)",
                              [BLENDER, "--background", "--python", f"{HERE}/02_rig_axis.py", "--", fbx, wd], rigged)
        else:
            ok = ok and stage("gemini markers", ["python3", f"{HERE}/gemini_rig_markers.py", wd],
                              os.path.join(wd, "markers.json"))
            ok = ok and stage("rig + skin",
                              [BLENDER, "--background", "--python", f"{HERE}/02_rig.py", "--", fbx, wd], rigged)
        results.append((name, tmpl, ok))
        print()

    print("[summary]")
    for name, tmpl, ok in results:
        print(f"  {'PASS' if ok else 'FAIL'}  [{tmpl}]  {name}")


if __name__ == "__main__":
    main()
