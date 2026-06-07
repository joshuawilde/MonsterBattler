#!/usr/bin/env python3
"""Builds the Showdown-style message box just above the move buttons: a dark panel that GROWS
vertically (ContentSizeFitter) to fit the current action's lines, anchored at its bottom so it
expands upward. Wires it to BattleView and hides the old debug LogPanel. Run in EDIT mode."""
import json, urllib.request

URL = "http://127.0.0.1:17984/"
SA = "BattleUI/SafeArea"
MB = f"{SA}/MessageBar"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

try: cmd("gameobject.delete", path=MB)
except Exception: pass

# Dark semi-transparent box, bottom pivot so it grows UP. Bottom edge just above the moves (top ≈ -250).
cmd("ui.create_image", name="MessageBar", parent={"path": SA}, color=[0.07, 0.08, 0.11, 0.88])
cmd("ui.set_rect", path=MB, anchorMin=[0, 0.5], anchorMax=[1, 0.5], pivot=[0.5, 0], anchoredPosition=[0, -245], sizeDelta=[-48, 70])
cmd("component.add", path=MB, type="UnityEngine.CanvasGroup")
cmd("component.add", path=MB, type="MonsterBattler.Game.UI.MessageBar")
cmd("component.add", path=MB, type="UnityEngine.UI.VerticalLayoutGroup")
cmd("component.set_fields", path=MB, type="UnityEngine.UI.VerticalLayoutGroup", fields={
    "m_Spacing": 2, "m_ChildAlignment": 0,  # UpperLeft
    "m_ChildControlWidth": True, "m_ChildControlHeight": True,
    "m_ChildForceExpandWidth": True, "m_ChildForceExpandHeight": False,
    "m_Padding.m_Left": 24, "m_Padding.m_Right": 24, "m_Padding.m_Top": 12, "m_Padding.m_Bottom": 12,
})
cmd("component.add", path=MB, type="UnityEngine.UI.ContentSizeFitter")
cmd("component.set_fields", path=MB, type="UnityEngine.UI.ContentSizeFitter",
    fields={"m_HorizontalFit": 0, "m_VerticalFit": 2})  # vertical = PreferredSize

cmd("ui.create_text", name="Text", parent={"path": MB}, text="", fontSize=32, alignment="Left", color=[0.95, 0.96, 0.98, 1])
cmd("ui.config_text", path=f"{MB}/Text", alignment="Left", autoSize=False, fontSize=32, wrap=True)

cmd("component.set_fields", path=MB, type="MonsterBattler.Game.UI.MessageBar", fields={
    "_text": {"sceneObjectPath": f"{MB}/Text", "componentType": "TMPro.TextMeshProUGUI"},
    "_group": {"sceneObjectPath": MB, "componentType": "UnityEngine.CanvasGroup"},
})
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_messageBar": {"sceneObjectPath": MB, "componentType": "MonsterBattler.Game.UI.MessageBar"},
})

try: cmd("gameobject.set_active", path=f"{SA}/LogPanel", active=False)
except Exception as e: print("LogPanel hide skipped:", e)

cmd("scene.save_active")
print("Growing message box built + wired; debug LogPanel hidden.")
