#!/usr/bin/env python3
"""Builds the Linux dedicated battle server through the open editor's bridge
(`build.server` → MonsterBattler/Builds/LinuxServer). Slow: target switch + full build."""
import json, urllib.request, os

_pf = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "MonsterBattler", "Temp", "MCPBridgePort.txt")
try: _PORT = int(open(_pf).read().strip())
except Exception: _PORT = 17984

body = json.dumps({"id": "x", "command": "build.server", "params": {}}).encode()
r = json.load(urllib.request.urlopen(urllib.request.Request(
    "http://127.0.0.1:%d/" % _PORT, body, {"content-type": "application/json"}), timeout=1800))
print(json.dumps(r, indent=2))
if not r.get("ok") or r.get("result", {}).get("result") != "Succeeded":
    raise SystemExit(1)
