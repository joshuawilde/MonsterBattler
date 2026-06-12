#!/usr/bin/env python3
"""Installs the fx sprite assets (Assets/UI/fx), builds the FxLayer (FxScene component + inactive
fx-sprite template + full-screen flash overlay), and wires BattleView._fxScene. EDIT mode."""
import json, urllib.request, glob, os

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=60))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

# 1) fx art → proper assets
NAMES = ["orb", "ring", "lightning", "fist", "impact", "slash", "icicle", "leaf", "rock"]
for n in NAMES:
    src = f"/tmp/fxout/fx_{n}.png"
    if not os.path.exists(src): raise SystemExit(f"missing {src}")
    cmd("asset.copy_in", **{"from": src, "to": f"Assets/UI/fx/fx_{n}.png"})
    cmd("asset.import_sprite", path=f"Assets/UI/fx/fx_{n}.png")
print("fx assets imported")

# 2) FxLayer scene object + inactive template child
try: cmd("gameobject.delete", path="FxLayer")
except Exception: pass
cmd("gameobject.create", name="FxLayer")
cmd("component.add", path="FxLayer", type="MonsterBattler.Game.UI.FxScene")
cmd("gameobject.create", name="FxTemplate", parent={"path": "FxLayer"})
cmd("component.add", path="FxLayer/FxTemplate", type="UnityEngine.SpriteRenderer")
cmd("component.set_fields", path="FxLayer/FxTemplate", type="UnityEngine.SpriteRenderer",
    fields={"m_SortingOrder": 50})  # draw over the mons
cmd("gameobject.set_active", path="FxLayer/FxTemplate", active=False)

# 3) flash overlay — first sibling under SafeArea so it tints the field, under the UI
FLASH = f"{SA}/FxFlash"
try: cmd("gameobject.delete", path=FLASH)
except Exception: pass
cmd("ui.create_image", name="FxFlash", parent={"path": SA}, color=[0, 0, 0, 0], raycastTarget=False)
cmd("ui.set_rect", path=FLASH, anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("gameobject.set_sibling_index", path=FLASH, index=0)

# 4) wire FxScene
def sprite(n): return {"assetPath": f"Assets/UI/fx/fx_{n}.png", "assetType": "UnityEngine.Sprite"}
cmd("component.set_fields", path="FxLayer", type="MonsterBattler.Game.UI.FxScene", fields={
    "_fxPrefab": {"sceneObjectPath": "FxLayer/FxTemplate", "componentType": "UnityEngine.SpriteRenderer"},
    "_flash": {"sceneObjectPath": FLASH, "componentType": "UnityEngine.UI.Image"},
    "_orb": sprite("orb"), "_ring": sprite("ring"), "_lightning": sprite("lightning"),
    "_fist": sprite("fist"), "_impact": sprite("impact"), "_slash": sprite("slash"),
    "_icicle": sprite("icicle"), "_leaf": sprite("leaf"), "_rock": sprite("rock"),
})
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_fxScene": {"sceneObjectPath": "FxLayer", "componentType": "MonsterBattler.Game.UI.FxScene"},
})
cmd("scene.save_active")
print("FxLayer built + wired")
