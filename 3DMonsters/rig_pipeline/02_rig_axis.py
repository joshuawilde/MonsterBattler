"""
Segmented / blob body template (pill bug, slug, amoeba...). No anatomical joints,
so we derive a spine that follows the body's centerline: slice the mesh along its
longest axis and take each slice's centroid. Legs/appendages auto-skin to the
nearest body segment and follow along. Names match ProceduralIdle (pelvis/spine/head).

Run:  Blender --background --python 02_rig_axis.py -- <fbx> <workdir>
"""
import bpy, mathutils, json, os, sys, math

_argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
FBX = _argv[0]
BASE = _argv[1]
RENDERS = os.path.join(BASE, "renders")
OUT_FBX = os.path.join(BASE, os.path.splitext(os.path.basename(FBX))[0] + "_rigged.fbx")
V = mathutils.Vector
SEGMENTS = 5                      # spine bones (pelvis + spines + head)
frame = json.load(open(os.path.join(RENDERS, "frame.json")))
cx, cy, cz = frame["center"]; S = frame["ortho_scale"]

# --- import + strip any pre-existing rig + join ---
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
for o in bpy.context.scene.objects:
    o.select_set(o in meshes)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
mesh = bpy.context.view_layer.objects.active
mesh.vertex_groups.clear()
for o in list(bpy.context.scene.objects):
    if o.type == "ARMATURE":
        bpy.data.objects.remove(o, do_unlink=True)
print(f"[info] mesh verts={len(mesh.data.vertices)}")

# --- longest axis, then centroid per slice along it ---
mw = mesh.matrix_world
wco = [mw @ v.co for v in mesh.data.vertices]
mn = V((min(p[i] for p in wco) for i in range(3)))
mx = V((max(p[i] for p in wco) for i in range(3)))
axis = max(range(3), key=lambda i: mx[i] - mn[i])     # body-length axis
lo, hi = mn[axis], mx[axis]
NP = SEGMENTS + 1
pts = []
for k in range(NP):
    a = lo + (hi - lo) * k / SEGMENTS
    b = lo + (hi - lo) * (k + 1) / SEGMENTS if k < SEGMENTS else hi + 1e-6
    band = [p for p in wco if a <= p[axis] < b] or [p for p in wco if abs(p[axis] - a) < (hi - lo) / SEGMENTS]
    c = V((sum(p[j] for p in band) / len(band) for j in range(3)))
    c[axis] = a if k == 0 else (hi if k == NP - 1 else (a + b) / 2)
    pts.append(c)
# order rear->front so the +axis (head) end is last
if pts[0][axis] > pts[-1][axis]:
    pts.reverse()
print(f"[info] axis={'XYZ'[axis]} spine points={len(pts)}")

BONES = ["pelvis", "spine01", "spine02", "spine03", "head"][:SEGMENTS]

# --- armature ---
arm = bpy.data.objects.new("Rig", bpy.data.armatures.new("Rig"))
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
eb = arm.data.edit_bones
prev = None
for i, name in enumerate(BONES):
    bone = eb.new(name)
    bone.head = pts[i]
    bone.tail = pts[i + 1]
    if prev:
        bone.parent = prev; bone.use_connect = True
    prev = bone
bpy.ops.object.mode_set(mode="OBJECT")
print(f"[info] bones: {len(arm.data.bones)}")

# --- skin + dead-bone fallback ---
mesh.select_set(True); arm.select_set(True)
bpy.context.view_layer.objects.active = arm
try:
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    print("[skin] bone-heat OK")
except RuntimeError:
    bpy.ops.object.parent_set(type="ARMATURE_ENVELOPE")
counts = {vg.index: 0 for vg in mesh.vertex_groups}
for v in mesh.data.vertices:
    for g in v.groups:
        if g.weight > 0.1 and g.group in counts:
            counts[g.group] += 1
idx2 = {vg.index: vg.name for vg in mesh.vertex_groups}
dead = [idx2[i] for i in counts if counts[i] == 0]
if dead:
    aw = arm.matrix_world
    segs = {b.name: (aw @ b.head_local, aw @ b.tail_local) for b in arm.data.bones}

    def sd(p, a, b):
        ab = b - a; d = ab.length_squared
        t = max(0.0, min(1.0, (p - a).dot(ab) / d)) if d > 1e-9 else 0.0
        return (p - (a + ab * t)).length
    vgs = {n: (mesh.vertex_groups.get(n) or mesh.vertex_groups.new(name=n)) for n in dead}
    for n in dead:
        a, b = segs[n]
        for i in sorted(range(len(wco)), key=lambda i: sd(wco[i], a, b))[:60]:
            vgs[n].add([i], 1.0, "REPLACE")
    bpy.context.view_layer.objects.active = mesh
    for o in bpy.context.scene.objects:
        o.select_set(o is mesh)
    bpy.ops.object.vertex_group_normalize_all()
print("[weights] " + ", ".join(f"{idx2[i]}={counts[i]}" for i in sorted(counts)))

# --- export ---
for o in bpy.context.scene.objects:
    o.select_set(o.type in {"MESH", "ARMATURE"})
bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, object_types={"MESH", "ARMATURE"},
                         add_leaf_bones=False, bake_anim=False, path_mode="COPY", embed_textures=True)
print(f"[export] {OUT_FBX}")

# --- posed verification: curl the body (roll-up) ---
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
for i, name in enumerate(BONES[1:], 1):
    pb = arm.pose.bones[name]
    pb.rotation_mode = "XYZ"
    pb.rotation_euler.y += math.radians(22 + 4 * i)      # progressive curl about Y
bpy.ops.object.mode_set(mode="OBJECT")
sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sc.render.resolution_x = sc.render.resolution_y = frame["res"]
sc.world = bpy.data.worlds.new("w"); sc.world.color = (0.18, 0.18, 0.2)
sc.display.shading.light = "STUDIO"; sc.display.shading.show_object_outline = True
cam_d = bpy.data.cameras.new("c"); cam_d.type = "ORTHO"; cam_d.ortho_scale = S
cam = bpy.data.objects.new("c", cam_d); sc.collection.objects.link(cam)
center = V((cx, cy, cz))
cam.location = center + V((0.75, -1.0, 0.35)).normalized() * (S * 3)
cam.rotation_euler = (center - cam.location).normalized().to_track_quat("-Z", "Y").to_euler()
sc.camera = cam
sc.render.filepath = os.path.join(RENDERS, "posed.png")
bpy.ops.render.render(write_still=True)
print(f"[verify] {sc.render.filepath}")
