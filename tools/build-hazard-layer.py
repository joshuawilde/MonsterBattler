#!/usr/bin/env python3
"""Installs the hazard fx sprites (spike, web; rock already exists), builds the HazardLayer scene
object (template + per-side ground anchors) and wires BattleView._hazards. EDIT mode."""
import json, urllib.request, os

_pf = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=60))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

# 1) hazard art → assets (rock reused from move fx)
for n in ["spike", "web"]:
    src = f"/tmp/fxout/fx_{n}.png"
    if not os.path.exists(src): raise SystemExit(f"missing {src}")
    cmd("asset.copy_in", **{"from": src, "to": f"Assets/UI/fx/fx_{n}.png"})
    cmd("asset.import_sprite", path=f"Assets/UI/fx/fx_{n}.png")
print("hazard sprites imported")

# 2) HazardLayer object + inactive template + ground anchors in front of each mon
#    MonSprite0 at (-0.92, .25, -1), MonSprite1 at (2, .25, 3); anchors ~1 unit toward center.
try: cmd("gameobject.delete", path="HazardLayer")
except Exception: pass
cmd("gameobject.create", name="HazardLayer")
cmd("component.add", path="HazardLayer", type="MonsterBattler.Game.UI.HazardLayer")

cmd("gameobject.create", name="Template", parent={"path": "HazardLayer"})
cmd("component.add", path="HazardLayer/Template", type="UnityEngine.SpriteRenderer")
cmd("gameobject.set_active", path="HazardLayer/Template", active=False)

cmd("gameobject.create", name="Anchor0", parent={"path": "HazardLayer"})
cmd("gameobject.set_transform", path="HazardLayer/Anchor0", position=[-0.33, 0.05, -0.19])
cmd("gameobject.create", name="Anchor1", parent={"path": "HazardLayer"})
cmd("gameobject.set_transform", path="HazardLayer/Anchor1", position=[1.41, 0.05, 2.19])

# 3) wire fields
def sprite(n): return {"assetPath": f"Assets/UI/fx/fx_{n}.png", "assetType": "UnityEngine.Sprite"}
cmd("component.set_fields", path="HazardLayer", type="MonsterBattler.Game.UI.HazardLayer", fields={
    "_template": {"sceneObjectPath": "HazardLayer/Template", "componentType": "UnityEngine.SpriteRenderer"},
    "_anchor0": {"sceneObjectPath": "HazardLayer/Anchor0", "componentType": "UnityEngine.Transform"},
    "_anchor1": {"sceneObjectPath": "HazardLayer/Anchor1", "componentType": "UnityEngine.Transform"},
    "_rock": sprite("rock"), "_spike": sprite("spike"), "_web": sprite("web"),
})
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_hazards": {"sceneObjectPath": "HazardLayer", "componentType": "MonsterBattler.Game.UI.HazardLayer"},
})
cmd("scene.save_active")
print("HazardLayer built + wired")
