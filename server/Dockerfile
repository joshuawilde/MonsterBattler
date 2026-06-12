# MonsterBattler dedicated battle server (FishNet lockstep relay + authoritative sim).
# Build context is the repo root:
#   docker buildx build --platform linux/amd64 -f server/Dockerfile -t monsterbattler-server .
# The Linux server player is produced by the editor bridge command `build.server`
# (tools/build-server.py) into MonsterBattler/Builds/LinuxServer/.
FROM ubuntu:22.04

RUN apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates libstdc++6 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY MonsterBattler/Builds/LinuxServer/ /app/
RUN chmod +x /app/MonsterBattler.x86_64

# Rivet injects PORT_game (see rivet.json); fall back to 7777 for local docker runs.
EXPOSE 7777/udp
ENTRYPOINT ["/bin/sh", "-c", "/app/MonsterBattler.x86_64 -batchmode -nographics -mpserver -mpport ${PORT_game:-7777} -logFile /dev/stdout"]
