#!/usr/bin/env python3
"""Restructures the 4 move buttons to fit a power/accuracy + description blurb: name → top band,
description → middle (best-fit), type/PP → thin bottom strip. Idempotent. Run with Unity open."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
MOVES = "BattleUI/SafeArea/Moves"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    req = urllib.request.Request(URL, body, {"content-type": "application/json"})
    r = json.load(urllib.request.urlopen(req, timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

for i in range(4):
    btn = f"{MOVES}/Move{i}"

    # Name: top band, best-fit so longer names don't clip the shorter band.
    cmd("ui.set_rect", path=f"{btn}/NameText",
        anchorMin=[0, 0.80], anchorMax=[1, 1], offsetMin=[10, 0], offsetMax=[-10, -4])
    cmd("component.set_fields", path=f"{btn}/NameText", type="UnityEngine.UI.Text", fields={
        "m_FontData.m_BestFit": True, "m_FontData.m_MinSize": 12, "m_FontData.m_MaxSize": 30})

    # Type / PP: thin bottom strip.
    cmd("ui.set_rect", path=f"{btn}/TypeText",
        anchorMin=[0, 0], anchorMax=[0.5, 0.16], offsetMin=[8, 2], offsetMax=[-4, -2])
    cmd("ui.set_rect", path=f"{btn}/PPText",
        anchorMin=[0.5, 0], anchorMax=[1, 0.16], offsetMin=[4, 2], offsetMax=[-8, -2])

    # Description blurb in the middle (best-fit handles long descriptions).
    desc = f"{btn}/DescText"
    try: cmd("gameobject.delete", path=desc)
    except Exception: pass
    cmd("ui.create_text", name="DescText", parent={"path": btn},
        text="", fontSize=20, alignment="UpperLeft", color=[1, 1, 1, 1], bestFit=True)
    cmd("ui.set_rect", path=desc,
        anchorMin=[0, 0.16], anchorMax=[1, 0.80], offsetMin=[12, 2], offsetMax=[-12, 2])
    cmd("component.add", path=desc, type="UnityEngine.UI.Outline")  # contrast on light type colors
    cmd("component.set_fields", path=desc, type="UnityEngine.UI.Outline",
        fields={"m_EffectColor": [0, 0, 0, 0.7], "m_EffectDistance": [1, -1]})

    cmd("component.set_fields", path=btn, type="MonsterBattler.Game.UI.MoveButton", fields={
        "_descText": {"sceneObjectPath": desc, "componentType": "UnityEngine.UI.Text"}})

cmd("scene.save_active")
print("Move buttons restructured with power/accuracy + description.")
