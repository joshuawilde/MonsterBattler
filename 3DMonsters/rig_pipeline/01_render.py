"""
Blender headless: import the Tripo dragon, frame it with axis-aligned ortho
cameras, and render front / right / top views for joint-marker placement.

Run:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python 01_render.py

Writes renders/front.png, renders/right.png, renders/top.png and prints the
framing constants (center, ortho_scale) that 02_rig.py must reuse to map
normalized image coords back to 3D.
"""
import bpy
import mathutils
import json
import os
import sys

_argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
FBX = _argv[0] if len(_argv) > 0 else "/Users/joshuawilde/MonsterBattler/3DMonsters/dragon+3d+model/tripo_test_1.fbx"
WORKDIR = _argv[1] if len(_argv) > 1 else "/Users/joshuawilde/MonsterBattler/3DMonsters/rig_pipeline"
OUT = os.path.join(WORKDIR, "renders")
os.makedirs(OUT, exist_ok=True)
RES = 1024
MARGIN = 1.12

# --- clean scene ---
bpy.ops.wm.read_factory_settings(use_empty=True)

# --- import ---
bpy.ops.import_scene.fbx(filepath=FBX)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
print(f"[info] imported {len(meshes)} mesh object(s): {[o.name for o in meshes]}")

# --- world-space bounding box over all mesh verts ---
mn = mathutils.Vector((1e18, 1e18, 1e18))
mx = mathutils.Vector((-1e18, -1e18, -1e18))
vcount = 0
for o in meshes:
    vcount += len(o.data.vertices)
    for c in o.bound_box:
        w = o.matrix_world @ mathutils.Vector(c)
        for i in range(3):
            mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
center = (mn + mx) / 2.0
size = mx - mn
ortho = max(size.x, size.y, size.z) * MARGIN
dist = max(size.x, size.y, size.z) * 3.0
print(f"[info] total verts: {vcount}")
print(f"[info] bbox min {tuple(round(v,4) for v in mn)} max {tuple(round(v,4) for v in mx)}")
print(f"[frame] center={tuple(round(v,5) for v in center)} ortho_scale={round(ortho,5)}")

# save framing for the rig step
with open(os.path.join(OUT, "frame.json"), "w") as f:
    json.dump({"center": list(center), "ortho_scale": ortho, "res": RES}, f)

# --- render settings: clean, flat, silhouette-friendly ---
scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = RES
scene.render.resolution_y = RES
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new("w")
scene.world.color = (0.18, 0.18, 0.2)
sh = scene.display.shading
sh.light = "STUDIO"
# Neutral grey (no albedo) so the model's own texture colors can't be mistaken for
# Gemini's marker dots. Cavity + outline keep surface form readable for joint placement.
sh.color_type = "SINGLE"
sh.single_color = (0.62, 0.62, 0.64)
sh.show_cavity = True
sh.show_object_outline = True


def make_cam(name, eye):
    cam = bpy.data.cameras.new(name)
    cam.type = "ORTHO"
    cam.ortho_scale = ortho
    obj = bpy.data.objects.new(name, cam)
    scene.collection.objects.link(obj)
    obj.location = center + eye * dist
    look = (center - obj.location).normalized()
    obj.rotation_euler = look.to_track_quat("-Z", "Y").to_euler()
    return obj


views = {
    "front": mathutils.Vector((0, -1, 0)),   # looks +Y : image axes = world X (right), Z (up)
    "right": mathutils.Vector((1, 0, 0)),    # looks -X : image axes = world Y (right), Z (up)
    "top":   mathutils.Vector((0, 0, 1)),    # looks -Z : image axes = world X (right), Y (up)
}
for vname, eye in views.items():
    cam = make_cam(vname, eye)
    scene.camera = cam
    scene.render.filepath = os.path.join(OUT, vname + ".png")
    bpy.ops.render.render(write_still=True)
    print(f"[render] {vname} -> {scene.render.filepath}")

print("[done] renders complete")
