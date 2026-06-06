#!/usr/bin/env python3
"""Adds status badges, per-side condition labels, and a field-status (weather/terrain/room) label
to BattleScene and wires them to BattleView. Idempotent. Run with Unity open."""
import json, urllib.request

URL = "http://127.0.0.1:17984/"
ROOT = "BattleUI/SafeArea"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    req = urllib.request.Request(URL, body, {"content-type": "application/json"})
    r = json.load(urllib.request.urlopen(req, timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def text(path_parent, name, font, align, rect):
    p = f"{path_parent}/{name}"
    try: cmd("gameobject.delete", path=p)
    except Exception: pass
    cmd("ui.create_text", name=name, parent={"path": path_parent},
        text="", fontSize=font, alignment=align, color=[1, 1, 1, 1])
    cmd("ui.set_rect", path=p, **rect)
    return p

# Status badges (top-right of each mon's info band) + per-side condition labels (along the bottom).
text(f"{ROOT}/LocalPlayer", "Status0Text", 30, "MiddleRight",
     dict(anchorMin=[0.78, 0.5], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[-10, -4]))
text(f"{ROOT}/OtherPlayer", "Status1Text", 30, "MiddleRight",
     dict(anchorMin=[0.78, 0.5], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[-10, -4]))
text(f"{ROOT}/LocalPlayer", "SideText0", 20, "LowerLeft",
     dict(anchorMin=[0, 0], anchorMax=[1, 0.32], offsetMin=[14, 2], offsetMax=[-14, 0]))
text(f"{ROOT}/OtherPlayer", "SideText1", 20, "LowerLeft",
     dict(anchorMin=[0, 0], anchorMax=[1, 0.32], offsetMin=[14, 2], offsetMax=[-14, 0]))

# Field-wide status (weather / terrain / Trick Room), centered in the upper stage area.
text(ROOT, "FieldText", 26, "MiddleCenter",
     dict(anchorMin=[0, 0.5], anchorMax=[1, 0.5], pivot=[0.5, 0.5],
          anchoredPosition=[0, 300], sizeDelta=[0, 44]))

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_status0":   {"sceneObjectPath": f"{ROOT}/LocalPlayer/Status0Text", "componentType": "UnityEngine.UI.Text"},
    "_status1":   {"sceneObjectPath": f"{ROOT}/OtherPlayer/Status1Text", "componentType": "UnityEngine.UI.Text"},
    "_sideText0": {"sceneObjectPath": f"{ROOT}/LocalPlayer/SideText0", "componentType": "UnityEngine.UI.Text"},
    "_sideText1": {"sceneObjectPath": f"{ROOT}/OtherPlayer/SideText1", "componentType": "UnityEngine.UI.Text"},
    "_fieldText": {"sceneObjectPath": f"{ROOT}/FieldText", "componentType": "UnityEngine.UI.Text"},
})

cmd("scene.save_active")
print("Status badges, side-condition labels, and field-status label built and wired.")
