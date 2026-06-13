#!/usr/bin/env python3
"""Round 2 for the account/leaderboard UI:
- Leaderboard: wrap the rows in a ScrollRect (scrollable, top 20), back button in FRONT.
- Account: username editor row (input + Save + status), only shown when signed in.
- Home: online-count label.
Re-wires the new MenuController fields. EDIT mode."""
import json, urllib.request, os

_pf = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT

def cmd(c, **p):
    body = json.dumps({"id": "x", "command": c, "params": p}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=120))
    if not r.get("ok"): raise RuntimeError(f"{c} failed: {r.get('error')}")
    return r.get("result")

ROOT = "BattleUI/SafeArea/MetaRoot"
HOME = f"{ROOT}/HomePanel"
LB = f"{ROOT}/LeaderboardPanel"
ACC = f"{ROOT}/AccountPanel"

def rect(path, amin, amax, piv, pos, size):
    cmd("ui.set_rect", path=path, anchorMin=list(amin), anchorMax=list(amax), pivot=list(piv),
        anchoredPosition=list(pos), sizeDelta=list(size))
def fill(p): cmd("ui.set_rect", path=p, anchorMin=[0,0], anchorMax=[1,1], offsetMin=[0,0], offsetMax=[0,0])
def img(name, parent, color):
    cmd("ui.create_image", name=name, parent={"path": parent}, color=list(color)); return f"{parent}/{name}"
def text(name, parent, s, size, color=(1,1,1,1), align="Center"):
    p=f"{parent}/{name}"; cmd("ui.create_text", name=name, parent={"path": parent}, text=s, fontSize=size, alignment=align, color=list(color))
    cmd("ui.config_text", path=p, alignment=align, autoSize=False, fontSize=size, bold=True, wrap=True); return p
def go(p): return {"sceneObjectPath": p}
def tmp(p): return {"sceneObjectPath": p, "componentType": "TMPro.TextMeshProUGUI"}
def btn(p): return {"sceneObjectPath": p, "componentType": "UnityEngine.UI.Button"}

# ---------- Leaderboard: rebuild as a ScrollRect ----------
# delete the old flat Content + RowTemplate, build ScrollView/Viewport/Content
for old in ["Content", "RowTemplate", "ScrollView"]:
    while True:  # delete ALL (handles dupes from a partial earlier run)
        try: cmd("gameobject.delete", path=f"{LB}/{old}")
        except Exception: break
scroll = img("ScrollView", LB, [0,0,0,0.001])
rect(scroll, [0.06,0.14],[0.94,0.84],[0.5,0.5],[0,0],[0,0])
cmd("component.add", path=scroll, type="UnityEngine.UI.ScrollRect")
viewport = img("Viewport", scroll, [0,0,0,0.001])
fill(viewport)
cmd("component.add", path=viewport, type="UnityEngine.UI.RectMask2D")
content = img("Content", viewport, [0,0,0,0])
rect(content, [0,1],[1,1],[0.5,1],[0,0],[0,0])  # top-anchored, grows downward
cmd("component.add", path=content, type="UnityEngine.UI.VerticalLayoutGroup")
cmd("component.set_fields", path=content, type="UnityEngine.UI.VerticalLayoutGroup", fields={
    "m_Spacing": 8, "m_ChildForceExpandHeight": False, "m_ChildForceExpandWidth": True,
    "m_ChildControlHeight": False, "m_ChildControlWidth": True})
cmd("component.add", path=content, type="UnityEngine.UI.ContentSizeFitter")
cmd("component.set_fields", path=content, type="UnityEngine.UI.ContentSizeFitter", fields={"m_VerticalFit": 2})  # PreferredSize
cmd("component.set_fields", path=scroll, type="UnityEngine.UI.ScrollRect", fields={
    "m_Content": {"sceneObjectPath": content, "componentType": "UnityEngine.RectTransform"},
    "m_Viewport": {"sceneObjectPath": viewport, "componentType": "UnityEngine.RectTransform"},
    "m_Horizontal": False, "m_Vertical": True, "m_MovementType": 1, "m_ScrollSensitivity": 30})

# row template (inactive) under content
row = img("RowTemplate", content, [0.10,0.12,0.20,0.85])
cmd("component.add", path=row, type="UnityEngine.UI.LayoutElement")
cmd("component.set_fields", path=row, type="UnityEngine.UI.LayoutElement", fields={"m_PreferredHeight": 76, "m_MinHeight": 76})
rk=text("Rank", row, "#1", 30); cmd("ui.set_rect", path=rk, anchorMin=[0,0],anchorMax=[0.18,1],offsetMin=[8,0],offsetMax=[0,0])
nm=text("Name", row, "Player", 30, align="Left"); cmd("ui.set_rect", path=nm, anchorMin=[0.20,0],anchorMax=[0.72,1],offsetMin=[0,0],offsetMax=[0,0])
el=text("Elo", row, "1000", 30); cmd("ui.set_rect", path=el, anchorMin=[0.74,0],anchorMax=[1,1],offsetMin=[0,-0],offsetMax=[-8,0])
cmd("gameobject.set_active", path=row, active=False)

# back button to FRONT (last sibling draws on top of the scroll view)
cmd("gameobject.set_sibling_index", path=f"{LB}/BackBtn", index=99)

# ---------- Account: username editor row ----------
try: cmd("gameobject.delete", path=f"{ACC}/UsernameRow")
except Exception: pass
urow = img("UsernameRow", ACC, [0,0,0,0.001])
rect(urow, [0.08,0.30],[0.92,0.52],[0.5,0.5],[0,0],[0,0])
lab = text("Label", urow, "Username", 30); rect(lab, [0,1],[1,1],[0.5,1],[0,0],[300,40])
# input field
fld = img("Input", urow, [1,1,1,0.10])
rect(fld, [0.5,0.5],[0.5,0.5],[0.5,0.5],[-60,-10],[360,80])
cmd("component.add", path=fld, type="TMPro.TMP_InputField")
ph = text("Placeholder", fld, "Enter name…", 28, color=(1,1,1,0.4), align="Left"); fill(ph)
txt = text("Text", fld, "", 28, align="Left"); fill(txt)
cmd("component.set_fields", path=fld, type="TMPro.TMP_InputField", fields={
    "m_TextComponent": {"sceneObjectPath": txt, "componentType": "TMPro.TextMeshProUGUI"},
    "m_Placeholder": {"sceneObjectPath": ph, "componentType": "TMPro.TextMeshProUGUI"},
    "m_CharacterLimit": 24})
# save button
save = img("SaveBtn", urow, [1,1,1,1])
cmd("component.set_fields", path=save, type="UnityEngine.UI.Image", fields={
    "m_Sprite": {"assetPath": "Assets/UI/btn_primary.png", "assetType": "UnityEngine.Sprite"}, "m_Type": 1})
rect(save, [0.5,0.5],[0.5,0.5],[0.5,0.5],[170,-10],[150,80])
cmd("component.add", path=save, type="UnityEngine.UI.Button")
cmd("component.set_fields", path=save, type="UnityEngine.UI.Button", fields={"m_TargetGraphic":{"sceneObjectPath":save,"componentType":"UnityEngine.UI.Image"}})
sl=text("Label", save, "Save", 30); fill(sl)
ust = text("Status", urow, "", 24, color=(1,1,1,0.8)); rect(ust, [0,0],[1,0],[0.5,0],[0,8],[400,36])

# ---------- Home: online count label ----------
try: cmd("gameobject.delete", path=f"{HOME}/OnlineCount")
except Exception: pass
oc = text("OnlineCount", HOME, "● 2 online", 28, align="Center")
rect(oc, [0.5,1],[0.5,1],[0.5,1],[0,-150],[400,40])
cmd("gameobject.set_active", path=oc, active=False)

# ---------- wire MenuController ----------
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.Meta.MenuController", fields={
    "_leaderboardContent": go(content),
    "_leaderboardRowTemplate": go(row),
    "_usernameRow": go(urow),
    "_usernameInput": {"sceneObjectPath": fld, "componentType": "TMPro.TMP_InputField"},
    "_usernameSaveButton": btn(save),
    "_usernameStatus": tmp(ust),
    "_onlineCount": tmp(oc),
})
cmd("scene.save_active")
print("Account/leaderboard round 2 built + wired")
