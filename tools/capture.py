#!/usr/bin/env python3
"""Capture a Game-view screenshot during play mode, GUARANTEEING play mode is exited afterward.

The whole point: never leave the editor running. Play mode is exited in a `finally` block (and the
script also exits any pre-existing play mode at the start), so a forgotten/failed step can't leave
the editor stuck in play.

Usage:
    python3 tools/capture.py <out.png> [click_path ...] [--wait N]

  out.png      where to write the screenshot
  click_path   optional UI paths to click (in order) before the shot, e.g. to drive a turn
  --wait N     seconds to wait after entering play before acting (default 8)
"""
import sys, os, json, time, urllib.request

URL = "http://127.0.0.1:17984/"

def cmd(command, **params):
    body = json.dumps({"id": "x", "command": command, "params": params}).encode()
    try:
        return json.load(urllib.request.urlopen(
            urllib.request.Request(URL, body, {"content-type": "application/json"}), timeout=30))
    except Exception as e:
        return {"ok": False, "error": str(e)}

def is_playing():
    return cmd("playmode.state").get("result", {}).get("isPlaying", False)

def exit_play():
    cmd("playmode.exit")
    for _ in range(20):
        if not is_playing():
            return True
        time.sleep(0.5)
    return False

wait = 8.0
raw = sys.argv[1:]
args = []
i = 0
while i < len(raw):
    if raw[i] == "--wait":
        wait = float(raw[i + 1]); i += 2; continue
    if raw[i].startswith("--"):
        i += 1; continue
    args.append(raw[i]); i += 1
out = os.path.abspath(args[0]) if args else "/tmp/mb.png"
clicks = args[1:]

if is_playing():
    exit_play()  # clean slate

try:
    cmd("playmode.enter")
    time.sleep(wait)
    for c in clicks:
        r = cmd("ui.click", path=c)
        print("click", c, "->", r.get("result", r.get("error")))
        time.sleep(10)  # let the turn's beat-by-beat playback finish before the next action/shot
    if os.path.exists(out):
        os.remove(out)
    cmd("game.screenshot", path=out)
    for _ in range(60):
        if os.path.exists(out) and os.path.getsize(out) > 0:
            break
        time.sleep(0.2)
    print("screenshot:", out, "ok" if os.path.exists(out) else "MISSING")
finally:
    ok = exit_play()
    print("playmode exited:", ok)
