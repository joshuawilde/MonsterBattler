#!/usr/bin/env python3
"""Builds the move-button prefab: name (top, bold, centered) / power·accuracy + description (middle) /
type (bottom-left) + PP (bottom-right). Saves Assets/Prefabs/MoveButton.prefab and rebuilds Move0-3
as instances, re-wiring BattleView. Run with Unity open in edit mode."""
import os, json, urllib.request

URL = "http://127.0.0.1:17984/"
MOVES = "BattleUI/SafeArea/Moves"
PREFAB = "Assets/Prefabs/MoveButton.prefab"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    req = urllib.request.Request(URL, body, {"content-type": "application/json"})
    r = json.load(urllib.request.urlopen(req, timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

os.makedirs("/Users/joshuawilde/MonsterBattler/MonsterBattler/Assets/Prefabs", exist_ok=True)
cmd("meta.refresh_assets")
m0 = f"{MOVES}/Move0"
INK = [0.13, 0.13, 0.15, 1]

# Name — top, bold, centered.
cmd("ui.set_rect", path=f"{m0}/NameText",
    anchorMin=[0, 0.70], anchorMax=[1, 1], offsetMin=[10, 0], offsetMax=[-10, -2])
cmd("ui.config_text", path=f"{m0}/NameText", alignment="Center", color=INK,
    autoSize=True, fontSize=30, fontSizeMin=14, bold=True, wrap=True)

# Description — middle, wrapping, best-fit so long text shrinks.
desc = f"{m0}/DescText"
try: cmd("gameobject.delete", path=desc)
except Exception: pass
cmd("ui.create_text", name="DescText", parent={"path": m0},
    text="", fontSize=20, alignment="Top", color=INK, bestFit=True)
cmd("ui.set_rect", path=desc, anchorMin=[0, 0.20], anchorMax=[1, 0.70], offsetMin=[12, 2], offsetMax=[-12, 2])
cmd("ui.config_text", path=desc, alignment="Top", autoSize=True, fontSize=20, fontSizeMin=11, wrap=True)

# Type (bottom-left) / PP (bottom-right).
cmd("ui.set_rect", path=f"{m0}/TypeText", anchorMin=[0, 0], anchorMax=[0.6, 0.20], offsetMin=[16, 2], offsetMax=[-2, -2])
cmd("ui.config_text", path=f"{m0}/TypeText", alignment="Left", color=INK, autoSize=False, fontSize=22, wrap=False)
cmd("ui.set_rect", path=f"{m0}/PPText", anchorMin=[0.4, 0], anchorMax=[1, 0.20], offsetMin=[2, 2], offsetMax=[-16, -2])
cmd("ui.config_text", path=f"{m0}/PPText", alignment="Right", autoSize=False, fontSize=22, wrap=False)

# Wire the new DescText into the MoveButton, then save + propagate.
cmd("component.set_fields", path=m0, type="MonsterBattler.Game.UI.MoveButton",
    fields={"_descText": {"sceneObjectPath": desc, "componentType": "TMPro.TextMeshProUGUI"}})

cmd("prefab.save_as", path=m0, assetPath=PREFAB, connectInstance=True)

for i in (1, 2, 3):
    try: cmd("gameobject.delete", path=f"{MOVES}/Move{i}")
    except Exception: pass
    cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": MOVES}, name=f"Move{i}")

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    f"_move{i}": {"sceneObjectPath": f"{MOVES}/Move{i}", "componentType": "MonsterBattler.Game.UI.MoveButton"}
    for i in range(4)
})

cmd("scene.save_active")
print("Move-button prefab rebuilt with description; Move0-3 re-instantiated + re-wired.")
