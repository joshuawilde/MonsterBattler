#!/usr/bin/env python3
"""Builds the meta-loop menu overlay (Home / Box / Summon) on top of the battle UI, plus a MonCell
prefab for the collection grid, and wires MenuController on BattleManager. Run in EDIT mode."""
import json, urllib.request

URL = "http://127.0.0.1:17984/"
SA = "BattleUI/SafeArea"
ROOT = f"{SA}/MetaRoot"
PREFAB = "Assets/Prefabs/MonCell.prefab"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def img(name, parent, color, raycast=True):
    cmd("ui.create_image", name=name, parent={"path": parent}, color=color, raycastTarget=raycast)
    return f"{parent}/{name}"

def fill(path, amin=(0,0), amax=(1,1), omin=(0,0), omax=(0,0)):
    cmd("ui.set_rect", path=path, anchorMin=list(amin), anchorMax=list(amax), offsetMin=list(omin), offsetMax=list(omax))

def rect(path, amin, amax, pivot, pos, size):
    cmd("ui.set_rect", path=path, anchorMin=list(amin), anchorMax=list(amax), pivot=list(pivot), anchoredPosition=list(pos), sizeDelta=list(size))

def text(name, parent, s, size, color=(1,1,1,1), bold=True):
    p = f"{parent}/{name}"
    cmd("ui.create_text", name=name, parent={"path": parent}, text=s, fontSize=size, alignment="Center", color=list(color))
    cmd("ui.config_text", path=p, alignment="Center", autoSize=False, fontSize=size, bold=bold, wrap=True)
    return p

def button(name, parent, label, color, pos, size, fontsize=42):
    p = img(name, parent, color)
    rect(p, [0.5,0.5],[0.5,0.5],[0.5,0.5], pos, size)
    cmd("component.add", path=p, type="UnityEngine.UI.Button")
    cmd("component.set_fields", path=p, type="UnityEngine.UI.Button",
        fields={"m_TargetGraphic": {"sceneObjectPath": p, "componentType": "UnityEngine.UI.Image"}})
    lp = text("Label", p, label, fontsize)
    fill(lp)
    return p

# ---------- MonCell prefab ----------
T = f"{SA}/__MonCellTemplate"
try: cmd("gameobject.delete", path=T)
except Exception: pass
img("__MonCellTemplate", SA, [0,0,0,0.001])  # transparent raycast root
rect(T, [0.5,0.5],[0.5,0.5],[0.5,0.5],[0,0],[150,170])
cmd("component.add", path=T, type="UnityEngine.UI.Button")
cmd("component.set_fields", path=T, type="UnityEngine.UI.Button",
    fields={"m_TargetGraphic": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Image"}})
cmd("component.add", path=T, type="UnityEngine.UI.LayoutElement")
cmd("component.set_fields", path=T, type="UnityEngine.UI.LayoutElement", fields={"m_PreferredWidth": 150, "m_PreferredHeight": 170})
cmd("component.add", path=T, type="MonsterBattler.Game.Meta.MonCell")
# selected frame (drawn first, slightly larger → only its edge shows past the card) toggled
sel = img("SelectFrame", T, [1, 0.86, 0.30, 1], raycast=False)
fill(sel, (0,0),(1,1),(-6,-6),(6,6))
cmd("gameobject.set_active", path=sel, active=False)
# rarity-tinted card body (on top of the frame's center)
card = img("Card", T, [0.16,0.17,0.22,0.95], raycast=False)
fill(card)
# thumb
th = img("Thumb", T, [1,1,1,1], raycast=False)
rect(th, [0.5,1],[0.5,1],[0.5,1],[0,-6],[138,118])
cmd("component.set_fields", path=th, type="UnityEngine.UI.Image", fields={"m_PreserveAspect": True})
# name
nm = text("Name", T, "Name", 26)
rect(nm, [0,0],[1,0],[0.5,0],[0,6],[0,40])
cmd("component.set_fields", path=T, type="MonsterBattler.Game.Meta.MonCell", fields={
    "_thumb": {"sceneObjectPath": th, "componentType": "UnityEngine.UI.Image"},
    "_name": {"sceneObjectPath": nm, "componentType": "TMPro.TextMeshProUGUI"},
    "_selectedOutline": {"sceneObjectPath": sel},
    "_button": {"sceneObjectPath": T, "componentType": "UnityEngine.UI.Button"},
    "_cardBg": {"sceneObjectPath": card, "componentType": "UnityEngine.UI.Image"},
})
cmd("prefab.save_as", path=T, assetPath=PREFAB, connectInstance=False)
cmd("gameobject.delete", path=T)

# ---------- MetaRoot (opaque overlay, last sibling = on top) ----------
try: cmd("gameobject.delete", path=ROOT)
except Exception: pass
img("MetaRoot", SA, [0.05,0.06,0.10,1.0])
fill(ROOT)

# Inactive MonCell instance used as the clone template (avoids prefab-asset field wiring).
cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": ROOT}, name="CellTemplate")
CELL_TMPL = f"{ROOT}/CellTemplate"
cmd("gameobject.set_active", path=CELL_TMPL, active=False)

# ===== Home panel =====
home = img("HomePanel", ROOT, [0,0,0,0])
fill(home)
title = text("Title", home, "MonsterBattler", 72)
rect(title, [0.5,1],[0.5,1],[0.5,1],[0,-70],[900,100])
coins = text("Coins", home, "0 ◈", 44, (1,0.9,0.4,1))
rect(coins, [0.5,1],[0.5,1],[0.5,1],[0,-170],[600,60])
team = text("Team", home, "Team 0/6", 34, (0.8,0.85,0.95,1))
rect(team, [0.5,1],[0.5,1],[0.5,1],[0,-230],[700,50])
b_battle = button("BattleBtn", home, "BATTLE", [0.20,0.55,0.30,1], [0,40], [520,140], 56)
b_box = button("BoxBtn", home, "Box", [0.22,0.34,0.52,1], [-150,-150], [240,100])
b_summon = button("SummonBtn", home, "Summon", [0.45,0.30,0.55,1], [150,-150], [240,100])

# ===== Box panel =====
box = img("BoxPanel", ROOT, [0,0,0,0])
fill(box)
text("BoxTitle", box, "Your Box", 56); rect(f"{box}/BoxTitle", [0.5,1],[0.5,1],[0.5,1],[0,-60],[700,80])
# scroll view (viewport masks; content holds the grid and grows vertically)
viewport = img("Scroll", box, [0,0,0,0.001])
fill(viewport, (0.04,0.12),(0.96,0.86))
cmd("component.add", path=viewport, type="UnityEngine.UI.ScrollRect")
cmd("component.add", path=viewport, type="UnityEngine.UI.RectMask2D")
grid = img("Content", viewport, [0,0,0,0], raycast=False)
# content anchored to top, height driven by ContentSizeFitter
cmd("ui.set_rect", path=grid, anchorMin=[0,1], anchorMax=[1,1], pivot=[0.5,1], anchoredPosition=[0,0], sizeDelta=[0,0])
cmd("component.add", path=grid, type="UnityEngine.UI.GridLayoutGroup")
cmd("component.set_fields", path=grid, type="UnityEngine.UI.GridLayoutGroup", fields={
    "m_CellSize": [150,170], "m_Spacing": [12,12], "m_Constraint": 1, "m_ConstraintCount": 5, "m_ChildAlignment": 1})
cmd("component.add", path=grid, type="UnityEngine.UI.ContentSizeFitter")
cmd("component.set_fields", path=grid, type="UnityEngine.UI.ContentSizeFitter",
    fields={"m_VerticalFit": 2})  # PreferredSize
cmd("component.set_fields", path=viewport, type="UnityEngine.UI.ScrollRect", fields={
    "m_Content": {"sceneObjectPath": grid, "componentType": "UnityEngine.RectTransform"},
    "m_Viewport": {"sceneObjectPath": viewport, "componentType": "UnityEngine.RectTransform"},
    "m_Horizontal": False, "m_Vertical": True, "m_MovementType": 1, "m_ScrollSensitivity": 30})
boxcount = text("BoxTeamCount", box, "Team 0/6", 34, (0.8,0.85,0.95,1))
rect(boxcount, [0.5,0],[0.5,0],[0.5,0],[0,70],[500,50])
b_boxback = button("BoxBack", box, "Back", [0.30,0.32,0.40,1], [0,160], [220,90])
cmd("gameobject.set_active", path=box, active=False)

# ===== Summon panel =====
summ = img("SummonPanel", ROOT, [0,0,0,0])
fill(summ)
text("SummonTitle", summ, "Summon", 56); rect(f"{summ}/SummonTitle", [0.5,1],[0.5,1],[0.5,1],[0,-60],[700,80])
scoins = text("SummonCoins", summ, "0 ◈", 40, (1,0.9,0.4,1))
rect(scoins, [0.5,1],[0.5,1],[0.5,1],[0,-150],[500,55])
res = img("ResultImage", summ, [1,1,1,1], raycast=False)
rect(res, [0.5,0.5],[0.5,0.5],[0.5,0.5],[0,60],[260,260])
cmd("component.set_fields", path=res, type="UnityEngine.UI.Image", fields={"m_PreserveAspect": True})
cmd("gameobject.set_active", path=res, active=False)
sname = text("ResultName", summ, "", 40); rect(sname, [0.5,0.5],[0.5,0.5],[0.5,0.5],[0,-110],[700,55])
sstatus = text("Status", summ, "Summon — 100 ◈", 30, (0.8,0.85,0.95,1)); rect(sstatus, [0.5,0.5],[0.5,0.5],[0.5,0.5],[0,-160],[700,45])
b_pull = button("PullBtn", summ, "SUMMON (100)", [0.45,0.30,0.55,1], [0,-260], [460,120], 44)
b_summback = button("SummonBack", summ, "Back", [0.30,0.32,0.40,1], [0,-400], [220,90])
cmd("gameobject.set_active", path=summ, active=False)

# ---------- MenuController on BattleManager ----------
cmd("component.add", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController")
def go(p): return {"sceneObjectPath": p}
def tmp(p): return {"sceneObjectPath": p, "componentType": "TMPro.TextMeshProUGUI"}
def btn(p): return {"sceneObjectPath": p, "componentType": "UnityEngine.UI.Button"}
def imgref(p): return {"sceneObjectPath": p, "componentType": "UnityEngine.UI.Image"}
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_menuRoot": go(ROOT), "_homePanel": go(home), "_boxPanel": go(box), "_summonPanel": go(summ),
    "_homeCoins": tmp(coins), "_homeTeam": tmp(team),
    "_battleButton": btn(b_battle), "_boxButton": btn(b_box), "_summonButton": btn(b_summon),
    "_boxContent": go(grid), "_monCellPrefab": {"sceneObjectPath": CELL_TMPL, "componentType": "MonsterBattler.Game.Meta.MonCell"},
    "_boxTeamCount": tmp(boxcount), "_boxBackButton": btn(b_boxback),
    "_pullButton": btn(b_pull), "_summonImage": imgref(res), "_summonName": tmp(sname),
    "_summonStatus": tmp(sstatus), "_summonCoins": tmp(scoins), "_summonBackButton": btn(b_summback),
})
cmd("scene.save_active")
print("Meta menu built and wired.")
