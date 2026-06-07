#!/usr/bin/env python3
"""Creates the two on-field monsters as WORLD-SPACE SpriteRenderers under BattleStage (where the
spheres were), with the MonsterView component, and wires BattleView. Removes any old UI-image views
and hides the placeholder spheres. Run with Unity in EDIT mode."""
import json, urllib.request

URL = "http://127.0.0.1:17984/"
SA = "BattleUI/SafeArea"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

# Remove the old UI-image views, if present.
for old in (f"{SA}/MonView0", f"{SA}/MonView1"):
    try: cmd("gameobject.delete", path=old)
    except Exception: pass

def make_view(name, pos, scale, player):
    p = f"BattleStage/{name}"
    try: cmd("gameobject.delete", path=p)
    except Exception: pass
    cmd("gameobject.create", name=name, parent={"path": "BattleStage"})
    cmd("gameobject.set_transform", path=p, position=pos, scale=[scale, scale, scale])
    cmd("component.add", path=p, type="UnityEngine.SpriteRenderer")
    cmd("component.set_fields", path=p, type="UnityEngine.SpriteRenderer", fields={"m_SortingOrder": 10})
    cmd("component.add", path=p, type="MonsterBattler.Game.UI.MonsterView")
    cmd("component.set_fields", path=p, type="MonsterBattler.Game.UI.MonsterView", fields={
        "_renderer": {"sceneObjectPath": p, "componentType": "UnityEngine.SpriteRenderer"},
        "_isPlayerSide": player,
    })
    return p

# Slots were at (-0.92,1,-1) player and (2,1,3) opponent; feet near ground (~y0.25).
ply = make_view("MonSprite0", [-0.92, 0.25, -1.0], 2.4, True)
opp = make_view("MonSprite1", [2.0, 0.25, 3.0], 2.4, False)

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_monView0": {"sceneObjectPath": ply, "componentType": "MonsterBattler.Game.UI.MonsterView"},
    "_monView1": {"sceneObjectPath": opp, "componentType": "MonsterBattler.Game.UI.MonsterView"},
})

for s in ("BattleStage/Slot0", "BattleStage/Slot1"):
    try: cmd("gameobject.set_active", path=s, active=False)
    except Exception as e: print(f"hide {s} skipped:", e)

cmd("scene.save_active")
print("World-space monster sprites built + wired; spheres hidden.")
