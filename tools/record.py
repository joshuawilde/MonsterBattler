#!/usr/bin/env python3
"""Record the running game as a frame sequence and assemble it for review. Guarantees play-mode
exit (same contract as capture.py).

Usage:
    python3 tools/record.py <outdir> [click_path ...] [--wait N] [--record S] [--every N] [--poll PATH]

  outdir        where frames + montage.png + clip.gif land
  click_path    UI paths clicked in order after entering play (10s apart, like capture.py)
  --wait N      seconds after entering play before acting (default 7)
  --record S    seconds to record after the last click (default 6)
  --every N     capture every N frames (default 3)
  --poll PATH   instead of recording immediately after clicks, click repeatedly (every 11s, same
                path as the last click_path) until this object is activeSelf, THEN record. Lets you
                e.g. play a battle to the end screen and record the end-screen animation.

Outputs: <outdir>/frames/frame_XXXX.png, <outdir>/montage.png (contact sheet for the agent to
Read), <outdir>/clip.gif (animated, for humans)."""
import sys, os, json, time, glob, urllib.request

import os as _os
_pf = _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984
URL = "http://127.0.0.1:%d/" % _PORT


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


def active(path):
    res = cmd("scene.get_object", path=path).get("result") or {}
    for k in ("activeSelf", "active", "isActive", "activeInHierarchy"):
        if k in res:
            return bool(res[k])
    return False


# ---- args ----
wait, record_s, every, poll = 7.0, 6.0, 3, None
raw, args = sys.argv[1:], []
i = 0
while i < len(raw):
    if raw[i] == "--wait": wait = float(raw[i + 1]); i += 2; continue
    if raw[i] == "--record": record_s = float(raw[i + 1]); i += 2; continue
    if raw[i] == "--every": every = int(raw[i + 1]); i += 2; continue
    if raw[i] == "--poll": poll = raw[i + 1]; i += 2; continue
    args.append(raw[i]); i += 1
outdir = os.path.abspath(args[0] if args else "/tmp/mb_recording")
clicks = args[1:]
frames_dir = os.path.join(outdir, "frames")
os.makedirs(frames_dir, exist_ok=True)
for f in glob.glob(os.path.join(frames_dir, "*.png")):
    os.remove(f)

if is_playing():
    exit_play()

try:
    cmd("playmode.enter")
    time.sleep(wait)
    for c in clicks:
        print("click", c, "->", cmd("ui.click", path=c).get("ok"))
        time.sleep(10)
    if poll:
        for n in range(30):
            if active(poll):
                print(f"poll target active after {n} rounds"); break
            if clicks:
                cmd("ui.click", path=clicks[-1])
            time.sleep(11)
    fps_guess = 60 / every
    max_frames = int(record_s * fps_guess) + 10
    r = cmd("game.record_start", dir=frames_dir, everyN=every, max=max_frames)
    print("recording:", r.get("result", r.get("error")))
    time.sleep(record_s)
    r = cmd("game.record_stop")
    print("stopped:", r.get("result", r.get("error")))
    time.sleep(1.5)  # let trailing writes land
finally:
    print("playmode exited:", exit_play())

# ---- assemble ----
frames = sorted(glob.glob(os.path.join(frames_dir, "*.png")))
print(f"{len(frames)} frames captured")
if frames:
    from PIL import Image
    # contact sheet: up to 24 frames, 4 per row, downscaled — one Readable overview image
    step = max(1, len(frames) // 24)
    picks = frames[::step][:24]
    thumbs = [Image.open(f).reduce(4) for f in picks]
    tw, th = thumbs[0].size
    cols = 4
    rows = (len(thumbs) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * tw, rows * th), (12, 12, 16))
    for k, t in enumerate(thumbs):
        sheet.paste(t, ((k % cols) * tw, (k // cols) * th))
    sheet.save(os.path.join(outdir, "montage.png"))
    # gif for humans
    gif = [Image.open(f).reduce(2).convert("P", palette=Image.ADAPTIVE) for f in frames]
    gif[0].save(os.path.join(outdir, "clip.gif"), save_all=True, append_images=gif[1:],
                duration=int(1000 * every / 60), loop=0)
    print("montage:", os.path.join(outdir, "montage.png"))
    print("gif:", os.path.join(outdir, "clip.gif"))
