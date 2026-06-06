#!/usr/bin/env python3
"""Creates Assets/Prefabs/TypeBadge.prefab — a small colored type chip (Image + TypeBadge component
+ centered Label). SetType(MonType) recolors/relabels it. Used by the info panel. Run with Unity open."""
import os, json, urllib.request

URL = "http://127.0.0.1:17984/"
PREFAB = "Assets/Prefabs/TypeBadge.prefab"
TEMPLATE = "BattleUI/SafeArea/__TypeBadgeTemplate"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    req = urllib.request.Request(URL, body, {"content-type": "application/json"})
    r = json.load(urllib.request.urlopen(req, timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

os.makedirs("/Users/joshuawilde/MonsterBattler/MonsterBattler/Assets/Prefabs", exist_ok=True)
cmd("meta.refresh_assets")

# Build a template in the scene, save it as a prefab asset, then remove the template.
try: cmd("gameobject.delete", path=TEMPLATE)
except Exception: pass

cmd("ui.create_image", name="__TypeBadgeTemplate", parent={"path": "BattleUI/SafeArea"}, color=[0.5, 0.5, 0.5, 1])
cmd("ui.set_rect", path=TEMPLATE, anchorMin=[0, 1], anchorMax=[0, 1], pivot=[0, 1],
    anchoredPosition=[0, 0], sizeDelta=[96, 36])
cmd("component.add", path=TEMPLATE, type="MonsterBattler.Game.UI.TypeBadge")

cmd("ui.create_text", name="Label", parent={"path": TEMPLATE},
    text="TYPE", fontSize=20, alignment="Center", color=[1, 1, 1, 1])
cmd("ui.set_rect", path=f"{TEMPLATE}/Label", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[2, 0], offsetMax=[-2, 0])
cmd("ui.config_text", path=f"{TEMPLATE}/Label", alignment="Center", autoSize=True, fontSize=20, fontSizeMin=9, bold=True)

cmd("component.set_fields", path=TEMPLATE, type="MonsterBattler.Game.UI.TypeBadge", fields={
    "_background": {"sceneObjectPath": TEMPLATE, "componentType": "UnityEngine.UI.Image"},
    "_label": {"sceneObjectPath": f"{TEMPLATE}/Label", "componentType": "TMPro.TextMeshProUGUI"},
})

cmd("prefab.save_as", path=TEMPLATE, assetPath=PREFAB, connectInstance=False)
cmd("gameobject.delete", path=TEMPLATE)  # asset persists; remove the scene template
cmd("scene.save_active")
print(f"TypeBadge prefab saved to {PREFAB}.")
