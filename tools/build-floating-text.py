#!/usr/bin/env python3
"""Builds Assets/Prefabs/FloatingText.prefab (a colored combat popup chip that floats up + fades)
and two popup anchors over the mons, then wires BattleView. Run with Unity open in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
PREFAB = "Assets/Prefabs/FloatingText.prefab"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

# --- FloatingText prefab ---
T = f"{SA}/__FloatTemplate"
try: cmd("gameobject.delete", path=T)
except Exception: pass
cmd("ui.create_image", name="__FloatTemplate", parent={"path": SA}, color=[0.27, 0.62, 0.30, 1])
cmd("ui.set_rect", path=T, anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5], anchoredPosition=[0, 0], sizeDelta=[170, 60])
cmd("component.add", path=T, type="UnityEngine.CanvasGroup")
cmd("component.add", path=T, type="MonsterBattler.Game.UI.FloatingText")
cmd("ui.create_text", name="Label", parent={"path": T}, text="+11%", fontSize=34, alignment="Center", color=[1, 1, 1, 1])
cmd("ui.set_rect", path=f"{T}/Label", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[6, 2], offsetMax=[-6, -2])
cmd("ui.config_text", path=f"{T}/Label", alignment="Center", autoSize=True, fontSize=34, fontSizeMin=16, bold=True, wrap=False)
cmd("component.set_fields", path=T, type="MonsterBattler.Game.UI.FloatingText", fields={
    "_background": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Image"},
    "_label": {"sceneObjectPath": f"{T}/Label", "componentType": "TMPro.TextMeshProUGUI"},
    "_group": {"sceneObjectPath": T, "componentType": "UnityEngine.CanvasGroup"},
})
cmd("prefab.save_as", path=T, assetPath=PREFAB, connectInstance=False)
cmd("gameobject.delete", path=T)

# --- Popup anchors over each mon (rough; reposition in editor to taste) ---
def anchor(name, pos):
    p = f"{SA}/{name}"
    try: cmd("gameobject.delete", path=p)
    except Exception: pass
    cmd("ui.create_image", name=name, parent={"path": SA}, color=[0, 0, 0, 0], raycastTarget=False)
    cmd("ui.set_rect", path=p, anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5], anchoredPosition=pos, sizeDelta=[10, 10])
    return p

a0 = anchor("PopupAnchor0", [-170, -210])   # player mon (lower-left of center)
a1 = anchor("PopupAnchor1", [230, 130])     # opponent mon (upper-right of center)

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_popupAnchor0": {"sceneObjectPath": a0, "componentType": "UnityEngine.RectTransform"},
    "_popupAnchor1": {"sceneObjectPath": a1, "componentType": "UnityEngine.RectTransform"},
    "_floatingTextPrefab": {"assetPath": PREFAB, "assetType": "MonsterBattler.Game.UI.FloatingText"},
})
cmd("scene.save_active")
print("FloatingText prefab + popup anchors built and wired.")
