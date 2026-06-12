#!/usr/bin/env python3
"""Drives a full battle from the Home menu to the end screen, handling FORCED SWITCHES
(when the active mon faints, taps benched roster icons -> info-panel Swap, which no-ops
unless legal). Snaps the end screen, then ALWAYS exits play mode.

Usage: python3 tools/play-battle.py <end_screenshot.png> [--move N] [--max-turns N]
"""
import json, os, sys, time, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT

def cmd(c, **p):
    b = json.dumps({"id": "x", "command": c, "params": p}).encode()
    try:
        return json.load(urllib.request.urlopen(
            urllib.request.Request(URL, b, {"content-type": "application/json"}), timeout=30))
    except Exception as e:
        return {"ok": False, "error": str(e)}

def playing(): return cmd("playmode.state").get("result", {}).get("isPlaying", False)

def active(path):
    r = cmd("scene.get_object", path=path).get("result", {})
    return bool(r.get("active") or r.get("activeSelf"))

def turn_text():
    # NOTE: TMP serializes its text as "m_text" (lowercase t).
    r = cmd("component.get", path="BattleUI/SafeArea/TurnPanel/TurnText", type="TMPro.TextMeshProUGUI")
    if r.get("ok"):
        f = r.get("result", {}).get("fields", {}) or {}
        return f.get("m_text") or f.get("m_Text") or ""
    return ""

def resolve_forced_switch():
    """Tap each benched roster icon -> Swap (validated game-side), closing the panel between tries."""
    for i in range(4):
        cmd("ui.click", path=f"BattleUI/SafeArea/PlayerRoster/Icon{i}")
        time.sleep(0.6)
        cmd("ui.click", path="BattleUI/SafeArea/InfoPanel/SwapButton")
        time.sleep(0.6)
        cmd("ui.click", path="BattleUI/SafeArea/InfoPanel/CloseButton")
        time.sleep(0.4)

args = [a for a in sys.argv[1:] if not a.startswith("--")]
out = os.path.abspath(args[0]) if args else "/tmp/mb_end.png"
move = 0
max_turns = 60
raw = sys.argv[1:]
for i, a in enumerate(raw):
    if a == "--move": move = int(raw[i + 1])
    if a == "--max-turns": max_turns = int(raw[i + 1])

if playing():
    cmd("playmode.exit"); time.sleep(2)

try:
    cmd("playmode.enter")
    # Wait for the menu to actually be live (post-compile play entry includes a slow domain
    # reload — clicking before Awake wires the listeners is a silent no-op).
    for _ in range(30):
        time.sleep(1)
        if active("BattleUI/SafeArea/MetaRoot/HomePanel"): break
    time.sleep(2)  # listeners wire in Awake; give the first frames a beat

    # Click Battle and VERIFY it took (Home hides immediately when the flow starts); retry if not.
    for attempt in range(4):
        cmd("ui.click", path="BattleUI/SafeArea/MetaRoot/HomePanel/BattleBtn")
        time.sleep(3)
        if not active("BattleUI/SafeArea/MetaRoot/HomePanel"):
            break
        print(f"battle click attempt {attempt + 1} didn't take; retrying")
    time.sleep(6)  # matchmaking flow + battle start

    last_turn = ""
    stalls = 0
    for it in range(max_turns):
        if active("BattleUI/SafeArea/EndScreen"):
            print(f"end screen after {it} actions")
            break
        t = turn_text()
        if t and t == last_turn:
            stalls += 1
            if stalls >= 1:  # same turn after a full action wait -> likely a forced switch
                print(f"stall on '{t}' -> trying forced switch")
                resolve_forced_switch()
                stalls = 0
        else:
            stalls = 0
        last_turn = t or last_turn
        cmd("ui.click", path=f"BattleUI/SafeArea/Moves/Move{move}")
        time.sleep(9)
    else:
        print("max turns reached without end screen")

    time.sleep(12)  # let the result pop + progression cards reveal
    if os.path.exists(out): os.remove(out)
    cmd("game.screenshot", path=out)
    for _ in range(50):
        if os.path.exists(out) and os.path.getsize(out) > 0: break
        time.sleep(0.2)
    print("shot:", out, "ok" if os.path.exists(out) else "MISSING")
    r = cmd("console.count").get("result", {})
    print("errors", r.get("error"), "exceptions", r.get("exception"))
finally:
    cmd("playmode.exit")
    for _ in range(20):
        if not playing(): break
        time.sleep(0.5)
    print("playmode exited:", not playing())
