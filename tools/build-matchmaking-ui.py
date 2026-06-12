#!/usr/bin/env python3
"""Adds the ranked Matchmaking panel (Searching… → Found: You vs Opp with Elos) under the meta
menu, plus a Home Elo line, and wires the new MenuController fields. Idempotent. Run in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
ROOT = "BattleUI/SafeArea/MetaRoot"
HOME = f"{ROOT}/HomePanel"
MM = f"{ROOT}/MatchmakingPanel"


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


def text(name, parent, s, size, color=(1, 1, 1, 1), bold=True):
    p = f"{parent}/{name}"
    cmd("ui.create_text", name=name, parent={"path": parent}, text=s, fontSize=size, alignment="Center", color=list(color))
    cmd("ui.config_text", path=p, alignment="Center", autoSize=False, fontSize=size, bold=bold, wrap=True)
    return p


def ctr(path, pos, size):  # center-anchored placement
    rect(path, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], pos, size)


def tmp(p): return {"sceneObjectPath": p, "componentType": "TMPro.TextMeshProUGUI"}
def go(p): return {"sceneObjectPath": p}


# ---- Matchmaking panel (opaque overlay) ----
try: cmd("gameobject.delete", path=MM)
except Exception: pass
img("MatchmakingPanel", ROOT, [0.06, 0.07, 0.12, 1.0])
fill(MM)

status = text("Status", MM, "Searching for opponent…", 48, (0.85, 0.88, 0.95, 1))
rect(status, [0.5, 1], [0.5, 1], [0.5, 1], [0, -220], [950, 80])

pname = text("PlayerName", MM, "You", 56, (0.45, 0.72, 1.0, 1))
ctr(pname, [0, 210], [850, 72])
pelo = text("PlayerElo", MM, "Elo 1000", 40, (0.80, 0.85, 0.95, 1))
ctr(pelo, [0, 150], [650, 54])

vs = text("VS", MM, "VS", 72, (1.0, 0.82, 0.30, 1))
ctr(vs, [0, 20], [300, 96])

oname = text("OppName", MM, "?", 56, (1.0, 0.50, 0.45, 1))
ctr(oname, [0, -130], [850, 72])
oelo = text("OppElo", MM, "", 40, (0.80, 0.85, 0.95, 1))
ctr(oelo, [0, -190], [650, 54])

cmd("gameobject.set_active", path=MM, active=False)

# ---- Home Elo line (under the Team line) ----
try: cmd("gameobject.delete", path=f"{HOME}/Elo")
except Exception: pass
helo = text("Elo", HOME, "You · Elo 1000", 34, (1.0, 0.86, 0.45, 1))
rect(helo, [0.5, 1], [0.5, 1], [0.5, 1], [0, -290], [800, 50])

# ---- wire MenuController ----
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_matchmakingPanel": go(MM),
    "_mmStatus": tmp(status), "_mmPlayerName": tmp(pname), "_mmPlayerElo": tmp(pelo),
    "_mmOppName": tmp(oname), "_mmOppElo": tmp(oelo),
    "_homeElo": tmp(helo),
})

cmd("scene.save_active")
print("Matchmaking panel built + wired.")
