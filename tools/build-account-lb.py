#!/usr/bin/env python3
"""Builds the Account + Leaderboard overlays and home-screen buttons, wired into
MenuController. Follows the meta-menu styling (btn sprites, dark panels). EDIT mode."""
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

ROOT = "BattleUI/SafeArea/MetaRoot"
HOME = f"{ROOT}/HomePanel"

def rect(path, amin, amax, pivot, pos, size):
    cmd("ui.set_rect", path=path, anchorMin=list(amin), anchorMax=list(amax), pivot=list(pivot),
        anchoredPosition=list(pos), sizeDelta=list(size))

def fill(path):
    cmd("ui.set_rect", path=path, anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])

def img(name, parent, color):
    cmd("ui.create_image", name=name, parent={"path": parent}, color=list(color))
    return f"{parent}/{name}"

def text(name, parent, s, size, color=(1, 1, 1, 1)):
    p = f"{parent}/{name}"
    cmd("ui.create_text", name=name, parent={"path": parent}, text=s, fontSize=size, alignment="Center", color=list(color))
    cmd("ui.config_text", path=p, alignment="Center", autoSize=False, fontSize=size, bold=True, wrap=True)
    return p

def button(name, parent, label, pos, size, fontsize=36, sprite="btn_dark", tint=(1, 1, 1, 1)):
    p = img(name, parent, [1, 1, 1, 1])
    cmd("component.set_fields", path=p, type="UnityEngine.UI.Image", fields={
        "m_Sprite": {"assetPath": f"Assets/UI/{sprite}.png", "assetType": "UnityEngine.Sprite"},
        "m_Type": 1, "m_Color": list(tint),
    })
    rect(p, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], pos, size)
    cmd("component.add", path=p, type="UnityEngine.UI.Button")
    cmd("component.set_fields", path=p, type="UnityEngine.UI.Button",
        fields={"m_TargetGraphic": {"sceneObjectPath": p, "componentType": "UnityEngine.UI.Image"}})
    lp = text("Label", p, label, fontsize)
    fill(lp)
    return p

def panel(name, title):
    try: cmd("gameobject.delete", path=f"{ROOT}/{name}")
    except Exception: pass
    p = img(name, ROOT, [1, 1, 1, 1])
    cmd("component.set_fields", path=p, type="UnityEngine.UI.Image", fields={
        "m_Sprite": {"assetPath": "Assets/UI/menubg.png", "assetType": "UnityEngine.Sprite"},
        "m_Type": 1, "m_Color": [0.92, 0.94, 1.0, 1.0],
    })
    fill(p)
    tp = text("Title", p, title, 56)
    rect(tp, [0.5, 1], [0.5, 1], [0.5, 1], [0, -60], [700, 90])
    back = button("BackBtn", p, "Back", [0, 0], [320, 92], 38)
    rect(back, [0.5, 0], [0.5, 0], [0.5, 0], [0, 50], [320, 92])
    cmd("gameobject.set_active", path=p, active=False)
    return p, back

# ---- home buttons row ---------------------------------------------------------------------
for n in ["LeaderboardBtn", "AccountBtn"]:
    try: cmd("gameobject.delete", path=f"{HOME}/{n}")
    except Exception: pass
lb_btn = button("LeaderboardBtn", HOME, "Leaderboard", [-150, -310], [240, 84], 30)
acct_btn = button("AccountBtn", HOME, "Account", [150, -310], [240, 84], 30)

# ---- Account panel ------------------------------------------------------------------------
acct, acct_back = panel("AccountPanel", "Account")
status = text("Status", acct, "…", 38)
rect(status, [0.1, 0.55], [0.9, 0.85], [0.5, 0.5], [0, 0], [0, 0])
apple = button("AppleBtn", acct, "Sign in with Apple", [0, -40], [520, 100], 36, sprite="btn_dark", tint=(0.15, 0.15, 0.18, 1))
google = button("GoogleBtn", acct, "Sign in with Google", [0, -160], [520, 100], 36, sprite="btn_dark", tint=(0.95, 0.95, 0.98, 1))
cmd("ui.config_text", path=f"{google}/Label", color=[0.15, 0.15, 0.18, 1], alignment="Center", autoSize=False, fontSize=36, bold=True)

# ---- Leaderboard panel --------------------------------------------------------------------
lb, lb_back = panel("LeaderboardPanel", "Leaderboard")
lstatus = text("Status", lb, "", 34)
rect(lstatus, [0.5, 1], [0.5, 1], [0.5, 1], [0, -150], [700, 60])
content = img("Content", lb, [0, 0, 0, 0.001])
rect(content, [0.06, 0.16], [0.94, 0.86], [0.5, 1], [0, 0], [0, 0])
cmd("component.add", path=content, type="UnityEngine.UI.VerticalLayoutGroup")
cmd("component.set_fields", path=content, type="UnityEngine.UI.VerticalLayoutGroup", fields={
    "m_Spacing": 10, "m_ChildForceExpandHeight": False, "m_ChildForceExpandWidth": True,
    "m_ChildControlHeight": False, "m_ChildControlWidth": True,
})
row = img("RowTemplate", content, [0.10, 0.12, 0.20, 0.85])
cmd("component.add", path=row, type="UnityEngine.UI.LayoutElement")
cmd("component.set_fields", path=row, type="UnityEngine.UI.LayoutElement", fields={"m_PreferredHeight": 76})
rk = text("Rank", row, "#1", 32)
cmd("ui.set_rect", path=rk, anchorMin=[0, 0], anchorMax=[0.18, 1], offsetMin=[0, 0], offsetMax=[0, 0])
nm = text("Name", row, "Player", 32)
cmd("ui.config_text", path=nm, alignment="Left", autoSize=False, fontSize=32, bold=True)
cmd("ui.set_rect", path=nm, anchorMin=[0.20, 0], anchorMax=[0.72, 1], offsetMin=[0, 0], offsetMax=[0, 0])
el = text("Elo", row, "1000", 32)
cmd("ui.set_rect", path=el, anchorMin=[0.74, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("gameobject.set_active", path=row, active=False)

# ---- wire MenuController ------------------------------------------------------------------
def btn(p): return {"sceneObjectPath": p, "componentType": "UnityEngine.UI.Button"}
def go(p): return {"sceneObjectPath": p}
def tmp(p): return {"sceneObjectPath": p, "componentType": "TMPro.TextMeshProUGUI"}
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_accountButton": btn(acct_btn), "_accountPanel": go(acct), "_accountStatus": tmp(status),
    "_appleSignInButton": btn(apple), "_googleSignInButton": btn(google), "_accountBackButton": btn(acct_back),
    "_leaderboardButton": btn(lb_btn), "_leaderboardPanel": go(lb), "_leaderboardStatus": tmp(lstatus),
    "_leaderboardContent": go(content), "_leaderboardRowTemplate": go(row), "_leaderboardBackButton": btn(lb_back),
})
cmd("scene.save_active")
print("Account + Leaderboard panels built + wired")
