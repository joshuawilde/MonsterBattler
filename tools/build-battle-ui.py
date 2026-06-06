#!/usr/bin/env python3
"""Authors the opponent-roster row and battle-log feed into BattleScene via the Unity MCP bridge,
then wires them to BattleView. Idempotent: deletes prior copies first. Re-runnable."""
import json, urllib.request

URL = "http://127.0.0.1:17984/"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    req = urllib.request.Request(URL, body, {"content-type": "application/json"})
    r = json.load(urllib.request.urlopen(req, timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

CANVAS = "BattleUI"

# --- clean prior runs ---
for path in (f"{CANVAS}/OppRoster", f"{CANVAS}/LogPanel"):
    try: cmd("gameobject.delete", path=path)
    except Exception: pass

# --- opponent roster: a transparent container holding 6 type-tinted chips, top of screen ---
cmd("ui.create_image", name="OppRoster", parent={"path": CANVAS}, color=[0, 0, 0, 0], raycastTarget=False)
cmd("ui.set_rect", path=f"{CANVAS}/OppRoster",
    anchorMin=[0, 1], anchorMax=[1, 1], pivot=[0.5, 1], anchoredPosition=[0, -205], sizeDelta=[-40, 64])

for i in range(6):
    chip = f"{CANVAS}/OppRoster/Chip{i}"
    cmd("ui.create_image", name=f"Chip{i}", parent={"path": f"{CANVAS}/OppRoster"}, color=[0.3, 0.3, 0.3, 1])
    cmd("ui.set_rect", path=chip, anchorMin=[i/6, 0], anchorMax=[(i+1)/6, 1], offsetMin=[3, 3], offsetMax=[-3, -3])
    cmd("component.add", path=chip, type="UnityEngine.UI.Outline")
    cmd("component.set_fields", path=chip, type="UnityEngine.UI.Outline",
        fields={"m_EffectColor": [1.0, 0.9, 0.2, 1.0], "m_EffectDistance": [3, -3]})
    cmd("component.add", path=chip, type="UnityEngine.UI.Button")  # tap to inspect
    cmd("component.add", path=chip, type="MonsterBattler.Game.UI.RosterIcon")
    cmd("ui.create_text", name="Label", parent={"path": chip},
        text="?", fontSize=20, alignment="MiddleCenter", color=[1, 1, 1, 1], bestFit=True)
    cmd("ui.set_rect", path=f"{chip}/Label", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[2, 2], offsetMax=[-2, -2])
    cmd("component.set_fields", path=chip, type="MonsterBattler.Game.UI.RosterIcon", fields={
        "_background":    {"sceneObjectPath": chip, "componentType": "UnityEngine.UI.Image"},
        "_label":         {"sceneObjectPath": f"{chip}/Label", "componentType": "UnityEngine.UI.Text"},
        "_activeOutline": {"sceneObjectPath": chip, "componentType": "UnityEngine.UI.Outline"},
        "_button":        {"sceneObjectPath": chip, "componentType": "UnityEngine.UI.Button"},
    })

# --- battle log feed: semi-transparent panel in the lower-center, above the move buttons ---
cmd("ui.create_image", name="LogPanel", parent={"path": CANVAS}, color=[0, 0, 0, 0.55])
cmd("ui.set_rect", path=f"{CANVAS}/LogPanel",
    anchorMin=[0, 0.5], anchorMax=[1, 0.5], pivot=[0.5, 1], anchoredPosition=[0, -15], sizeDelta=[-40, 220])
cmd("ui.create_text", name="LogText", parent={"path": f"{CANVAS}/LogPanel"},
    text="", fontSize=22, alignment="LowerLeft", color=[0.95, 0.95, 0.95, 1])
cmd("ui.set_rect", path=f"{CANVAS}/LogPanel/LogText",
    anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[14, 10], offsetMax=[-14, -10])

# --- wire BattleView (path-based object refs) ---
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_opponentRosterParent": {"sceneObjectPath": f"{CANVAS}/OppRoster", "componentType": "UnityEngine.RectTransform"},
    "_logText":              {"sceneObjectPath": f"{CANVAS}/LogPanel/LogText", "componentType": "UnityEngine.UI.Text"},
})

cmd("scene.save_active")
print("Battle UI built + wired + scene saved.")
