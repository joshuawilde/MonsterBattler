#!/usr/bin/env python3
"""Adds the forced-switch prompt banner ("Choose your next monster!" + down arrow) just above
the player roster, wired to BattleView._forcedSwitchBanner. Hidden until a faint. EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
B = f"{SA}/ForcedSwitchBanner"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

try: cmd("gameobject.delete", path=B)
except Exception: pass

# Banner card — sits just above the bottom roster row (roster is ~140 tall, anchored to bottom).
cmd("ui.create_image", name="ForcedSwitchBanner", parent={"path": SA}, color=[0.93, 0.76, 0.18, 0.97])
cmd("ui.set_rect", path=B, anchorMin=[0.5, 0], anchorMax=[0.5, 0], pivot=[0.5, 0], anchoredPosition=[0, 175], sizeDelta=[640, 86])

cmd("ui.create_text", name="Label", parent={"path": B}, text="Choose your next monster!", fontSize=34, alignment="Center", color=[0.12, 0.10, 0.03, 1])
cmd("ui.set_rect", path=f"{B}/Label", anchorMin=[0, 0.28], anchorMax=[1, 1], offsetMin=[10, 0], offsetMax=[-10, 0])
cmd("ui.config_text", path=f"{B}/Label", alignment="Center", autoSize=False, fontSize=34, bold=True, wrap=False)

# Sub-line (plain words — the default TMP font lacks arrow/symbol glyphs).
cmd("ui.create_text", name="Arrow", parent={"path": B}, text="tap one below", fontSize=22, alignment="Center", color=[0.25, 0.20, 0.05, 1])
cmd("ui.set_rect", path=f"{B}/Arrow", anchorMin=[0, 0], anchorMax=[1, 0.34], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("ui.config_text", path=f"{B}/Arrow", alignment="Center", autoSize=False, fontSize=22, bold=False, wrap=False)

cmd("gameobject.set_active", path=B, active=False)

cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_forcedSwitchBanner": {"sceneObjectPath": B},
})
cmd("scene.save_active")
print("Forced-switch banner built + wired.")
