#!/usr/bin/env python3
"""Adds an always-on info strip (ability · item, then the stat line) under each active mon's
nameplate, wired to BattleView (_activeInfo0 player / _activeInfo1 opponent). Run in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def make(parent, name, anchor, pivot, pos, align):
    p = f"{parent}/{name}"
    try: cmd("gameobject.delete", path=p)
    except Exception: pass
    cmd("ui.create_text", name=name, parent={"path": parent}, text="", fontSize=20, alignment=align, color=[0.92, 0.94, 1, 1])
    cmd("ui.set_rect", path=p, anchorMin=anchor, anchorMax=anchor, pivot=pivot, anchoredPosition=pos, sizeDelta=[760, 60])
    cmd("ui.config_text", path=p, alignment=align, autoSize=False, fontSize=20, wrap=True)
    return p

p0 = make(f"{SA}/LocalPlayer", "ActiveInfo0", [0, 1], [0, 1], [8, -104], "Left")
p1 = make(f"{SA}/OtherPlayer", "ActiveInfo1", [1, 1], [1, 1], [-8, -104], "Right")

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_activeInfo0": {"sceneObjectPath": p0, "componentType": "TMPro.TextMeshProUGUI"},
    "_activeInfo1": {"sceneObjectPath": p1, "componentType": "TMPro.TextMeshProUGUI"},
})
cmd("scene.save_active")
print("Active-info strips added under each nameplate and wired.")
