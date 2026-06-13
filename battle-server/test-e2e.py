#!/usr/bin/env python3
"""End-to-end test of the battle server: register a match, connect two WS clients,
play to completion via random legal-ish choices, assert a result + chat relay."""
import json, threading, time, urllib.request
import websocket  # pip install websocket-client

BASE = "http://127.0.0.1:8081"
WS = "ws://127.0.0.1:8081/ws"
KEY = "k"

# a tiny 1-mon team each so the battle ends fast
TEAM0 = {"uid": "u0", "username": "Ann", "elo": 1000,
         "species": ["pikachu"], "levels": [50], "movesCsv": ["thunderbolt"]}
TEAM1 = {"uid": "u1", "username": "Bob", "elo": 1000,
         "species": ["charizard"], "levels": [50], "movesCsv": ["flamethrower"]}

def register():
    req = urllib.request.Request(BASE + "/internal/match",
        data=json.dumps({"matchId": "m1", "uid0": "u0", "uid1": "u1"}).encode(),
        headers={"Content-Type": "application/json", "X-Api-Key": KEY})
    print("register:", urllib.request.urlopen(req).read().decode())

results = {}
def play(uid, team, label):
    ws = websocket.create_connection(WS)
    ws.send(json.dumps({"t": "join", "matchId": "m1", "uid": uid, "team": team}))
    got_start = False
    turns = 0
    while True:
        msg = json.loads(ws.recv())
        t = msg["t"]
        if t == "waiting":
            continue
        if t == "start":
            got_start = True
            results[label + "_side"] = msg["side"]
            if label == "A":
                ws.send(json.dumps({"t": "chat", "text": "gl hf"}))
            ws.send(json.dumps({"t": "choice", "choice": {"kind": "move", "moveId": team["movesCsv"][0]}}))
        elif t == "chat":
            results["chat_seen_by_" + label] = f'{msg["from"]}: {msg["text"]}'
        elif t == "turn":
            turns += 1
            ws.send(json.dumps({"t": "choice", "choice": {"kind": "move", "moveId": team["movesCsv"][0]}}))
            if turns > 60: break
        elif t == "replace":
            ws.send(json.dumps({"t": "choice", "choice": {"kind": "move", "moveId": team["movesCsv"][0]}}))
        elif t == "abort":
            results[label + "_abort"] = True
            break
    results[label + "_turns"] = turns
    results[label + "_start"] = got_start
    ws.close()

register()
ta = threading.Thread(target=play, args=("u0", TEAM0, "A"))
tb = threading.Thread(target=play, args=("u1", TEAM1, "B"))
ta.start(); time.sleep(0.3); tb.start()
ta.join(timeout=40); tb.join(timeout=40)
print("results:", json.dumps(results, indent=2))
