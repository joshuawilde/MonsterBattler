#!/usr/bin/env python3
"""Imports the generated audio (music stems from tools/gen-audio.py, SFX from ElevenLabs —
both staged in /tmp/audio_out), builds the AudioRoot scene object (5 AudioSources: menu/sting,
3 phase-locked battle stems, sfx one-shots) and wires every AudioManager clip field. EDIT mode."""
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

MUSIC = ["music_menu.wav", "music_battle_base.wav", "music_battle_tension.wav",
         "music_battle_triumph.wav", "sting_victory.wav", "sting_defeat.wav"]
SFX = ["sfx_click.mp3", "sfx_hit.mp3", "sfx_hit_super.mp3", "sfx_hit_weak.mp3", "sfx_faint.mp3",
       "sfx_switch.mp3", "sfx_heal.mp3", "sfx_status.mp3", "sfx_boost.mp3", "sfx_unboost.mp3",
       "sfx_hazard.mp3", "sfx_item_off.mp3", "sfx_levelup.mp3", "sfx_charge.mp3"]

for f in MUSIC + SFX:
    src = f"/tmp/audio_out/{f}"
    if not os.path.exists(src): raise SystemExit(f"missing {src}")
    cmd("asset.copy_in", **{"from": src, "to": f"Assets/Audio/{f}"})
print("audio assets imported")

try: cmd("gameobject.delete", path="AudioRoot")
except Exception: pass
cmd("gameobject.create", name="AudioRoot")
for child in ["MenuSrc", "StemBase", "StemTension", "StemTriumph", "SfxSrc"]:
    cmd("gameobject.create", name=child, parent={"path": "AudioRoot"})
    cmd("component.add", path=f"AudioRoot/{child}", type="UnityEngine.AudioSource")
    cmd("component.set_fields", path=f"AudioRoot/{child}", type="UnityEngine.AudioSource",
        fields={"m_PlayOnAwake": False})
cmd("component.add", path="AudioRoot", type="MonsterBattler.Game.AudioManager")

def src(child): return {"sceneObjectPath": f"AudioRoot/{child}", "componentType": "UnityEngine.AudioSource"}
def clip(f): return {"assetPath": f"Assets/Audio/{f}", "assetType": "UnityEngine.AudioClip"}

cmd("component.set_fields", path="AudioRoot", type="MonsterBattler.Game.AudioManager", fields={
    "_menuSrc": src("MenuSrc"), "_stemBase": src("StemBase"), "_stemTension": src("StemTension"),
    "_stemTriumph": src("StemTriumph"), "_sfxSrc": src("SfxSrc"),
    "_menuTheme": clip("music_menu.wav"), "_battleBase": clip("music_battle_base.wav"),
    "_battleTension": clip("music_battle_tension.wav"), "_battleTriumph": clip("music_battle_triumph.wav"),
    "_stingVictory": clip("sting_victory.wav"), "_stingDefeat": clip("sting_defeat.wav"),
    "_click": clip("sfx_click.mp3"), "_hit": clip("sfx_hit.mp3"), "_hitSuper": clip("sfx_hit_super.mp3"),
    "_hitWeak": clip("sfx_hit_weak.mp3"), "_faint": clip("sfx_faint.mp3"), "_switch": clip("sfx_switch.mp3"),
    "_heal": clip("sfx_heal.mp3"), "_status": clip("sfx_status.mp3"), "_boost": clip("sfx_boost.mp3"),
    "_unboost": clip("sfx_unboost.mp3"), "_hazard": clip("sfx_hazard.mp3"),
    "_itemOff": clip("sfx_item_off.mp3"), "_levelUp": clip("sfx_levelup.mp3"), "_charge": clip("sfx_charge.mp3"),
})
cmd("scene.save_active")
print("AudioRoot built + wired")
