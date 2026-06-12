#!/usr/bin/env python3
"""Builds the mon-detail / move-equip panel (opened by tapping a mon in the Box) + the MoveCell
prefab, and wires the new MenuController fields. Idempotent. Run in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
ROOT = f"{SA}/MetaRoot"
PANEL = f"{ROOT}/DetailPanel"
PREFAB = "Assets/Prefabs/MoveCell.prefab"


def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")


def img(name, parent, color, raycast=True):
    cmd("ui.create_image", name=name, parent={"path": parent}, color=color, raycastTarget=raycast)
    return f"{parent}/{name}"


def fill(path, amin=(0, 0), amax=(1, 1), omin=(0, 0), omax=(0, 0)):
    cmd("ui.set_rect", path=path, anchorMin=list(amin), anchorMax=list(amax), offsetMin=list(omin), offsetMax=list(omax))


def rect(path, amin, amax, pivot, pos, size):
    cmd("ui.set_rect", path=path, anchorMin=list(amin), anchorMax=list(amax), pivot=list(pivot), anchoredPosition=list(pos), sizeDelta=list(size))


def text(name, parent, s, size, color=(1, 1, 1, 1), bold=True, align="Center"):
    p = f"{parent}/{name}"
    cmd("ui.create_text", name=name, parent={"path": parent}, text=s, fontSize=size, alignment=align, color=list(color))
    cmd("ui.config_text", path=p, alignment=align, autoSize=False, fontSize=size, bold=bold, wrap=False)
    return p


def button(name, parent, label, color, pos, size, fontsize=36):
    p = img(name, parent, color)
    rect(p, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], pos, size)
    cmd("component.add", path=p, type="UnityEngine.UI.Button")
    cmd("component.set_fields", path=p, type="UnityEngine.UI.Button",
        fields={"m_TargetGraphic": {"sceneObjectPath": p, "componentType": "UnityEngine.UI.Image"}})
    lp = text("Label", p, label, fontsize)
    fill(lp)
    return p


# ---------- MoveCell prefab ----------
T = f"{SA}/__MoveCellTemplate"
try: cmd("gameobject.delete", path=T)
except Exception: pass
img("__MoveCellTemplate", SA, [0.16, 0.17, 0.22, 0.95])
rect(T, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], [0, 0], [760, 104])
cmd("component.add", path=T, type="UnityEngine.UI.Button")
cmd("component.set_fields", path=T, type="UnityEngine.UI.Button",
    fields={"m_TargetGraphic": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Image"}})
cmd("component.add", path=T, type="UnityEngine.UI.LayoutElement")
cmd("component.set_fields", path=T, type="UnityEngine.UI.LayoutElement", fields={"m_PreferredWidth": 760, "m_PreferredHeight": 104})
cmd("component.add", path=T, type="MonsterBattler.Game.Meta.MoveCell")
# type accent strip — left 20% of the row, art fading out to the right (drawn first = under text)
cmd("ui.create_image", name="TypeStrip", parent={"path": T}, color=[1, 1, 1, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/TypeStrip", anchorMin=[0, 0], anchorMax=[0.2, 1], offsetMin=[0, 0], offsetMax=[0, 0])
# category icon (top-left), then name / stats
cmd("ui.create_image", name="CatIcon", parent={"path": T}, color=[1, 1, 1, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/CatIcon", anchorMin=[0, 1], anchorMax=[0, 1], pivot=[0, 1], anchoredPosition=[14, -10], sizeDelta=[28, 28])
cmd("component.set_fields", path=f"{T}/CatIcon", type="UnityEngine.UI.Image", fields={"m_PreserveAspect": True})
# top band: name left (after the icon) / stats right
nm = text("Name", T, "Move", 30, align="Left")
cmd("ui.set_rect", path=nm, anchorMin=[0, 0.55], anchorMax=[0.5, 1], offsetMin=[52, 0], offsetMax=[0, -4])
sub = text("Sub", T, "type · 00 BP", 24, (0.72, 0.76, 0.86, 1), bold=False, align="Right")
cmd("ui.set_rect", path=sub, anchorMin=[0.4, 0.55], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[-20, -4])
# bottom band: what the move does (wrapped, smaller) — leaves room for the progress strip below
desc = text("Desc", T, "", 20, (0.62, 0.66, 0.76, 1), bold=False, align="Left")
cmd("ui.set_rect", path=desc, anchorMin=[0, 0.08], anchorMax=[1, 0.55], offsetMin=[20, 2], offsetMax=[-20, 0])
cmd("ui.config_text", path=desc, alignment="TopLeft", autoSize=False, fontSize=20, bold=False, wrap=True)
# unlock progress bar: thin strip along the very bottom (locked moves only; toggled by MoveCell)
cmd("ui.create_image", name="Progress", parent={"path": T}, color=[0.07, 0.08, 0.11, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/Progress", anchorMin=[0, 0], anchorMax=[1, 0.08], offsetMin=[12, 3], offsetMax=[-12, 0])
cmd("ui.create_image", name="Fill", parent={"path": f"{T}/Progress"}, color=[0.36, 0.66, 1.0, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{T}/Progress/Fill", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("component.set_fields", path=f"{T}/Progress/Fill", type="UnityEngine.UI.Image",
    fields={"m_Type": 3, "m_FillMethod": 0, "m_FillOrigin": 0, "m_FillAmount": 0.0})  # Filled / Horizontal / Left
cmd("gameobject.set_active", path=f"{T}/Progress", active=False)
cmd("component.set_fields", path=T, type="MonsterBattler.Game.Meta.MoveCell", fields={
    "_bg": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Image"},
    "_name": {"sceneObjectPath": nm, "componentType": "TMPro.TextMeshProUGUI"},
    "_sub": {"sceneObjectPath": sub, "componentType": "TMPro.TextMeshProUGUI"},
    "_desc": {"sceneObjectPath": desc, "componentType": "TMPro.TextMeshProUGUI"},
    "_button": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Button"},
    "_progressRoot": {"sceneObjectPath": f"{T}/Progress"},
    "_progressFill": {"sceneObjectPath": f"{T}/Progress/Fill", "componentType": "UnityEngine.UI.Image"},
    "_catIcon": {"sceneObjectPath": f"{T}/CatIcon", "componentType": "UnityEngine.UI.Image"},
    "_typeStrip": {"sceneObjectPath": f"{T}/TypeStrip", "componentType": "UnityEngine.UI.Image"},
})
cmd("prefab.save_as", path=T, assetPath=PREFAB, connectInstance=False)
cmd("gameobject.delete", path=T)

# ---------- Detail PREVIEW PANE (docked at the bottom of the Box screen) ----------
BOX = f"{ROOT}/BoxPanel"
# Make room: grid scroll takes the upper band; count + back move to the top.
cmd("ui.set_rect", path=f"{BOX}/Scroll", anchorMin=[0.04, 0.47], anchorMax=[0.96, 0.84], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("ui.set_rect", path=f"{BOX}/BoxTeamCount", anchorMin=[0.5, 1], anchorMax=[0.5, 1], pivot=[0.5, 1], anchoredPosition=[0, -130], sizeDelta=[500, 50])
cmd("ui.set_rect", path=f"{BOX}/BoxBack", anchorMin=[0, 1], anchorMax=[0, 1], pivot=[0, 1], anchoredPosition=[30, -40], sizeDelta=[190, 80])

# Remove any old full-page panel from previous layouts, then build the pane inside BoxPanel.
try: cmd("gameobject.delete", path=f"{ROOT}/DetailPanel")
except Exception: pass
PANEL = f"{BOX}/DetailPanel"
try: cmd("gameobject.delete", path=PANEL)
except Exception: pass
img("DetailPanel", BOX, [0.08, 0.09, 0.14, 0.98])
fill(PANEL, (0.02, 0.015), (0.98, 0.455))

# inactive clone template (same trick as MonCell)
try: cmd("gameobject.delete", path=f"{ROOT}/MoveCellTemplate")
except Exception: pass
cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": ROOT}, name="MoveCellTemplate")
MTMPL = f"{ROOT}/MoveCellTemplate"
cmd("gameobject.set_active", path=MTMPL, active=False)

# Header row: sprite left, name+level beside it, team button right.
sprite = img("Sprite", PANEL, [1, 1, 1, 1], raycast=False)
rect(sprite, [0, 1], [0, 1], [0, 1], [16, -10], [120, 120])
cmd("component.set_fields", path=sprite, type="UnityEngine.UI.Image", fields={"m_PreserveAspect": True})
title = text("Title", PANEL, "Mon", 40, align="Left")
cmd("ui.set_rect", path=title, anchorMin=[0.13, 0.86], anchorMax=[0.62, 1.0], offsetMin=[8, 0], offsetMax=[0, -6])
b_team = button("TeamBtn", PANEL, "Add to Team", [0.22, 0.34, 0.52, 1], [0, 0], [300, 76], fontsize=30)
rect(b_team, [1, 1], [1, 1], [1, 1], [-14, -12], [300, 76])
eq = text("EquipCount", PANEL, "Equipped 4/4 — tap to swap", 24, (0.8, 0.85, 0.95, 1), bold=False)
cmd("ui.set_rect", path=eq, anchorMin=[0.13, 0.76], anchorMax=[0.95, 0.86], offsetMin=[8, 0], offsetMax=[0, 0])

# scrollable move list fills the rest of the pane
viewport = img("Scroll", PANEL, [0, 0, 0, 0.001])
fill(viewport, (0.02, 0.02), (0.98, 0.75))
cmd("component.add", path=viewport, type="UnityEngine.UI.ScrollRect")
cmd("component.add", path=viewport, type="UnityEngine.UI.RectMask2D")
content = img("Content", viewport, [0, 0, 0, 0], raycast=False)
cmd("ui.set_rect", path=content, anchorMin=[0, 1], anchorMax=[1, 1], pivot=[0.5, 1], anchoredPosition=[0, 0], sizeDelta=[0, 0])
cmd("component.add", path=content, type="UnityEngine.UI.VerticalLayoutGroup")
cmd("component.set_fields", path=content, type="UnityEngine.UI.VerticalLayoutGroup", fields={
    "m_Spacing": 8, "m_ChildAlignment": 1,
    "m_ChildControlWidth": False, "m_ChildControlHeight": False,
    "m_ChildForceExpandWidth": False, "m_ChildForceExpandHeight": False})
cmd("component.add", path=content, type="UnityEngine.UI.ContentSizeFitter")
cmd("component.set_fields", path=content, type="UnityEngine.UI.ContentSizeFitter", fields={"m_VerticalFit": 2})
cmd("component.set_fields", path=viewport, type="UnityEngine.UI.ScrollRect", fields={
    "m_Content": {"sceneObjectPath": content, "componentType": "UnityEngine.RectTransform"},
    "m_Viewport": {"sceneObjectPath": viewport, "componentType": "UnityEngine.RectTransform"},
    "m_Horizontal": False, "m_Vertical": True, "m_MovementType": 1, "m_ScrollSensitivity": 30})

# No DetailBack — the pane lives inside the Box screen; BoxBack leaves the Box.
cmd("gameobject.set_active", path=PANEL, active=False)

# ---------- wire MenuController ----------
def tmp(p): return {"sceneObjectPath": p, "componentType": "TMPro.TextMeshProUGUI"}
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_detailPanel": {"sceneObjectPath": PANEL},
    "_detailTitle": tmp(title),
    "_detailImage": {"sceneObjectPath": sprite, "componentType": "UnityEngine.UI.Image"},
    "_detailTeamButton": {"sceneObjectPath": b_team, "componentType": "UnityEngine.UI.Button"},
    "_detailTeamLabel": tmp(f"{b_team}/Label"),
    "_detailEquipCount": tmp(eq),
    "_detailMoveContent": {"sceneObjectPath": content, "componentType": "UnityEngine.RectTransform"},
    "_moveCellPrefab": {"sceneObjectPath": MTMPL, "componentType": "MonsterBattler.Game.Meta.MoveCell"},
})
cmd("scene.save_active")
print("Move-equip detail panel built + wired.")
