# Pure-dotnet WebSocket battle server. Build context = repo root:
#   docker build -f battle-server/Dockerfile -t monsterbattler-battle .
# Runs many concurrent turn-based matches in one process (async I/O).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# the csproj compiles the sim + AI sources directly, so the whole repo subset is needed
COPY battle-server/ battle-server/
COPY MonsterBattler/Assets/Scripts/Sim/ MonsterBattler/Assets/Scripts/Sim/
COPY MonsterBattler/Assets/Scripts/Game/AI/ MonsterBattler/Assets/Scripts/Game/AI/
RUN dotnet publish battle-server/MonsterBattler.BattleServer.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/ /app/
# dex data lives beside the binary (DexData looks for ./dex when DEX_DIR is unset)
COPY MonsterBattler/Assets/StreamingAssets/dex/ /app/dex/
# Rivet/most platforms reject root; harmless on a VPS too
RUN useradd --uid 10001 --no-create-home --shell /usr/sbin/nologin mb && chown -R mb /app
USER 10001
ENV PORT=8081
EXPOSE 8081
ENTRYPOINT ["dotnet", "MonsterBattler.BattleServer.dll"]
