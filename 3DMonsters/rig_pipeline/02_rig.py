"""
Lift the 2D joint markers to 3D, build the dragon armature, skin with Automatic
Weights, export a rigged FBX, and render a posed verification image.

Run:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python 02_rig.py
"""
import bpy
import mathutils
import json
import os
import math
import sys

_argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
FBX = _argv[0] if len(_argv) > 0 else "/Users/joshuawilde/MonsterBattler/3DMonsters/dragon+3d+model/tripo_test_1.fbx"
BASE = _argv[1] if len(_argv) > 1 else "/Users/joshuawilde/MonsterBattler/3DMonsters/rig_pipeline"
_name = os.path.splitext(os.path.basename(FBX))[0]
OUT_FBX = os.path.join(BASE, _name + "_rigged.fbx")
RENDERS = os.path.join(BASE, "renders")
V = mathutils.Vector

frame = json.load(open(os.path.join(RENDERS, "frame.json")))
markers = json.load(open(os.path.join(BASE, "markers.json")))["joints"]
cx, cy, cz = frame["center"]
S = frame["ortho_scale"]


def lift(m):
    fu, fv = m["front"]
    su = m.get("side_u", 0.5)
    x = cx + (fu - 0.5) * S
    z = cz + (0.5 - fv) * S
    y = cy + (su - 0.5) * S
    return V((x, y, z))


J = {name: lift(m) for name, m in markers.items()}

# tip helper for terminal bones (point forward +X, slightly down) so they have length
TIP = V((0.07, 0.0, -0.03))

# (bone_name, head_joint_or_vec, tail_joint_or_vec, parent, connect)
BONES = [
    ("pelvis", "hips", "spineJ", None, False),
    ("spine", "spineJ", "chestJ", "pelvis", True),
    ("chest", "chestJ", "neckJ", "spine", True),
    ("neck", "neckJ", "headJ", "chest", True),
    ("head", "headJ", "snoutJ", "neck", True),
    ("jaw", "headJ", "jawJ", "head", False),

    ("tailA", "hips", "tail1", "pelvis", False),
    ("tailB", "tail1", "tail2", "tailA", True),
    ("tailC", "tail2", "tail3", "tailB", True),

    ("upperarm.L", "shoulderL", "elbowL", "chest", False),
    ("forearm.L", "elbowL", "handL", "upperarm.L", True),
    ("hand.L", "handL", None, "forearm.L", True),
    ("upperarm.R", "shoulderR", "elbowR", "chest", False),
    ("forearm.R", "elbowR", "handR", "upperarm.R", True),
    ("hand.R", "handR", None, "forearm.R", True),

    ("thigh.L", "thighL", "kneeL", "pelvis", False),
    ("shin.L", "kneeL", "footL", "thigh.L", True),
    ("foot.L", "footL", None, "shin.L", True),
    ("thigh.R", "thighR", "kneeR", "pelvis", False),
    ("shin.R", "kneeR", "footR", "thigh.R", True),
    ("foot.R", "footR", None, "shin.R", True),
]


def pos(ref, head):
    if ref is None:           # terminal: offset from head
        return head + TIP
    return J[ref]


# --- scene + import ---
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
for o in bpy.context.scene.objects:
    o.select_set(False)
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
mesh = bpy.context.view_layer.objects.active
mesh.select_set(False)
print(f"[info] mesh: {mesh.name}, verts={len(mesh.data.vertices)}")

# The Tripo FBX ships pre-rigged (a humanoid skeleton + skin, no tail/jaw). Strip
# all of it so our output contains ONLY our rig — otherwise the export carries two
# skeletons and the viewer may bind the mesh to the old one.
stripped = len(mesh.vertex_groups)
mesh.vertex_groups.clear()
for m in list(mesh.modifiers):
    if m.type == "ARMATURE":
        mesh.modifiers.remove(m)
for o in list(bpy.context.scene.objects):
    if o.type == "ARMATURE":
        bpy.data.objects.remove(o, do_unlink=True)
print(f"[clean] stripped {stripped} pre-existing vertex groups + any imported armature")

# --- armature ---
arm_data = bpy.data.armatures.new("DragonRig")
arm = bpy.data.objects.new("DragonRig", arm_data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
eb = arm_data.edit_bones
created = {}
for name, hj, tj, parent, connect in BONES:
    b = eb.new(name)
    b.head = J[hj]
    b.tail = pos(tj, J[hj])
    if (b.tail - b.head).length < 1e-4:
        b.tail = b.head + TIP
    if parent:
        b.parent = created[parent]
        b.use_connect = bool(connect)
    created[name] = b
bpy.ops.object.mode_set(mode="OBJECT")
print(f"[info] armature bones: {len(arm_data.bones)}")

# --- skin: automatic (bone heat) weights ---
mesh.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
try:
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    print("[skin] automatic (bone-heat) weights OK")
except RuntimeError as e:
    print(f"[skin] auto failed ({e}); falling back to envelope")
    bpy.ops.object.parent_set(type="ARMATURE_ENVELOPE")

# diagnostic: how many verts each bone actually controls (weight > 0.1)
counts = {vg.index: 0 for vg in mesh.vertex_groups}
idx2name = {vg.index: vg.name for vg in mesh.vertex_groups}
for v in mesh.data.vertices:
    for g in v.groups:
        if g.weight > 0.1 and g.group in counts:
            counts[g.group] += 1
print("[weights] " + ", ".join(f"{idx2name[i]}={counts[i]}" for i in sorted(counts)))
dead = [idx2name[i] for i in counts if counts[i] == 0]
if dead:
    print("[weights] DEAD bones (0 verts):", dead, "- assigning nearest verts")
    mw = mesh.matrix_world
    aw = arm.matrix_world
    segs = {b.name: (aw @ b.head_local, aw @ b.tail_local) for b in arm.data.bones}

    def seg_dist(p, a, b):
        ab = b - a
        d = ab.length_squared
        t = max(0.0, min(1.0, (p - a).dot(ab) / d)) if d > 1e-9 else 0.0
        return (p - (a + ab * t)).length

    vgs = {n: (mesh.vertex_groups.get(n) or mesh.vertex_groups.new(name=n)) for n in dead}
    wco = [mw @ v.co for v in mesh.data.vertices]
    K = 60
    for n in dead:                                            # each dead bone grabs its own nearest verts
        a, b = segs[n]
        order = sorted(range(len(wco)), key=lambda i: seg_dist(wco[i], a, b))
        for i in order[:K]:
            vgs[n].add([i], 1.0, "REPLACE")
    bpy.context.view_layer.objects.active = mesh
    for o in bpy.context.scene.objects:
        o.select_set(o is mesh)
    bpy.ops.object.vertex_group_normalize_all()
    c2 = {vg.index: 0 for vg in mesh.vertex_groups}
    for v in mesh.data.vertices:
        for g in v.groups:
            if g.weight > 0.1 and g.group in c2:
                c2[g.group] += 1
    print("[weights] fixed: " + ", ".join(f"{n}={c2[mesh.vertex_groups[n].index]}" for n in dead))

# --- export rigged FBX (rest pose) ---
for o in bpy.context.scene.objects:
    o.select_set(o.type in {"MESH", "ARMATURE"})
bpy.ops.export_scene.fbx(
    filepath=OUT_FBX,
    use_selection=True,
    object_types={"MESH", "ARMATURE"},
    add_leaf_bones=False,
    bake_anim=False,
    path_mode="COPY",
    embed_textures=True,
)
print(f"[export] {OUT_FBX}")

# --- verification: pose a few bones and render the profile ---
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")


def rot(bone, axis, deg):
    pb = arm.pose.bones[bone]
    pb.rotation_mode = "XYZ"
    r = list(pb.rotation_euler)
    r["XYZ".index(axis)] += math.radians(deg)
    pb.rotation_euler = r


rot("jaw", "Y", 42)                       # open mouth wide
rot("upperarm.L", "X", 55); rot("upperarm.R", "X", 55)   # swing both arms forward (visible from 3/4)
rot("forearm.L", "X", 40); rot("forearm.R", "X", 40)
rot("tailB", "Z", 26); rot("tailC", "Z", 34)             # curl tail
rot("thigh.L", "X", -35); rot("thigh.R", "X", -35)       # kick legs back
bpy.ops.object.mode_set(mode="OBJECT")

# render the posed profile using same framing as 01
scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = frame["res"]
scene.render.resolution_y = frame["res"]
scene.world = bpy.data.worlds.new("w")
scene.world.color = (0.18, 0.18, 0.2)
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
scene.display.shading.show_object_outline = True
cam_d = bpy.data.cameras.new("c"); cam_d.type = "ORTHO"; cam_d.ortho_scale = S
cam = bpy.data.objects.new("c", cam_d); scene.collection.objects.link(cam)
center = V((cx, cy, cz))
cam.location = center + V((0.75, -1.0, 0.35)).normalized() * (S * 3)
cam.rotation_euler = (center - cam.location).normalized().to_track_quat("-Z", "Y").to_euler()
scene.camera = cam
scene.render.filepath = os.path.join(RENDERS, "posed.png")
bpy.ops.render.render(write_still=True)
print(f"[verify] {scene.render.filepath}")
print("[done]")
