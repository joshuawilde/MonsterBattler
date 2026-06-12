#!/usr/bin/env python3
"""Builds the post-battle move-progress reveal: MoveCard.prefab (slide-in card with progress bar,
"MOVE UNLOCKED!" label, flash bloom, and a UIParticle spark burst) + the GainsContainer under
EndScreen, and wires BattleView. Requires com.coffee.ui-particle. Idempotent; run in EDIT mode."""
import json, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT
SA = "BattleUI/SafeArea"
END = f"{SA}/EndScreen"
PREFAB = "Assets/Prefabs/MoveCard.prefab"


def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    if not r.get("ok"):
        raise RuntimeError(f"{command} failed: {r.get('error')}")
    return r.get("result")


def img(name, parent, color, raycast=False):
    cmd("ui.create_image", name=name, parent={"path": parent}, color=color, raycastTarget=raycast)
    return f"{parent}/{name}"


def rect(path, amin, amax, pivot, pos, size):
    cmd("ui.set_rect", path=path, anchorMin=list(amin), anchorMax=list(amax), pivot=list(pivot), anchoredPosition=list(pos), sizeDelta=list(size))


def text(name, parent, s, size, color=(1, 1, 1, 1), bold=True, align="Left"):
    p = f"{parent}/{name}"
    cmd("ui.create_text", name=name, parent={"path": parent}, text=s, fontSize=size, alignment=align, color=list(color))
    cmd("ui.config_text", path=p, alignment=align, autoSize=False, fontSize=size, bold=bold, wrap=False)
    return p


# ---------- MoveCard prefab ----------
T = f"{SA}/__MoveCardTemplate"
try: cmd("gameobject.delete", path=T)
except Exception: pass
img("__MoveCardTemplate", SA, [0.10, 0.11, 0.16, 0.97])
rect(T, [0.5, 1], [0.5, 1], [0.5, 1], [0, 0], [840, 104])
cmd("component.add", path=T, type="UnityEngine.CanvasGroup")
cmd("component.add", path=T, type="MonsterBattler.Game.UI.MoveProgressCard")

# Mon icon — left edge, square, sits above the progress bar strip.
icon = img("MonIcon", T, [1, 1, 1, 1], raycast=False)
rect(icon, [0, 0.5], [0, 0.5], [0, 0.5], [12, 8], [72, 72])
cmd("component.set_fields", path=icon, type="UnityEngine.UI.Image", fields={"m_PreserveAspect": True})

mv = text("MoveName", T, "Thunderbolt", 34)
cmd("ui.set_rect", path=mv, anchorMin=[0, 0.5], anchorMax=[0.62, 1], offsetMin=[94, 0], offsetMax=[0, -6])
mon = text("MonName", T, "Raichu-Alola", 22, (0.62, 0.67, 0.78, 1), bold=False)
cmd("ui.set_rect", path=mon, anchorMin=[0, 0.18], anchorMax=[0.62, 0.52], offsetMin=[94, 0], offsetMax=[0, 0])
barlab = text("BarLabel", T, "3/10", 24, (0.80, 0.85, 0.95, 1), align="Right")
cmd("ui.set_rect", path=barlab, anchorMin=[0.62, 0.45], anchorMax=[1, 0.95], offsetMin=[0, 0], offsetMax=[-24, 0])

bg = img("BarBg", T, [0.04, 0.045, 0.07, 1])
cmd("ui.set_rect", path=bg, anchorMin=[0.03, 0.10], anchorMax=[0.97, 0.26], offsetMin=[0, 0], offsetMax=[0, 0])
fillp = img("BarFill", bg, [0.36, 0.66, 1.0, 1])
cmd("ui.set_rect", path=fillp, anchorMin=[0, 0], anchorMax=[1, 1], offsetMin=[0, 0], offsetMax=[0, 0])
cmd("component.set_fields", path=fillp, type="UnityEngine.UI.Image",
    fields={"m_Type": 3, "m_FillMethod": 0, "m_FillOrigin": 0, "m_FillAmount": 0.3})

flash = img("Flash", T, [1, 1, 1, 0])
rect(flash, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], [0, 0], [300, 120])
unlocked = text("UnlockedLabel", T, "MOVE UNLOCKED!", 36, (1, 0.82, 0.30, 1), align="Center")
rect(unlocked, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], [0, 8], [600, 50])
cmd("gameobject.set_active", path=unlocked, active=False)

# spark burst: ParticleSystem + UIParticle + UnlockBurst (recipe configured in code)
burst = f"{T}/Burst"
cmd("gameobject.create", name="Burst", parent={"path": T})
cmd("component.add", path=burst, type="UnityEngine.ParticleSystem")
cmd("component.add", path=burst, type="Coffee.UIExtensions.UIParticle")
cmd("component.add", path=burst, type="MonsterBattler.Game.UI.UnlockBurst")
rect(burst, [0.5, 0.5], [0.5, 0.5], [0.5, 0.5], [0, 0], [10, 10])

cmd("component.set_fields", path=T, type="MonsterBattler.Game.UI.MoveProgressCard", fields={
    "_moveName": {"sceneObjectPath": mv, "componentType": "TMPro.TextMeshProUGUI"},
    "_monName": {"sceneObjectPath": mon, "componentType": "TMPro.TextMeshProUGUI"},
    "_monIcon": {"sceneObjectPath": icon, "componentType": "UnityEngine.UI.Image"},
    "_barFill": {"sceneObjectPath": fillp, "componentType": "UnityEngine.UI.Image"},
    "_barLabel": {"sceneObjectPath": barlab, "componentType": "TMPro.TextMeshProUGUI"},
    "_unlockedLabel": {"sceneObjectPath": unlocked},
    "_flash": {"sceneObjectPath": flash, "componentType": "UnityEngine.UI.Image"},
    "_burst": {"sceneObjectPath": burst, "componentType": "MonsterBattler.Game.UI.UnlockBurst"},
    "_group": {"sceneObjectPath": T, "componentType": "UnityEngine.CanvasGroup"},
})
cmd("prefab.save_as", path=T, assetPath=PREFAB, connectInstance=False)
cmd("gameobject.delete", path=T)

# ---------- container + inactive template under EndScreen ----------
GAINS = f"{END}/GainsContainer"
try: cmd("gameobject.delete", path=GAINS)
except Exception: pass
img("GainsContainer", END, [0, 0, 0, 0])
rect(GAINS, [0.5, 0.5], [0.5, 0.5], [0.5, 1], [0, -40], [840, 360])

try: cmd("gameobject.delete", path=f"{END}/MoveCardTemplate")
except Exception: pass
cmd("prefab.instantiate", assetPath=PREFAB, parent={"path": END}, name="MoveCardTemplate")
TMPL = f"{END}/MoveCardTemplate"
cmd("gameobject.set_active", path=TMPL, active=False)

# ---------- wire BattleView ----------
cmd("component.set_fields", path="BattleManager", type="MonsterBattler.Game.BattleView", fields={
    "_gainsContainer": {"sceneObjectPath": GAINS, "componentType": "UnityEngine.RectTransform"},
    "_moveCardPrefab": {"sceneObjectPath": TMPL, "componentType": "MonsterBattler.Game.UI.MoveProgressCard"},
})
cmd("scene.save_active")
print("MoveCard prefab + GainsContainer built and wired.")
