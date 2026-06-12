#!/usr/bin/env python3
"""Adds the physical/special/status category icon slot to the MoveButton prefab (battle move
cards) and re-wires _categoryIcon. MoveCell gets its icon via build-move-equip-ui.py (rerun it
after this). Run in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
MOVES = "BattleUI/SafeArea/Moves"
PREFAB = "Assets/Prefabs/MoveButton.prefab"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

m0 = f"{MOVES}/Move0"

# Category icon — bottom-left corner, just before the type text.
try: cmd("gameobject.delete", path=f"{m0}/CatIcon")
except Exception: pass
cmd("ui.create_image", name="CatIcon", parent={"path": m0}, color=[1, 1, 1, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{m0}/CatIcon", anchorMin=[0, 0], anchorMax=[0, 0], pivot=[0, 0], anchoredPosition=[10, 5], sizeDelta=[26, 26])
cmd("component.set_fields", path=f"{m0}/CatIcon", type="UnityEngine.UI.Image", fields={"m_PreserveAspect": True})

# Shift the type text right so it doesn't sit under the icon.
cmd("ui.set_rect", path=f"{m0}/TypeText", anchorMin=[0, 0], anchorMax=[0.6, 0.20], offsetMin=[44, 2], offsetMax=[-2, -2])

cmd("component.set_fields", path=m0, type="MonsterBattler.Game.UI.MoveButton",
    fields={"_categoryIcon": {"sceneObjectPath": f"{m0}/CatIcon", "componentType": "UnityEngine.UI.Image"}})

cmd("prefab.save_as", path=m0, assetPath=PREFAB, connectInstance=True)

# Recreate Move1-3 as fresh instances so they pick up the new child + wiring.
for i in (1, 2, 3):
    try: cmd("gameobject.delete", path=f"{MOVES}/Move{i}")
    except Exception: pass
    cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": MOVES}, name=f"Move{i}")

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_move1": {"sceneObjectPath": f"{MOVES}/Move1", "componentType": "MonsterBattler.Game.UI.MoveButton"},
    "_move2": {"sceneObjectPath": f"{MOVES}/Move2", "componentType": "MonsterBattler.Game.UI.MoveButton"},
    "_move3": {"sceneObjectPath": f"{MOVES}/Move3", "componentType": "MonsterBattler.Game.UI.MoveButton"},
})
cmd("scene.save_active")
print("Category icon added to MoveButton prefab + instances rewired.")
