#!/usr/bin/env python3
"""Builds the Pokemon info overlay + an 'Info' toggle button into BattleScene and wires them to
BattleView. Idempotent. UI lives under BattleUI/SafeArea. Run with Unity open."""
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

for path in (f"{ROOT}/InfoPanel", f"{ROOT}/InfoButton"):  # InfoButton is legacy — removed if present
    try: cmd("gameobject.delete", path=path)
    except Exception: pass

# --- Info overlay: a dark panel covering the mid screen, with the details Text + InfoPanel component ---
cmd("ui.create_image", name="InfoPanel", parent={"path": ROOT}, color=[0.09, 0.10, 0.13, 0.97])
cmd("ui.set_rect", path=f"{ROOT}/InfoPanel",
    anchorMin=[0.03, 0.12], anchorMax=[0.97, 0.88], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("component.add", path=f"{ROOT}/InfoPanel", type="MonsterBattler.Game.UI.InfoPanel")
cmd("ui.create_text", name="InfoText", parent={"path": f"{ROOT}/InfoPanel"},
    text="", fontSize=26, alignment="UpperLeft", color=[0.92, 0.93, 0.96, 1])
cmd("ui.set_rect", path=f"{ROOT}/InfoPanel/InfoText",
    anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[24, 100], offsetMax=[-24, -20])  # leave room for buttons

def panel_button(name, color, anchor, pivot, pos, label):
    p = f"{ROOT}/InfoPanel/{name}"
    cmd("ui.create_image", name=name, parent={"path": f"{ROOT}/InfoPanel"}, color=color)
    cmd("ui.set_rect", path=p, anchorMin=anchor, anchorMax=anchor, pivot=pivot,
        anchoredPosition=pos, sizeDelta=[240, 70])
    cmd("component.add", path=p, type="UnityEngine.UI.Button")
    cmd("ui.create_text", name="Label", parent={"path": p},
        text=label, fontSize=30, alignment="MiddleCenter", color=[1, 1, 1, 1])
    cmd("ui.set_rect", path=f"{p}/Label", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
    return p

close_btn = panel_button("CloseButton", [0.30, 0.18, 0.20, 1], [0, 0], [0, 0], [24, 18], "Close")
swap_btn = panel_button("SwapButton", [0.18, 0.30, 0.22, 1], [1, 0], [1, 0], [-24, 18], "Swap")

cmd("component.set_fields", path=f"{ROOT}/InfoPanel", type="MonsterBattler.Game.UI.InfoPanel", fields={
    "_text": {"sceneObjectPath": f"{ROOT}/InfoPanel/InfoText", "componentType": "UnityEngine.UI.Text"},
    "_closeButton": {"sceneObjectPath": close_btn, "componentType": "UnityEngine.UI.Button"},
    "_swapButton": {"sceneObjectPath": swap_btn, "componentType": "UnityEngine.UI.Button"},
})

# --- wire BattleView (panel opens by tapping any monster; closes via the panel's Close button) ---
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_infoPanel": {"sceneObjectPath": f"{ROOT}/InfoPanel", "componentType": "MonsterBattler.Game.UI.InfoPanel"},
})

cmd("scene.save_active")
print("Info overlay built and wired.")
