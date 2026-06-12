// Create a MonsterBattler server actor on Rivet (one actor per battle — spin one up when a
// match is made, let it die after). Mirrors the pattern in ~/NPCLLMGame/Backend/rivet.ts.
//
//   cd tools && npm install @rivet-gg/api
//   RIVET_CLOUD_TOKEN=... RIVET_PROJECT=<project-slug> node rivet-create-actor.mjs
//
// The actor's public hostname:port (Rivet dashboard → actor → network → "game") goes into
// NetBootstrap's host/port (or -mphost/-mpport). The server binds PORT_game inside the container.
import { RivetClient } from "@rivet-gg/api";

const token = process.env.RIVET_CLOUD_TOKEN;
const project = process.env.RIVET_PROJECT;
const environment = process.env.RIVET_ENV ?? "prod";
if (!token || !project) {
  console.error("Set RIVET_CLOUD_TOKEN and RIVET_PROJECT (and optionally RIVET_ENV).");
  process.exit(1);
}

const client = new RivetClient({ token });
const res = await client.actors.create({
  project,
  environment,
  body: {
    tags: { name: "battle-server" },
    buildTags: { name: "game", current: "true" },   // matches rivet.json build name
    network: {
      ports: {
        game: { protocol: "udp", internalPort: 7777, routing: { host: {} } },
      },
    },
    resources: { cpu: 1000, memory: 1024 },          // 1 core / 1GB plenty for a turn-based relay
    // per-match actor: not durable — create on matchmake, dies with the battle
  },
});
console.log(JSON.stringify(res, null, 2));
