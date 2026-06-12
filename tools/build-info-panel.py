#!/usr/bin/env python3
"""Restructures the InfoPanel into stacked sections (a VerticalLayoutGroup): Header (text),
TypesRow (HLG of type-badge chips), Matchup (5 EffRow HLG rows of chips), Body (text). Types and
effectiveness become real TypeBadge chips. Wires InfoPanel. Run with Unity open in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
IP = "BattleUI/SafeArea/InfoPanel"
TYPEBADGE = "Assets/Prefabs/TypeBadge.prefab"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def add_layout(path, kind, **fields):
    cmd("component.add", path=path, type=f"UnityEngine.UI.{kind}")
    cmd("component.set_fields", path=path, type=f"UnityEngine.UI.{kind}", fields=fields)

def container(parent, name, raycast=False):
    p = f"{parent}/{name}"
    try: cmd("gameobject.delete", path=p)
    except Exception: pass
    cmd("ui.create_image", name=name, parent={"path": parent}, color=[0, 0, 0, 0], raycastTarget=raycast)
    return p

def text(parent, name, font, align="Left", color=(0.92, 0.93, 0.96, 1)):
    p = f"{parent}/{name}"
    cmd("ui.create_text", name=name, parent={"path": parent}, text="", fontSize=font, alignment=align, color=list(color))
    cmd("ui.config_text", path=p, alignment=align, autoSize=False, fontSize=font, wrap=True)
    return p

# Clean prior structure (old InfoText + any prior Content).
for old in ("InfoText", "Content"):
    try: cmd("gameobject.delete", path=f"{IP}/{old}")
    except Exception: pass

# --- Content: a VerticalLayoutGroup filling the panel above the buttons ---
content = container(IP, "Content")
cmd("ui.set_rect", path=content, anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[22, 96], offsetMax=[-22, -18])
add_layout(content, "VerticalLayoutGroup", m_Spacing=8, m_ChildAlignment=0,  # UpperLeft
           m_ChildControlWidth=True, m_ChildControlHeight=True,
           m_ChildForceExpandWidth=True, m_ChildForceExpandHeight=False)

header = text(content, "Header", 30)
cmd("ui.config_text", path=header, autoSize=False, fontSize=30, wrap=False)

types_row = container(content, "TypesRow")  # created 2nd → sits below header
add_layout(types_row, "HorizontalLayoutGroup", m_Spacing=6, m_ChildAlignment=3,  # MiddleLeft
           m_ChildControlWidth=False, m_ChildControlHeight=False,
           m_ChildForceExpandWidth=False, m_ChildForceExpandHeight=False)
cmd("component.add", path=types_row, type="UnityEngine.UI.LayoutElement")
cmd("component.set_fields", path=types_row, type="UnityEngine.UI.LayoutElement", fields={"m_MinHeight": 34, "m_PreferredHeight": 34})

# Order: type → hp%+stats → eff → ability+item+moves.
top_body = text(content, "TopBody", 24)

matchup = container(content, "Matchup")
add_layout(matchup, "VerticalLayoutGroup", m_Spacing=4, m_ChildAlignment=0,
           m_ChildControlWidth=True, m_ChildControlHeight=True,
           m_ChildForceExpandWidth=True, m_ChildForceExpandHeight=False)

# 5 EffRow children (x4, x2, x½, x¼, x0).
for i in range(5):
    row = f"{matchup}/EffRow{i}"
    cmd("ui.create_image", name=f"EffRow{i}", parent={"path": matchup}, color=[0, 0, 0, 0], raycastTarget=False)
    add_layout(row, "HorizontalLayoutGroup", m_Spacing=6, m_ChildAlignment=3,
               m_ChildControlWidth=False, m_ChildControlHeight=False,
               m_ChildForceExpandWidth=False, m_ChildForceExpandHeight=False)
    cmd("component.add", path=row, type="UnityEngine.UI.LayoutElement")
    cmd("component.set_fields", path=row, type="UnityEngine.UI.LayoutElement", fields={"m_MinHeight": 34, "m_PreferredHeight": 34})
    cmd("component.add", path=row, type="MonsterBattler.Game.UI.EffRow")
    # label child (child 0)
    cmd("ui.create_text", name="Label", parent={"path": row}, text="x2", fontSize=22, alignment="Left", color=[0.92, 0.93, 0.96, 1])
    cmd("ui.set_rect", path=f"{row}/Label", anchorMin=[0, 0], anchorMax=[0, 1], pivot=[0, 0.5], anchoredPosition=[0, 0], sizeDelta=[52, 0])
    cmd("component.add", path=f"{row}/Label", type="UnityEngine.UI.LayoutElement")
    cmd("component.set_fields", path=f"{row}/Label", type="UnityEngine.UI.LayoutElement", fields={"m_MinWidth": 52, "m_PreferredWidth": 52})
    cmd("ui.config_text", path=f"{row}/Label", alignment="Left", autoSize=False, fontSize=22, bold=True, wrap=False)
    cmd("component.set_fields", path=row, type="MonsterBattler.Game.UI.EffRow",
        fields={"_label": {"sceneObjectPath": f"{row}/Label", "componentType": "TMPro.TextMeshProUGUI"}})

bottom_body = text(content, "BottomBody", 24)

# --- wire InfoPanel ---
cmd("component.set_fields", path=IP, type="MonsterBattler.Game.UI.InfoPanel", fields={
    "_header": {"sceneObjectPath": header, "componentType": "TMPro.TextMeshProUGUI"},
    "_topBody": {"sceneObjectPath": top_body, "componentType": "TMPro.TextMeshProUGUI"},
    "_bottomBody": {"sceneObjectPath": bottom_body, "componentType": "TMPro.TextMeshProUGUI"},
    "_typesRow": {"sceneObjectPath": types_row, "componentType": "UnityEngine.RectTransform"},
    "_matchupParent": {"sceneObjectPath": matchup, "componentType": "UnityEngine.RectTransform"},
    "_typeBadgePrefab": {"assetPath": TYPEBADGE, "assetType": "MonsterBattler.Game.UI.TypeBadge"},
    "_closeButton": {"sceneObjectPath": f"{IP}/CloseButton", "componentType": "UnityEngine.UI.Button"},
    "_swapButton": {"sceneObjectPath": f"{IP}/SwapButton", "componentType": "UnityEngine.UI.Button"},
})
cmd("scene.save_active")
print("InfoPanel restructured with type/effectiveness chips and wired.")
