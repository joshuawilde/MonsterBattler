#!/usr/bin/env python3
"""Builds the in-battle pause UI: a small pause button in the HUD (top-right) and a full-screen
pause overlay (Resume / Forfeit + Music / Sound FX / Haptics toggles), then wires PauseController
on BattleManager. Hidden until opened. Idempotent — deletes prior copies. Run with Unity in EDIT mode."""
import json, urllib.request, os

_pf = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
BTN = f"{SA}/PauseButton"
OV = f"{SA}/PauseOverlay"
CARD = f"{OV}/Card"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")

def img(name, parent, color, raycast=True):
    cmd("ui.create_image", name=name, parent={"path": parent}, color=list(color), raycastTarget=raycast)
    return f"{parent}/{name}"

def fill(path, omin=(0, 0), omax=(0, 0)):
    cmd("ui.set_rect", path=path, anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=list(omin), offsetMax=list(omax))

def rect(path, amin, amax, pivot, pos, size):
    cmd("ui.set_rect", path=path, anchorMin=list(amin), anchorMax=list(amax), pivot=list(pivot),
        anchoredPosition=list(pos), sizeDelta=list(size))

def text(name, parent, s, size, color=(1, 1, 1, 1), bold=True):
    p = f"{parent}/{name}"
    cmd("ui.create_text", name=name, parent={"path": parent}, text=s, fontSize=size, alignment="Center", color=list(color))
    cmd("ui.config_text", path=p, alignment="Center", autoSize=False, fontSize=size, bold=bold, wrap=False)
    return p

def button(name, parent, label, color, pos, size, fontsize=40):
    p = img(name, parent, color)
    rect(p, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], pos, size)
    cmd("component.add", path=p, type="UnityEngine.UI.Button")
    cmd("component.set_fields", path=p, type="UnityEngine.UI.Button",
        fields={"m_TargetGraphic": {"sceneObjectPath": p, "componentType": "UnityEngine.UI.Image"}})
    lp = text("Label", p, label, fontsize)
    fill(lp)
    return p

# ---------- clean prior ----------
for path in (BTN, OV):
    try: cmd("gameobject.delete", path=path)
    except Exception: pass

# ---------- HUD pause button (top-right) ----------
img("PauseButton", SA, [0.08, 0.09, 0.13, 0.78])
rect(BTN, [1, 1], [1, 1], [1, 1], [-16, -16], [84, 84])
cmd("component.add", path=BTN, type="UnityEngine.UI.Button")
cmd("component.set_fields", path=BTN, type="UnityEngine.UI.Button",
    fields={"m_TargetGraphic": {"sceneObjectPath": BTN, "componentType": "UnityEngine.UI.Image"}})
lbl = text("Label", BTN, "II", 44)
fill(lbl)

# ---------- full-screen pause overlay ----------
img("PauseOverlay", SA, [0.03, 0.04, 0.07, 0.86])   # dim backdrop (raycast blocks the battle)
fill(OV)
# centered card
img("Card", OV, [0.12, 0.13, 0.18, 0.99])
rect(CARD, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], [0, 0], [600, 760])
text("Title", CARD, "Paused", 64)
rect(f"{CARD}/Title", [0.5, 1], [0.5, 1], [0.5, 1], [0, -34], [560, 90])

# toggle buttons (label flips On/Off in PauseController)
TGL = [0.20, 0.32, 0.46, 1]
music = button("MusicButton", CARD, "Music: On", TGL, [0, 200], [500, 96], 38)
sfx = button("SfxButton", CARD, "Sound FX: On", TGL, [0, 90], [500, 96], 38)
hap = button("HapticsButton", CARD, "Haptics: On", TGL, [0, -20], [500, 96], 38)
# resume / forfeit
resume = button("ResumeButton", CARD, "Resume", [0.20, 0.55, 0.30, 1], [0, -150], [500, 104], 46)
forfeit = button("ForfeitButton", CARD, "Forfeit", [0.62, 0.22, 0.20, 1], [0, -280], [500, 104], 46)

# ---------- wire PauseController on BattleManager ----------
try: cmd("component.add", path="BattleManager", type="MonsterBattler.Game.UI.PauseController")
except Exception: pass  # already present
def ref(p, t=None):
    return {"sceneObjectPath": p} if t is None else {"sceneObjectPath": p, "componentType": t}
BTN_T, TXT_T = "UnityEngine.UI.Button", "TMPro.TextMeshProUGUI"
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.UI.PauseController", fields={
    "_battleView": ref("BattleManager", "MonsterBattler.Game.BattleView"),
    "_pauseButton": ref(BTN, BTN_T),
    "_overlay": ref(OV),
    "_resumeButton": ref(resume, BTN_T),
    "_forfeitButton": ref(forfeit, BTN_T),
    "_forfeitLabel": ref(f"{forfeit}/Label", TXT_T),
    "_musicButton": ref(music, BTN_T),
    "_musicLabel": ref(f"{music}/Label", TXT_T),
    "_sfxButton": ref(sfx, BTN_T),
    "_sfxLabel": ref(f"{sfx}/Label", TXT_T),
    "_hapticsButton": ref(hap, BTN_T),
    "_hapticsLabel": ref(f"{hap}/Label", TXT_T),
})

# hidden until opened (PauseController also disables on Awake)
cmd("gameobject.set_active", path=OV, active=False)
cmd("scene.save_active")
print("pause UI built + wired OK")
