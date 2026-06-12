#!/usr/bin/env python3
"""Builds the multiplayer scene plumbing: NetworkManager (FishNet + Tugboat + NetBootstrap),
NetBattleMgr (NetworkObject + NetBattleManager), and the home-screen BATTLE ONLINE button
(wired to MenuController._battleOnlineButton). EDIT mode."""
import json, urllib.request, os

_pf = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=120))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

HOME = "BattleUI/SafeArea/MetaRoot/HomePanel"

# 1) NetworkManager + transport + bootstrap
try: cmd("gameobject.delete", path="NetworkManager")
except Exception: pass
cmd("gameobject.create", name="NetworkManager")
cmd("component.add", path="NetworkManager", type="FishNet.Managing.NetworkManager")
cmd("component.add", path="NetworkManager", type="FishNet.Transporting.Tugboat.Tugboat")
cmd("component.add", path="NetworkManager", type="MonsterBattler.Game.Net.NetBootstrap")
cmd("component.set_fields", path="NetworkManager", type="MonsterBattler.Game.Net.NetBootstrap", fields={
    "_battleView": {"sceneObjectPath": "BattleManager", "componentType": "MonsterBattler.Game.BattleView"},
})

# 2) networked match orchestrator (scene NetworkObject)
try: cmd("gameobject.delete", path="NetBattleMgr")
except Exception: pass
cmd("gameobject.create", name="NetBattleMgr")
cmd("component.add", path="NetBattleMgr", type="FishNet.Object.NetworkObject")
cmd("component.add", path="NetBattleMgr", type="MonsterBattler.Game.Net.NetBattleManager")

# 3) BATTLE ONLINE button on the home panel (match the primary button styling)
try: cmd("gameobject.delete", path=f"{HOME}/BattleOnlineBtn")
except Exception: pass
cmd("ui.create_image", name="BattleOnlineBtn", parent={"path": HOME}, color=[1, 1, 1, 1])
P = f"{HOME}/BattleOnlineBtn"
cmd("component.set_fields", path=P, type="UnityEngine.UI.Image", fields={
    "m_Sprite": {"assetPath": "Assets/UI/btn_primary.png", "assetType": "UnityEngine.Sprite"},
    "m_Type": 1,  # sliced
    "m_Color": [0.75, 0.85, 1.0, 1.0],  # cool tint to read as the online variant
})
cmd("ui.set_rect", path=P, anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5],
    anchoredPosition=[0, -50], sizeDelta=[520, 110])
cmd("component.add", path=P, type="UnityEngine.UI.Button")
cmd("component.set_fields", path=P, type="UnityEngine.UI.Button",
    fields={"m_TargetGraphic": {"sceneObjectPath": P, "componentType": "UnityEngine.UI.Image"}})
cmd("ui.create_text", name="Label", parent={"path": P}, text="BATTLE ONLINE", fontSize=44, alignment="Center", color=[1, 1, 1, 1])
cmd("ui.config_text", path=f"{P}/Label", alignment="Center", autoSize=False, fontSize=44, bold=True)
cmd("ui.set_rect", path=f"{P}/Label", anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])

# nudge the existing layout to fit: local battle up, box/summon row down
cmd("ui.set_rect", path=f"{HOME}/BattleBtn", anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5],
    anchoredPosition=[0, 75], sizeDelta=[520, 110])
cmd("ui.set_rect", path=f"{HOME}/BoxBtn", anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5],
    anchoredPosition=[-150, -185], sizeDelta=[240, 100])
cmd("ui.set_rect", path=f"{HOME}/SummonBtn", anchorMin=[0.5, 0.5], anchorMax=[0.5, 0.5], pivot=[0.5, 0.5],
    anchoredPosition=[150, -185], sizeDelta=[240, 100])

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_battleOnlineButton": {"sceneObjectPath": P, "componentType": "UnityEngine.UI.Button"},
})
cmd("scene.save_active")
print("Net plumbing built + wired")
