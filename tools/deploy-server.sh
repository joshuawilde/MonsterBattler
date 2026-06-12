#!/bin/zsh
# Build + deploy the dedicated battle server to Rivet.
# Prereqs: Unity editor open (for the bridge build), docker running, `rivet login` done once.
set -e
cd "$(dirname "$0")/.."

echo "==> 1/3 building Linux dedicated server (via editor bridge)…"
python3 tools/build-server.py

echo "==> 2/3 building docker image (linux/amd64)…"
docker buildx build --platform linux/amd64 -f server/Dockerfile -t monsterbattler-server .

echo "==> 3/3 deploying to Rivet…"
rivet deploy "$@"
