#!/usr/bin/env python3
"""Replaces the <mark>-based boost text with real chip prefabs in HorizontalLayoutGroup rows:
creates Assets/Prefabs/StatChip.prefab (solid dark chip, styled, editable) and BoostRow0/1 HLG
containers under each nameplate, then wires BattleView. Run with Unity open in EDIT mode."""
import os, json, urllib.request

URL = "http://127.0.0.1:17984/"
SA = "BattleUI/SafeArea"
CHIP = "Assets/Prefabs/StatChip.prefab"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

os.makedirs("/Users/joshuawilde/MonsterBattler/MonsterBattler/Assets/Prefabs", exist_ok=True)
cmd("meta.refresh_assets")

# --- StatChip prefab: a solid dark chip with a centered label (editable style) ---
TMPL = f"{SA}/__StatChipTemplate"
try: cmd("gameobject.delete", path=TMPL)
except Exception: pass
cmd("ui.create_image", name="__StatChipTemplate", parent={"path": SA}, color=[0.06, 0.07, 0.10, 1])
cmd("ui.set_rect", path=TMPL, anchorMin=[0, 1], anchorMax=[0, 1], pivot=[0, 1], anchoredPosition=[0, 0], sizeDelta=[120, 34])
cmd("component.add", path=TMPL, type="MonsterBattler.Game.UI.TypeBadge")
cmd("ui.create_text", name="Label", parent={"path": TMPL}, text="Atk ×1.5", fontSize=19, alignment="Center", color=[1, 1, 1, 1])
cmd("ui.set_rect", path=f"{TMPL}/Label", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[6, 1], offsetMax=[-6, -1])
cmd("ui.config_text", path=f"{TMPL}/Label", alignment="Center", autoSize=True, fontSize=19, fontSizeMin=10, bold=True)
cmd("component.set_fields", path=TMPL, type="MonsterBattler.Game.UI.TypeBadge", fields={
    "_background": {"sceneObjectPath": TMPL, "componentType": "UnityEngine.UI.Image"},
    "_label": {"sceneObjectPath": f"{TMPL}/Label", "componentType": "TMPro.TextMeshProUGUI"},
})
cmd("prefab.save_as", path=TMPL, assetPath=CHIP, connectInstance=False)
cmd("gameobject.delete", path=TMPL)

# --- BoostRow HLG containers (replace the old BoostText TMPs) ---
def make_row(parent, name, anchor, pivot, pos, child_align):
    p = f"{parent}/{name}"
    for old in (f"{parent}/BoostText0", f"{parent}/BoostText1", p):
        try: cmd("gameobject.delete", path=old)
        except Exception: pass
    cmd("ui.create_image", name=name, parent={"path": parent}, color=[0, 0, 0, 0], raycastTarget=False)
    cmd("ui.set_rect", path=p, anchorMin=anchor, anchorMax=anchor, pivot=pivot, anchoredPosition=pos, sizeDelta=[640, 40])
    cmd("component.add", path=p, type="UnityEngine.UI.HorizontalLayoutGroup")
    cmd("component.set_fields", path=p, type="UnityEngine.UI.HorizontalLayoutGroup", fields={
        "m_Spacing": 6, "m_ChildAlignment": child_align,
        "m_ChildControlWidth": False, "m_ChildControlHeight": False,
        "m_ChildForceExpandWidth": False, "m_ChildForceExpandHeight": False,
    })
    return p

make_row(f"{SA}/LocalPlayer", "BoostRow0", [0, 1], [0, 1], [495, -44], 3)   # 3 = MiddleLeft
make_row(f"{SA}/OtherPlayer", "BoostRow1", [1, 1], [1, 1], [-495, -44], 5)  # 5 = MiddleRight

# --- wire BattleView ---
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_boostRow0": {"sceneObjectPath": f"{SA}/LocalPlayer/BoostRow0", "componentType": "UnityEngine.RectTransform"},
    "_boostRow1": {"sceneObjectPath": f"{SA}/OtherPlayer/BoostRow1", "componentType": "UnityEngine.RectTransform"},
    "_statChipPrefab": {"assetPath": CHIP, "assetType": "MonsterBattler.Game.UI.TypeBadge"},
})
cmd("scene.save_active")
print("StatChip prefab + BoostRow HLG containers built and wired.")
