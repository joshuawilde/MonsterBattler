// Mirrors RandomTeamGenerator's data paths against the real JSON to catch crashes
// (empty pools, unresolved items) across ALL 508 species, and prints a sample team.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const D = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../MonsterBattler/Assets/StreamingAssets/dex');
const load = (f) => JSON.parse(fs.readFileSync(path.join(D, f), 'utf8'));
const sp = load('species.json'), mv = load('moves.json'), it = load('items.json'), rb = load('randbats.json');
const toID = (s) => s.toLowerCase().replace(/[^a-z0-9]/g, '');

let rngState = 12345 >>> 0;
const rand = (n) => { rngState = (rngState * 1664525 + 1013904223) >>> 0; return rngState % n; };
const sample = (a) => a[rand(a.length)];

const SETUP = new Set(['Setup Sweeper', 'Bulky Setup', 'Fast Bulky Setup']);
const ATK_IRREL = new Set(['bodypress','foulplay','seismictoss','nightshade','endeavor','superfang','dragonrage','sonicboom','finalgambit','metalburst','counter']);

function buildMoves(species, set, tera) {
  const pool = set.movepool.map(toID);
  if (pool.length <= 4) return pool.slice();
  const chosen = [];
  const take = (id) => { const i = pool.indexOf(id); if (id && i >= 0 && !chosen.includes(id) && chosen.length < 4) { chosen.push(id); pool.splice(i, 1); } };
  if (set.role === 'Tera Blast user') take('terablast');
  if (set.role === 'Bulky Support') { take('rapidspin'); take('defog'); }
  if (pool.includes('stickyweb')) take('stickyweb');
  const types = species.types;
  const isStab = (id) => { const m = mv[id]; return m && m.basePower > 0 && types.includes(m.type); };
  const isTypeDmg = (id, t) => { const m = mv[id]; return m && m.basePower > 0 && m.type === t; };
  if (!chosen.some(isStab)) { const c = pool.filter(isStab); if (c.length) take(sample(c)); }
  if (set.role !== 'Bulky Support' && !chosen.some((id) => isTypeDmg(id, tera))) { const c = pool.filter((id) => isTypeDmg(id, tera)); if (c.length) take(sample(c)); }
  while (chosen.length < 4 && pool.length) take(pool[rand(pool.length)]);
  return chosen;
}
function pickItem(set, abilityId, moves) {
  if (abilityId === 'protosynthesis' || abilityId === 'quarkdrive') return 'boosterenergy';
  if (set.role === 'AV Pivot') return 'assaultvest';
  if (set.role === 'Fast Support') return 'heavydutyboots';
  if (set.role === 'Fast Attacker' || set.role === 'Wallbreaker') {
    const dmg = moves.map((id) => mv[id]).filter((m) => m.basePower > 0);
    if (dmg.length && dmg.every((m) => m.category === 'Physical')) return 'choiceband';
    if (dmg.length && dmg.every((m) => m.category === 'Special')) return 'choicespecs';
    return 'lifeorb';
  }
  if (SETUP.has(set.role)) return 'lifeorb';
  return 'leftovers';
}
const Hp = (b, ev, L) => Math.floor((2*b+31+Math.floor(ev/4))*L/100)+L+10;
const Other = (b, ev, L) => Math.floor((2*b+31+Math.floor(ev/4))*L/100)+5;

// Exhaustive crash check: build every set of every species.
let built = 0, problems = [];
for (const id of Object.keys(rb)) {
  if (!sp[id]) { problems.push(`species ${id} missing in dex`); continue; }
  for (const set of rb[id].sets) {
    const tera = set.teraTypes.length ? sample(set.teraTypes) : sp[id].types[0];
    const moves = buildMoves(sp[id], set, tera);
    if (!moves.length) problems.push(`${id}/${set.role}: 0 moves`);
    for (const m of moves) if (!mv[m]) problems.push(`${id}: bad move ${m}`);
    const ab = set.abilities.length ? toID(sample(set.abilities)) : null;
    const item = pickItem(set, ab, moves);
    if (!it[item]) problems.push(`${id}/${set.role}: item '${item}' not in items.json`);
    built++;
  }
}
console.log(`Built ${built} sets across ${Object.keys(rb).length} species. Problems: ${problems.length}`);
problems.slice(0, 20).forEach((p) => console.log('  !', p));

// Sample team
console.log('\n=== sample team (seed 12345) ===');
const ids = Object.keys(rb).filter((id) => sp[id]);
const team = [];
const poolIds = ids.slice();
while (team.length < 6) { const i = rand(poolIds.length); team.push(poolIds[i]); poolIds.splice(i, 1); }
for (const id of team) {
  const e = rb[id], set = sample(e.sets);
  const tera = set.teraTypes.length ? sample(set.teraTypes) : sp[id].types[0];
  const moves = buildMoves(sp[id], set, tera);
  const ab = set.abilities.length ? toID(sample(set.abilities)) : '?';
  const item = pickItem(set, ab, moves);
  let evs = { atk: 85, spe: 85 };
  const keepAtk = moves.some((m) => mv[m].category === 'Physical' && mv[m].basePower > 0 && !ATK_IRREL.has(m));
  if (!keepAtk) evs.atk = 0;
  if (moves.includes('gyroball') || moves.includes('trickroom')) evs.spe = 0;
  const b = sp[id].baseStats;
  const hp = Hp(b.hp, 85, e.level), spe = Other(b.spe, evs.spe, e.level);
  console.log(`${sp[id].name} L${e.level} [${set.role}] @${item} ${ab} tera:${tera}`);
  console.log(`   moves: ${moves.map((m) => mv[m].name).join(', ')}`);
  console.log(`   HP ${hp}  Spe ${spe}  atkEV ${evs.atk}`);
}
