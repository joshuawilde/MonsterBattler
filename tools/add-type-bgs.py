#!/usr/bin/env python3
"""Adds a type-colored backdrop Image behind every mon thumbnail: MonCell prefab (Box grid),
TeamIcon prefab (battle rosters), the Box preview sprite, and the summon result. Sprites are
assigned at runtime by TypeBgSprites. Run in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
ROOT = f"{SA}/MetaRoot"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def children_of(path):
    """Children via scene.get_hierarchy — scene.get_object does NOT report children."""
    h = cmd("scene.get_hierarchy")
    target = path.split("/")[-1]
    def find(n, p=""):
        q = f"{p}/{n.get('name','')}" if p else n.get("name", "")
        if q == path or (q.endswith(target) and q.endswith(path)): return n
        for c in n.get("children", []):
            r = find(c, q)
            if r: return r
    roots = h.get("roots", h if isinstance(h, list) else [h])
    for r in roots:
        n = find(r)
        if n: return n.get("children", [])
    return []

def sib_index(parent, child_name):
    for i, c in enumerate(children_of(parent)):
        if c.get("name") == child_name: return i
    return -1

# ---------- MonCell prefab: TypeBg behind the Thumb ----------
PREFAB = "Assets/Prefabs/MonCell.prefab"
T = f"{SA}/__MonCellEdit"
try: cmd("gameobject.delete", path=T)
except Exception: pass
cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": SA}, name="__MonCellEdit")
try: cmd("gameobject.delete", path=f"{T}/TypeBg")
except Exception: pass
cmd("ui.create_image", name="TypeBg", parent={"path": T}, color=[1, 1, 1, 1], raycastTarget=False)
# full-cell backdrop (the type plate IS the card background; rarity lives in the name color)
cmd("ui.set_rect", path=f"{T}/TypeBg", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
idx = sib_index(T, "Thumb")
cmd("gameobject.set_sibling_index", path=f"{T}/TypeBg", index=max(0, idx))  # just before Thumb
cmd("component.set_fields", path=T, type="MonsterBattler.Game.Meta.MonCell", fields={
    "_typeBg": {"sceneObjectPath": f"{T}/TypeBg", "componentType": "UnityEngine.UI.Image"},
})
cmd("prefab.save_as", path=T, assetPath=PREFAB, connectInstance=False)
cmd("gameobject.delete", path=T)
# refresh the Box clone template
try: cmd("gameobject.delete", path=f"{ROOT}/CellTemplate")
except Exception: pass
cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": ROOT}, name="CellTemplate")
cmd("gameobject.set_active", path=f"{ROOT}/CellTemplate", active=False)
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_monCellPrefab": {"sceneObjectPath": f"{ROOT}/CellTemplate", "componentType": "MonsterBattler.Game.Meta.MonCell"},
})

# ---------- TeamIcon prefab: TypeBg behind the Thumb ----------
PREFAB2 = "Assets/Prefabs/TeamIcon.prefab"
T2 = f"{SA}/__TeamIconEdit"
try: cmd("gameobject.delete", path=T2)
except Exception: pass
cmd("prefab.instantiate", assetPath=PREFAB2, parent={"path": SA}, name="__TeamIconEdit")
try: cmd("gameobject.delete", path=f"{T2}/TypeBg")
except Exception: pass
cmd("ui.create_image", name="TypeBg", parent={"path": T2}, color=[1, 1, 1, 1], raycastTarget=False)
# same band as the Thumb (anchors 0..1 x, 0.33..1 y, insets 4/2)
cmd("ui.set_rect", path=f"{T2}/TypeBg", anchorMin=[0, 0.33], anchorMax=[1, 1], offsetMin=[4, 0], offsetMax=[-4, -2])
idx = sib_index(T2, "Thumb")
cmd("gameobject.set_sibling_index", path=f"{T2}/TypeBg", index=max(0, idx))
cmd("component.set_fields", path=T2, type="MonsterBattler.Game.UI.TeamIcon", fields={
    "_typeBg": {"sceneObjectPath": f"{T2}/TypeBg", "componentType": "UnityEngine.UI.Image"},
})
cmd("prefab.save_as", path=T2, assetPath=PREFAB2, connectInstance=False)
cmd("gameobject.delete", path=T2)
# recreate roster icons so they pick up the new child + wiring (4 = TeamSize)
for roster in ("PlayerRoster", "OppRoster"):
    p = f"{SA}/{roster}"
    for k in children_of(p):
        try: cmd("gameobject.delete", id=k.get("id"))
        except Exception: pass
    for i in range(4):
        cmd("prefab.instantiate", assetPath=PREFAB2, parent={"path": p}, name=f"Icon{i}")

# ---------- Box preview pane: plate behind the Sprite ----------
PANEL = f"{ROOT}/BoxPanel/DetailPanel"
try: cmd("gameobject.delete", path=f"{PANEL}/TypeBg")
except Exception: pass
cmd("ui.create_image", name="TypeBg", parent={"path": PANEL}, color=[1, 1, 1, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{PANEL}/TypeBg", anchorMin=[0, 1], anchorMax=[0, 1], pivot=[0, 1], anchoredPosition=[16, -10], sizeDelta=[120, 120])
idx = sib_index(PANEL, "Sprite")
cmd("gameobject.set_sibling_index", path=f"{PANEL}/TypeBg", index=max(0, idx))

# ---------- Summon result: plate behind ResultImage ----------
SUMM = f"{ROOT}/SummonPanel"
try: cmd("gameobject.delete", path=f"{SUMM}/TypeBg")
except Exception: pass
cmd("ui.create_image", name="TypeBg", parent={"path": SUMM}, color=[1, 1, 1, 1], raycastTarget=False)
cmd("ui.set_rect", path=f"{SUMM}/TypeBg", anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5], anchoredPosition=[0, 60], sizeDelta=[280, 280])
idx = sib_index(SUMM, "ResultImage")
cmd("gameobject.set_sibling_index", path=f"{SUMM}/TypeBg", index=max(0, idx))

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_detailTypeBg": {"sceneObjectPath": f"{PANEL}/TypeBg", "componentType": "UnityEngine.UI.Image"},
    "_summonTypeBg": {"sceneObjectPath": f"{SUMM}/TypeBg", "componentType": "UnityEngine.UI.Image"},
})
cmd("scene.save_active")
print("Type backdrops added to MonCell, TeamIcon, Box preview, and Summon.")
