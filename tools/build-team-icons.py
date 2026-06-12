#!/usr/bin/env python3
"""Builds Assets/Prefabs/TeamIcon.prefab (thumbnail + HP bar + button) and uses the SAME prefab for
both team rosters: a new PlayerRoster (bottom, replaces Switch0-5) and the rebuilt OppRoster (top).
Wires BattleView. Run with Unity in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
PREFAB = "Assets/Prefabs/TeamIcon.prefab"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

# ---------- TeamIcon prefab ----------
T = f"{SA}/__TeamIconTemplate"
try: cmd("gameobject.delete", path=T)
except Exception: pass
def txt(name, parent, s, size, color=(1,1,1,1)):
    p = f"{parent}/{name}"
    cmd("ui.create_text", name=name, parent={"path": parent}, text=s, fontSize=size, alignment="Center", color=list(color))
    cmd("ui.config_text", path=p, alignment="Center", autoSize=False, fontSize=size, bold=True, wrap=False)
    return p

cmd("ui.create_image", name="__TeamIconTemplate", parent={"path": SA}, color=[0.13, 0.14, 0.18, 0.92])
cmd("ui.set_rect", path=T, anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5], anchoredPosition=[0, 0], sizeDelta=[150, 132])
cmd("component.add", path=T, type="UnityEngine.UI.Button")
cmd("component.add", path=T, type="UnityEngine.UI.Outline")
cmd("component.set_fields", path=T, type="UnityEngine.UI.Outline",
    fields={"m_EffectColor": [1, 0.92, 0.4, 1], "m_Enabled": False})
cmd("component.add", path=T, type="UnityEngine.UI.LayoutElement")
cmd("component.set_fields", path=T, type="UnityEngine.UI.LayoutElement", fields={"m_PreferredWidth": 150, "m_PreferredHeight": 132})
cmd("component.add", path=T, type="MonsterBattler.Game.UI.TeamIcon")

# thumbnail (top ~67%)
cmd("ui.create_image", name="Thumb", parent={"path": T}, color=[1, 1, 1, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/Thumb", anchorMin=[0, 0.33], anchorMax=[1, 1], offsetMin=[4, 0], offsetMax=[-4, -2])
cmd("component.set_fields", path=f"{T}/Thumb", type="UnityEngine.UI.Image", fields={"m_PreserveAspect": True})

# name band
NAME = txt("Name", T, "Name", 21)
cmd("ui.set_rect", path=NAME, anchorMin=[0, 0.15], anchorMax=[1, 0.33], pivot=[0.5, 0.5], anchoredPosition=[0, 0], sizeDelta=[-6, 0])

# hp bar (bottom strip)
cmd("ui.create_image", name="HpBg", parent={"path": T}, color=[0.05, 0.05, 0.07, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/HpBg", anchorMin=[0.08, 0.05], anchorMax=[0.92, 0.13], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("ui.create_image", name="HpFill", parent={"path": f"{T}/HpBg"}, color=[0.30, 0.78, 0.33, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/HpBg/HpFill", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("component.set_fields", path=f"{T}/HpBg/HpFill", type="UnityEngine.UI.Image",
    fields={"m_Type": 3, "m_FillMethod": 0, "m_FillOrigin": 0, "m_FillAmount": 1.0})

# type tags (top, side by side): colored block + 3-letter text
def tag(name, x):
    p = f"{T}/{name}"
    cmd("ui.create_image", name=name, parent={"path": T}, color=[0.4,0.4,0.45,1], raycastTarget=False)
    cmd("ui.set_rect", path=p, anchorMin=[0,1], anchorMax=[0,1], pivot=[0,1], anchoredPosition=[x,-3], sizeDelta=[45,20])
    lab = txt("Txt", p, "TYP", 13)
    cmd("ui.set_rect", path=lab, anchorMin=[0,0], anchorMax=[1,1], offsetMin=[0,0], offsetMax=[0,0])
    return p, lab
TAG1, TXT1 = tag("Tag1", 3)
TAG2, TXT2 = tag("Tag2", 51)

# ghost overlay (unseen enemy) — dark cover + big "?"
cmd("ui.create_image", name="Ghost", parent={"path": T}, color=[0.06,0.07,0.11,0.96], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/Ghost", anchorMin=[0,0], anchorMax=[1,1], offsetMin=[0,0], offsetMax=[0,0])
GQ = txt("Q", f"{T}/Ghost", "?", 64, (0.7,0.72,0.85,1))
cmd("ui.set_rect", path=GQ, anchorMin=[0,0], anchorMax=[1,1], offsetMin=[0,0], offsetMax=[0,0])
cmd("gameobject.set_active", path=f"{T}/Ghost", active=False)

# unplayed badge (own mon not yet sent out) — corner "?"
cmd("ui.create_image", name="Unplayed", parent={"path": T}, color=[0.15,0.16,0.22,0.95], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/Unplayed", anchorMin=[1,1], anchorMax=[1,1], pivot=[1,1], anchoredPosition=[-3,-3], sizeDelta=[30,30])
UQ = txt("Q", f"{T}/Unplayed", "?", 20, (0.85,0.87,0.95,1))
cmd("ui.set_rect", path=UQ, anchorMin=[0,0], anchorMax=[1,1], offsetMin=[0,0], offsetMax=[0,0])
cmd("gameobject.set_active", path=f"{T}/Unplayed", active=False)

cmd("component.set_fields", path=T, type="UnityEngine.UI.Button",
    fields={"m_TargetGraphic": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Image"}})
cmd("component.set_fields", path=T, type="MonsterBattler.Game.UI.TeamIcon", fields={
    "_thumbnail": {"sceneObjectPath": f"{T}/Thumb", "componentType": "UnityEngine.UI.Image"},
    "_hpFill": {"sceneObjectPath": f"{T}/HpBg/HpFill", "componentType": "UnityEngine.UI.Image"},
    "_background": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Image"},
    "_activeOutline": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Outline"},
    "_button": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Button"},
    "_nameText": {"sceneObjectPath": NAME, "componentType": "TMPro.TextMeshProUGUI"},
    "_typePip1": {"sceneObjectPath": TAG1, "componentType": "UnityEngine.UI.Image"},
    "_typePip2": {"sceneObjectPath": TAG2, "componentType": "UnityEngine.UI.Image"},
    "_typeText1": {"sceneObjectPath": TXT1, "componentType": "TMPro.TextMeshProUGUI"},
    "_typeText2": {"sceneObjectPath": TXT2, "componentType": "TMPro.TextMeshProUGUI"},
    "_ghostOverlay": {"sceneObjectPath": f"{T}/Ghost"},
    "_unplayedBadge": {"sceneObjectPath": f"{T}/Unplayed"},
})
cmd("prefab.save_as", path=T, assetPath=PREFAB, connectInstance=False)
cmd("gameobject.delete", path=T)

# ---------- roster containers (same prefab in both) ----------
def make_roster(name, anchorMin, anchorMax, pivot, pos):
    p = f"{SA}/{name}"
    try: cmd("gameobject.delete", path=p)
    except Exception: pass
    cmd("ui.create_image", name=name, parent={"path": SA}, color=[0, 0, 0, 0], raycastTarget=False)
    cmd("ui.set_rect", path=p, anchorMin=anchorMin, anchorMax=anchorMax, pivot=pivot, anchoredPosition=pos, sizeDelta=[-40, 140])
    cmd("component.add", path=p, type="UnityEngine.UI.HorizontalLayoutGroup")
    cmd("component.set_fields", path=p, type="UnityEngine.UI.HorizontalLayoutGroup", fields={
        "m_Spacing": 8, "m_ChildAlignment": 4,  # MiddleCenter
        "m_ChildControlWidth": True, "m_ChildControlHeight": True,
        "m_ChildForceExpandWidth": False, "m_ChildForceExpandHeight": False,
    })
    for i in range(4):  # TeamSize
        cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": p}, name=f"Icon{i}")
    return p

# Opponent: top row (replaces old OppRoster + Chip0-5).
opp = make_roster("OppRoster", [0, 1], [1, 1], [0.5, 1], [0, -200])
# Player: bottom row (replaces Switch0-5).
ply = make_roster("PlayerRoster", [0, 0], [1, 0], [0.5, 0], [0, 14])

# Remove the old loose switch buttons.
for i in range(6):
    try: cmd("gameobject.delete", path=f"{SA}/Switch{i}")
    except Exception: pass

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_playerRosterParent": {"sceneObjectPath": ply, "componentType": "UnityEngine.RectTransform"},
    "_opponentRosterParent": {"sceneObjectPath": opp, "componentType": "UnityEngine.RectTransform"},
})
cmd("scene.save_active")
print("TeamIcon prefab + both rosters built and wired; old switches removed.")
