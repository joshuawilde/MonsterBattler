#!/usr/bin/env python3
"""Builds the win/loss end screen: a full-screen dark overlay with a big result label and a
'New Battle' button, wired to BattleView. Hidden until the battle ends. Run with Unity in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
ES = f"{SA}/EndScreen"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

try: cmd("gameobject.delete", path=ES)
except Exception: pass

# Full-screen dim overlay.
cmd("ui.create_image", name="EndScreen", parent={"path": SA}, color=[0.03, 0.04, 0.07, 0.88])
cmd("ui.set_rect", path=ES, anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])

# Result label (big, centered).
cmd("ui.create_text", name="ResultText", parent={"path": ES}, text="Victory!", fontSize=110, alignment="Center", color=[0.30, 0.78, 0.33, 1])
cmd("ui.set_rect", path=f"{ES}/ResultText", anchorMin=[0, 0.5], anchorMax=[1, 0.5], pivot=[0.5, 0.5], anchoredPosition=[0, 120], sizeDelta=[0, 200])
cmd("ui.config_text", path=f"{ES}/ResultText", alignment="Center", autoSize=False, fontSize=110, bold=True, wrap=False)

# New Battle button.
cmd("ui.create_image", name="RematchButton", parent={"path": ES}, color=[0.20, 0.42, 0.62, 1])
cmd("ui.set_rect", path=f"{ES}/RematchButton", anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5], anchoredPosition=[0, -120], sizeDelta=[420, 110])
cmd("component.add", path=f"{ES}/RematchButton", type="UnityEngine.UI.Button")
cmd("ui.create_text", name="Label", parent={"path": f"{ES}/RematchButton"}, text="New Battle", fontSize=44, alignment="Center", color=[1, 1, 1, 1])
cmd("ui.set_rect", path=f"{ES}/RematchButton/Label", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("ui.config_text", path=f"{ES}/RematchButton/Label", alignment="Center", autoSize=False, fontSize=44, bold=True)

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_endScreen": {"sceneObjectPath": ES},  # GameObject field — no componentType
    "_endResultText": {"sceneObjectPath": f"{ES}/ResultText", "componentType": "TMPro.TextMeshProUGUI"},
    "_rematchButton": {"sceneObjectPath": f"{ES}/RematchButton", "componentType": "UnityEngine.UI.Button"},
})
cmd("scene.save_active")
print("End screen built and wired.")
