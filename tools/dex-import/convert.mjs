// Converts @pkmn gen9 data into MonsterBattler's StreamingAssets/dex JSON schema.
//
// Output matches what Assets/Scripts/Game/DexLoader.cs parses. Hand-authored
// `effectId` fields on existing moves/abilities/items are PRESERVED (merged on top
// of regenerated pure-data), so re-running this never clobbers Effect-class links.
//
// Usage: node convert.mjs   (writes to ../../MonsterBattler/Assets/StreamingAssets/dex)

import { Dex } from '@pkmn/dex';
import { Generations } from '@pkmn/data';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.resolve(__dirname, '../../MonsterBattler/Assets/StreamingAssets/dex');

const gens = new Generations(Dex);
const g = gens.get(9);

const toID = (s) => (s || '').toLowerCase().replace(/[^a-z0-9]+/g, '');

// PS move target -> our MoveTarget enum name (DexLoader parses case-insensitively).
const TARGET_MAP = {
  normal: 'Normal', self: 'Self', any: 'Normal', randomNormal: 'RandomNormal',
  adjacentAlly: 'AdjacentAlly', adjacentAllyOrSelf: 'AllyOrSelf', allyOrSelf: 'AllyOrSelf',
  allAdjacent: 'AllAdjacent', allAdjacentFoes: 'AllAdjacentFoes', all: 'All',
  allySide: 'AllySide', allyTeam: 'AllySide', allies: 'AllySide', foeSide: 'FoeSide',
  scripted: 'Normal',
};

// Read existing file (if any) so we can preserve hand-authored fields.
function readExisting(name) {
  const p = path.join(OUT, name);
  if (!fs.existsSync(p)) return {};
  try { return JSON.parse(fs.readFileSync(p, 'utf8')); } catch { return {}; }
}

function flinchChance(move) {
  const all = move.secondaries || (move.secondary ? [move.secondary] : []);
  for (const s of all) if (s && s.volatileStatus === 'flinch') return s.chance || 0;
  return 0;
}

function convertSpecies() {
  const out = {};
  for (const s of g.species) {
    if (s.isNonstandard) continue; // gen9-legal SV+DLC pool only
    out[s.id] = {
      name: s.name,
      types: s.types,
      baseStats: {
        hp: s.baseStats.hp, atk: s.baseStats.atk, def: s.baseStats.def,
        spa: s.baseStats.spa, spd: s.baseStats.spd, spe: s.baseStats.spe,
      },
      abilities: [...new Set(Object.values(s.abilities).map(toID))],
    };
  }
  return out;
}

function convertMoves() {
  const existing = readExisting('moves.json');
  const out = {};
  for (const m of g.moves) {
    if (m.isNonstandard) continue;
    const o = {
      name: m.name,
      type: m.type,
      category: m.category, // 'Physical' | 'Special' | 'Status'
      basePower: m.basePower || 0,
      accuracy: m.accuracy === true ? 0 : m.accuracy, // PS true = never-miss = our 0
      pp: m.pp || 0,
    };
    if (m.priority) o.priority = m.priority;
    const crit = (m.willCrit ? 4 : (m.critRatio || 1)) - 1; // our 0 = base 1/24
    if (crit) o.critRatio = crit;
    if (Array.isArray(m.recoil)) { o.recoilNum = m.recoil[0]; o.recoilDen = m.recoil[1]; }
    if (Array.isArray(m.drain)) { o.drainNum = m.drain[0]; o.drainDen = m.drain[1]; }
    if (m.selfdestruct) o.selfKO = true;
    if (m.selfSwitch) o.pivotsOut = true;
    if (m.multihit !== undefined) {
      if (Array.isArray(m.multihit)) { o.multihitMin = m.multihit[0]; o.multihitMax = m.multihit[1]; }
      else { o.multihitMin = m.multihit; o.multihitMax = m.multihit; }
    }
    const fl = flinchChance(m);
    if (fl) o.flinchChance = fl;
    if (m.flags && m.flags.charge) o.twoTurn = true;
    o.target = TARGET_MAP[m.target] || 'Normal';

    const f = m.flags || {};
    const flags = {};
    for (const [ours, ps] of [['contact','contact'],['protect','protect'],['sound','sound'],
      ['punch','punch'],['bite','bite'],['slicing','slicing'],['wind','wind'],['bullet','bullet']]) {
      if (f[ps]) flags[ours] = 1;
    }
    if (Object.keys(flags).length) o.flags = flags;

    // Preserve hand-authored effectId.
    const prev = existing[m.id];
    if (prev && prev.effectId) o.effectId = prev.effectId;
    out[m.id] = o;
  }
  // Keep any existing move ids not present in the gen9 set (don't drop hand-added data).
  for (const id of Object.keys(existing)) if (!out[id]) out[id] = existing[id];
  return out;
}

function convertAbilities() {
  const existing = readExisting('abilities.json');
  const out = {};
  for (const a of g.abilities) {
    if (a.isNonstandard) continue;
    const o = { name: a.name };
    const prev = existing[a.id];
    if (prev && prev.effectId) o.effectId = prev.effectId;
    out[a.id] = o;
  }
  for (const id of Object.keys(existing)) if (!out[id]) out[id] = existing[id];
  return out;
}

function convertItems() {
  const existing = readExisting('items.json');
  const out = {};
  for (const it of g.items) {
    if (it.isNonstandard) continue;
    const o = { name: it.name };
    if (it.isBerry) o.isBerry = true;
    if (it.isBerry || it.isGem) o.consumedOnUse = true;
    const prev = existing[it.id];
    if (prev && prev.effectId) o.effectId = prev.effectId;
    out[it.id] = o;
  }
  for (const id of Object.keys(existing)) if (!out[id]) out[id] = existing[id];
  return out;
}

function write(name, obj) {
  const p = path.join(OUT, name);
  fs.writeFileSync(p, JSON.stringify(obj, null, 2) + '\n');
  console.log(`${name}: ${Object.keys(obj).length} entries -> ${p}`);
}

write('species.json', convertSpecies());
write('moves.json', convertMoves());
write('abilities.json', convertAbilities());
write('items.json', convertItems());
console.log('Done.');
