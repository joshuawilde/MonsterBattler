#!/usr/bin/env python3
"""Adds a small green "TEAM" chip to the MonCell prefab (top-right) and wires _teamBadge.
Replaces the inactive MetaRoot/CellTemplate clone + rewires MenuController. EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
ROOT = f"{SA}/MetaRoot"
PREFAB = "Assets/Prefabs/MonCell.prefab"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

# Work on a temp instance of the prefab.
T = f"{SA}/__MonCellEdit"
try: cmd("gameobject.delete", path=T)
except Exception: pass
cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": SA}, name="__MonCellEdit")

try: cmd("gameobject.delete", path=f"{T}/TeamBadge")
except Exception: pass
cmd("ui.create_image", name="TeamBadge", parent={"path": T}, color=[0.18, 0.55, 0.28, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/TeamBadge", anchorMin=[1, 1], anchorMax=[1, 1], pivot=[1, 1], anchoredPosition=[-4, -4], sizeDelta=[64, 28])
cmd("ui.create_text", name="Label", parent={"path": f"{T}/TeamBadge"}, text="TEAM", fontSize=15, alignment="Center", color=[1, 1, 1, 1])
cmd("ui.set_rect", path=f"{T}/TeamBadge/Label", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("ui.config_text", path=f"{T}/TeamBadge/Label", alignment="Center", autoSize=False, fontSize=15, bold=True, wrap=False)
cmd("gameobject.set_active", path=f"{T}/TeamBadge", active=False)

cmd("component.set_fields", path=T, type="MonsterBattler.Game.Meta.MonCell", fields={
    "_teamBadge": {"sceneObjectPath": f"{T}/TeamBadge"},
})
cmd("prefab.save_as", path=T, assetPath=PREFAB, connectInstance=False)
cmd("gameobject.delete", path=T)

# Refresh the inactive clone template the Box grid instantiates from.
try: cmd("gameobject.delete", path=f"{ROOT}/CellTemplate")
except Exception: pass
cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": ROOT}, name="CellTemplate")
cmd("gameobject.set_active", path=f"{ROOT}/CellTemplate", active=False)
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_monCellPrefab": {"sceneObjectPath": f"{ROOT}/CellTemplate", "componentType": "MonsterBattler.Game.Meta.MonCell"},
})
cmd("scene.save_active")
print("TEAM badge added to MonCell + template refreshed.")
