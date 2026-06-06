#!/usr/bin/env python3
"""Adds a stat-boost readout line under each active mon's HP (player left / opponent right) and
wires them to BattleView. Idempotent. Run with Unity open in edit mode."""
import json, urllib.request

URL = "http://127.0.0.1:17984/"
SA = "BattleUI/SafeArea"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    req = urllib.request.Request(URL, body, {"content-type": "application/json"})
    r = json.load(urllib.request.urlopen(req, timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def make(parent, name, anchor, pivot, pos, align):
    p = f"{parent}/{name}"
    try: cmd("gameobject.delete", path=p)
    except Exception: pass
    cmd("ui.create_text", name=name, parent={"path": parent}, text="", fontSize=20, alignment=align, color=[1, 1, 1, 1])
    cmd("ui.set_rect", path=p, anchorMin=anchor, anchorMax=anchor, pivot=pivot, anchoredPosition=pos, sizeDelta=[740, 32])
    cmd("ui.config_text", path=p, alignment=align, autoSize=False, fontSize=20, wrap=False)
    return p

# Right of the name/HP bar (clear space; the area below collides with the roster row).
make(f"{SA}/LocalPlayer", "BoostText0", [0, 1], [0, 1], [500, -44], "Left")
make(f"{SA}/OtherPlayer", "BoostText1", [1, 1], [1, 1], [-500, -44], "Right")

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_boost0": {"sceneObjectPath": f"{SA}/LocalPlayer/BoostText0", "componentType": "TMPro.TextMeshProUGUI"},
    "_boost1": {"sceneObjectPath": f"{SA}/OtherPlayer/BoostText1", "componentType": "TMPro.TextMeshProUGUI"},
})
cmd("scene.save_active")
print("Boost readouts added under each nameplate and wired.")
