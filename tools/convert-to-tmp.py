#!/usr/bin/env python3
"""Converts every legacy UnityEngine.UI.Text under BattleUI to TextMeshProUGUI and re-wires the
serialized references that point at them. Idempotent. Run with Unity open (edit mode)."""
import json, urllib.request

URL = "http://127.0.0.1:17984/"
SA = "BattleUI/SafeArea"
TMP = "TMPro.TextMeshProUGUI"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    req = urllib.request.Request(URL, body, {"content-type": "application/json"})
    r = json.load(urllib.request.urlopen(req, timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def ref(path):
    return {"sceneObjectPath": path, "componentType": TMP}

# 1. Walk BattleUI and convert every Text-bearing GameObject to TMP.
def walk(node, prefix):
    path = node["name"] if not prefix else f"{prefix}/{node['name']}"
    if any(c in ("Text", "UnityEngine.UI.Text") for c in node.get("components", [])):
        yield path
    for c in node.get("children", []):
        yield from walk(c, path)

h = cmd("scene.get_hierarchy")
texts = []
for root in h["roots"]:
    if root["name"] == "BattleUI":
        texts = list(walk(root, ""))
print(f"Converting {len(texts)} Text components to TMP...")
for p in texts:
    cmd("ui.text_to_tmp", path=p)

# 2. Re-wire serialized references (the conversion made new components).
BV = "MonsterBattler.Game.BattleView"
cmd("component.set_fields", path="BattleManager", type=BV, fields={
    "_name0": ref(f"{SA}/LocalPlayer/Name0Text"), "_hp0Text": ref(f"{SA}/LocalPlayer/HP0Text"),
    "_status0": ref(f"{SA}/LocalPlayer/Status0Text"), "_sideText0": ref(f"{SA}/LocalPlayer/SideText0"),
    "_name1": ref(f"{SA}/OtherPlayer/Name1Text"), "_hp1Text": ref(f"{SA}/OtherPlayer/HP1Text"),
    "_status1": ref(f"{SA}/OtherPlayer/Status1Text"), "_sideText1": ref(f"{SA}/OtherPlayer/SideText1"),
    "_turnText": ref(f"{SA}/TurnPanel/TurnText"), "_teraLabel": ref(f"{SA}/TeraButton/Label"),
    "_fieldText": ref(f"{SA}/FieldText"), "_logText": ref(f"{SA}/LogPanel/LogText"),
})

for i in range(4):
    b = f"{SA}/Moves/Move{i}"
    cmd("component.set_fields", path=b, type="MonsterBattler.Game.UI.MoveButton", fields={
        "_nameText": ref(f"{b}/NameText"), "_typeText": ref(f"{b}/TypeText"),
        "_ppText": ref(f"{b}/PPText"), "_descText": ref(f"{b}/DescText"),
    })

for i in range(6):
    cmd("component.set_fields", path=f"{SA}/Switch{i}", type="MonsterBattler.Game.UI.SwitchButton",
        fields={"_nameText": ref(f"{SA}/Switch{i}/NameText")})
    cmd("component.set_fields", path=f"{SA}/OppRoster/Chip{i}", type="MonsterBattler.Game.UI.RosterIcon",
        fields={"_label": ref(f"{SA}/OppRoster/Chip{i}/Label")})

cmd("component.set_fields", path=f"{SA}/InfoPanel", type="MonsterBattler.Game.UI.InfoPanel",
    fields={"_text": ref(f"{SA}/InfoPanel/InfoText")})

cmd("scene.save_active")
print("All UI text converted to TMP and re-wired.")
