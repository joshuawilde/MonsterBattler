#!/usr/bin/env python3
"""Wraps the BattleUI content in a full-screen SafeArea container (so the UI insets away from
notches / home indicator). Re-runnable: un-nests any existing SafeArea first. Run with Unity open."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
CANVAS = "BattleUI"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    req = urllib.request.Request(URL, body, {"content-type": "application/json"})
    r = json.load(urllib.request.urlopen(req, timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def find(node, name):
    if node["name"] == name:
        return node
    for c in node.get("children", []):
        r = find(c, name)
        if r:
            return r
    return None

def battleui_node():
    h = cmd("scene.get_hierarchy")
    for root in h["roots"]:
        n = find(root, CANVAS)
        if n:
            return n
    raise RuntimeError("BattleUI canvas not found")

# 1. If a SafeArea already exists, lift its children back under the canvas, then remove it.
canvas = battleui_node()
existing = next((c for c in canvas.get("children", []) if c["name"] == "SafeArea"), None)
if existing:
    for child in existing.get("children", []):
        cmd("gameobject.reparent", path=f"{CANVAS}/SafeArea/{child['name']}",
            parent={"path": CANVAS}, worldPositionStays=False)
    cmd("gameobject.delete", path=f"{CANVAS}/SafeArea")

# 2. The content to wrap = every direct child of the canvas (in order), preserving draw order.
content = [c["name"] for c in battleui_node().get("children", [])]

# 3. Create the SafeArea container, full-screen stretch, with the SafeArea component.
cmd("ui.create_image", name="SafeArea", parent={"path": CANVAS}, color=[0, 0, 0, 0], raycastTarget=False)
cmd("ui.set_rect", path=f"{CANVAS}/SafeArea",
    anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("component.add", path=f"{CANVAS}/SafeArea", type="MonsterBattler.Game.UI.SafeArea")

# 4. Move the content under it, keeping local layout (parent is the same size, so nothing shifts).
for name in content:
    cmd("gameobject.reparent", path=f"{CANVAS}/{name}", parent={"path": f"{CANVAS}/SafeArea"},
        worldPositionStays=False)

cmd("scene.save_active")
print(f"SafeArea wraps {len(content)} UI elements: {', '.join(content)}")
