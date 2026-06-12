#!/usr/bin/env python3
"""Procedural chiptune music + sfxr-style SFX for MonsterBattler → /tmp/audio_out/*.wav.

Music: one shared chord chart at 150 BPM renders three PHASE-LOCKED battle stems
(base / tension / triumph — identical length & grid, so AudioManager can crossfade
between simultaneously-playing sources sample-accurately), plus a calm menu loop
and short victory/defeat stings. SFX are classic synthesized one-shots.
"""
import numpy as np, wave, os

SR = 22050
OUT = "/tmp/audio_out"
os.makedirs(OUT, exist_ok=True)

def save(name, x, stereo_spread=0.0):
    x = np.clip(x / (np.max(np.abs(x)) + 1e-9) * 0.92, -1, 1)
    if stereo_spread > 0:
        d = int(SR * 0.012)
        l = x
        r = np.concatenate([np.zeros(d), x[:-d]]) * (1 - stereo_spread) + x * stereo_spread
        frames = np.stack([l, r], axis=1)
        nch = 2
    else:
        frames = x[:, None]
        nch = 1
    pcm = (frames * 32767).astype("<i2")
    with wave.open(f"{OUT}/{name}.wav", "wb") as w:
        w.setnchannels(nch); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(name, round(len(x)/SR, 2), "s")

def t_axis(dur): return np.arange(int(SR * dur)) / SR

def env(n, a, d, sustain=0.0, s_level=0.5):
    """attack/decay(+optional sustain tail) envelope over n samples"""
    e = np.zeros(n)
    ia, idk = int(a * SR), int(d * SR)
    ia = max(1, min(ia, n)); idk = max(1, min(idk, n - ia)) if n > ia else 0
    e[:ia] = np.linspace(0, 1, ia)
    if idk: e[ia:ia+idk] = np.linspace(1, s_level if sustain else 0, idk)
    if sustain and ia + idk < n:
        e[ia+idk:] = np.linspace(s_level, 0, n - ia - idk)
    return e

def osc(freq, dur, wave_="square", duty=0.5):
    t = t_axis(dur)
    ph = np.cumsum(np.full(len(t), freq) / SR) if np.isscalar(freq) else np.cumsum(freq / SR)
    frac = ph % 1.0
    if wave_ == "square": return np.where(frac < duty, 1.0, -1.0)
    if wave_ == "tri": return 2 * np.abs(2 * frac - 1) - 1
    if wave_ == "saw": return 2 * frac - 1
    return np.sin(2 * np.pi * ph)

def note_hz(semis_from_a4): return 440.0 * 2 ** (semis_from_a4 / 12.0)

# note names → semitones from A4
NAMES = {"C":-9,"C#":-8,"D":-7,"D#":-6,"E":-5,"F":-4,"F#":-3,"G":-2,"G#":-1,"A":0,"A#":1,"B":2}
def N(name, octv): return note_hz(NAMES[name] + (octv - 4) * 12)

def kick(dur=0.16):
    t = t_axis(dur)
    f = 110 * np.exp(-t * 22) + 38
    return np.sin(2*np.pi*np.cumsum(f)/SR) * env(len(t), 0.002, dur-0.002)

def snare(dur=0.14):
    n = len(t_axis(dur))
    noise = np.random.default_rng(7).uniform(-1, 1, n)
    body = osc(190, dur, "tri") * 0.4
    return (noise * 0.8 + body) * env(n, 0.001, dur-0.001)

def hat(dur=0.05):
    n = len(t_axis(dur))
    noise = np.random.default_rng(3).uniform(-1, 1, n)
    return np.convolve(noise, [1,-0.9], mode="same") * env(n, 0.001, dur-0.001) * 0.5

def place(buf, snd, at_s, gain=1.0):
    i = int(at_s * SR)
    j = min(len(buf), i + len(snd))
    if i < len(buf): buf[i:j] += snd[:j-i] * gain

def tone_at(buf, freq, at_s, dur, gain, wave_="square", a=0.004, duty=0.5, vib=0.0):
    n = int(dur * SR)
    f = np.full(n, float(freq))
    if vib: f *= 1 + vib * np.sin(2*np.pi*5.5*np.arange(n)/SR)
    s = osc(f, dur, wave_, duty) * env(n, a, dur-a, sustain=True, s_level=0.6)
    place(buf, s, at_s, gain)

def echo(x, ms=190, fb=0.3, mix=0.25):
    d = int(SR * ms / 1000)
    y = np.copy(x)
    for k in range(1, 4):
        g = mix * fb ** (k - 1)
        if k * d < len(x): y[k*d:] += x[:-k*d] * g
    return y

# ---------------- battle stems: shared chart, 150 BPM, 16 bars -------------------------------
BPM = 150.0
BEAT = 60.0 / BPM
BAR = BEAT * 4
BARS = 16
DUR = BAR * BARS
# chart: (root, third-offset) per bar — Am F C G ×3, then Am F E E
CHART = (["A4F", "F4M", "C4M", "G4M"] * 3) + ["A4F", "F4M", "E4M", "E4M"]
CHORDS = {  # name → (bass note, triad notes)
    "A4F": (("A", 2), [("A", 3), ("C", 4), ("E", 4)]),
    "F4M": (("F", 2), [("F", 3), ("A", 3), ("C", 4)]),
    "C4M": (("C", 3), [("C", 4), ("E", 4), ("G", 4)]),
    "G4M": (("G", 2), [("G", 3), ("B", 3), ("D", 4)]),
    "E4M": (("E", 2), [("E", 3), ("G#", 3), ("B", 3)]),
}
# 2-bar melody cells in A minor (degree offsets in semis from A4), -99 = rest
MEL = [0,-99,3,5, 7,5,3,0, -2,0,3,-99, 0,-99,-2,-4]  # eighth grid over 2 bars

rng = np.random.default_rng(42)

def render_battle(mode):
    n = int(DUR * SR)
    buf = np.zeros(n + SR)  # headroom tail, trimmed to loop length at the end
    for bar in range(BARS):
        t0 = bar * BAR
        bass, triad = CHORDS[CHART[bar]]
        bfreq = N(*bass)
        if mode == "tension":
            # halftime: brooding bass on 1 and 3 with a chromatic push, heartbeat kick
            for b in (0, 2):
                tone_at(buf, bfreq, t0 + b*BEAT, BEAT*0.9, 0.5, "tri")
            tone_at(buf, bfreq * 2**(1/12), t0 + 3.5*BEAT, BEAT*0.45, 0.3, "tri")
            place(buf, kick(), t0, 0.9); place(buf, kick(), t0 + 0.42*BEAT, 0.55)
            # sparse dark arp: root + minor 2nd shimmer up top
            if bar % 2 == 0:
                tone_at(buf, N(*triad[0])*2, t0 + 2*BEAT, BEAT*0.5, 0.16, "square", duty=0.3)
                tone_at(buf, N(*triad[0])*2*2**(1/12), t0 + 2.5*BEAT, BEAT*0.5, 0.12, "square", duty=0.3)
        else:
            # driving 8th bass
            for e8 in range(8):
                g = 0.5 if e8 % 2 == 0 else 0.38
                tone_at(buf, bfreq * (2 if e8 in (3, 7) else 1), t0 + e8*BEAT/2, BEAT*0.42, g, "tri")
            # drums
            for b in range(4):
                place(buf, kick(), t0 + b*BEAT, 1.0 if b in (0, 2) else 0.0)
                if b in (1, 3): place(buf, snare(), t0 + b*BEAT, 0.8)
            for e8 in range(8):
                place(buf, hat(), t0 + e8*BEAT/2, 0.5 if e8 % 2 else 0.3)
            if mode == "triumph":
                for e16 in range(16):  # extra drive
                    place(buf, hat(), t0 + e16*BEAT/4, 0.18)
                place(buf, snare(), t0 + 3.75*BEAT, 0.4)
            # chord arp (square)
            seq = triad + [triad[1]]
            for e8 in range(8):
                f = N(*seq[e8 % len(seq)]) * 2
                tone_at(buf, f, t0 + e8*BEAT/2, BEAT*0.3, 0.10, "square", duty=0.25)
            # lead melody every other pair of bars (eighth grid)
            if (bar // 2) % 2 == (0 if mode == "triumph" else 1):
                cell = MEL
                for i, semis in enumerate(cell):
                    if semis == -99: continue
                    f = note_hz(semis) * (2 if mode == "triumph" else 1)
                    at = t0 + (i % 16) * BEAT/2
                    if i // 16 != (0 if bar % 2 == 0 else 1): continue
                    tone_at(buf, f, at, BEAT*0.45, 0.2 if mode == "triumph" else 0.16,
                            "square", duty=0.5, vib=0.004)
            if mode == "triumph" and bar % 4 == 3:  # major-lift stab at phrase ends
                for f in [N("A",4), N("C#",5), N("E",5)]:
                    tone_at(buf, f, t0 + 3*BEAT, BEAT*0.9, 0.12, "square", duty=0.4)
    buf = echo(buf, 160, 0.25, 0.18)
    return buf[:n]  # exact loop length — all stems identical

save("music_battle_base", render_battle("base"), stereo_spread=0.3)
save("music_battle_tension", render_battle("tension"), stereo_spread=0.3)
save("music_battle_triumph", render_battle("triumph"), stereo_spread=0.3)

# ---------------- menu loop: 90 BPM, soft tri chords + gentle lead ----------------------------
MBPM = 90.0; MBEAT = 60.0/MBPM; MBAR = MBEAT*4; MBARS = 8
mn = int(MBAR * MBARS * SR)
mbuf = np.zeros(mn + SR)
MCHART = ["C4M", "A4F", "F4M", "G4M"] * 2
MLEAD = [7, 5, 3, 5, 0, -99, 3, -99]  # quarter notes over 2 bars
for bar in range(MBARS):
    t0 = bar * MBAR
    bass, triad = CHORDS[MCHART[bar]]
    tone_at(mbuf, N(*bass), t0, MBAR*0.95, 0.32, "tri", a=0.05)
    for nt in triad:
        tone_at(mbuf, N(*nt), t0, MBAR*0.95, 0.10, "tri", a=0.08)
    for q in range(4):
        i = (bar % 2) * 4 + q
        if MLEAD[i] != -99:
            tone_at(mbuf, note_hz(MLEAD[i]) * 2, t0 + q*MBEAT, MBEAT*0.8, 0.10, "tri", a=0.03, vib=0.005)
save("music_menu", echo(mbuf[:mn], 250, 0.35, 0.3), stereo_spread=0.35)

# ---------------- stings ----------------------------------------------------------------------
def sting(notes, step, dur_each, name, wave_="square"):
    total = step * len(notes) + 1.2
    b = np.zeros(int(total * SR))
    for i, group in enumerate(notes):
        for nt in (group if isinstance(group, list) else [group]):
            tone_at(b, N(*nt), i * step, dur_each, 0.3, wave_, a=0.005)
    save(name, echo(b, 180, 0.3, 0.25), stereo_spread=0.3)

sting([("C",4), ("E",4), ("G",4), [("C",5),("E",5),("G",5)]], 0.16, 0.5, "sting_victory")
sting([("E",4), ("D#",4), [("D",4),("G#",3)]], 0.4, 0.9, "sting_defeat", wave_="tri")

# ---------------- SFX --------------------------------------------------------------------------
def sweep(f0, f1, dur, wave_="square", gain=1.0, noise=0.0, a=0.004, duty=0.5, seed=1):
    n = int(dur * SR)
    f = np.geomspace(max(f0, 1), max(f1, 1), n)
    s = osc(f, dur, wave_, duty)
    if noise: s = s * (1-noise) + np.random.default_rng(seed).uniform(-1, 1, n) * noise
    return s * env(n, a, dur - a) * gain

save("sfx_click", sweep(1400, 900, 0.05, "square", duty=0.3))
save("sfx_hit", sweep(300, 80, 0.16, "square", noise=0.45))
save("sfx_hit_super", np.concatenate([sweep(500, 90, 0.13, "square", noise=0.6),
                                      sweep(900, 400, 0.1, "square", gain=0.5)]))
save("sfx_hit_weak", sweep(160, 70, 0.13, "tri", noise=0.3, gain=0.7))
save("sfx_faint", sweep(420, 60, 0.7, "square", duty=0.4))
save("sfx_switch", sweep(220, 1100, 0.18, "saw", noise=0.2, gain=0.6))
heal = np.zeros(int(0.5 * SR))
for i, f in enumerate([N("C",5), N("E",5), N("G",5), N("C",6)]):
    place(heal, osc(f, 0.16, "tri") * env(int(0.16*SR), 0.005, 0.15), i * 0.09)
save("sfx_heal", heal)
save("sfx_status", sweep(160, 140, 0.3, "square", duty=0.18, noise=0.15))
save("sfx_boost", sweep(250, 900, 0.25, "square", duty=0.35))
save("sfx_unboost", sweep(900, 250, 0.25, "square", duty=0.35))
save("sfx_hazard", sweep(700, 120, 0.35, "saw", noise=0.55))
save("sfx_item_off", np.concatenate([sweep(800, 1300, 0.06, "square"), sweep(700, 200, 0.18, "tri", gain=0.6)]))
lvl = np.zeros(int(0.7 * SR))
for i, f in enumerate([N("G",4), N("C",5), N("E",5), N("G",5), N("C",6)]):
    place(lvl, osc(f, 0.14, "square", 0.4) * env(int(0.14*SR), 0.004, 0.13), i * 0.08)
save("sfx_levelup", echo(lvl, 140, 0.3, 0.3))
save("sfx_charge", sweep(180, 1400, 0.6, "square", duty=0.3, gain=0.5))
print("done")
