// "How many effects left?" — measures effect coverage of the gen9 randbats set specifically.
import fs from 'node:fs'; import path from 'node:path'; import { fileURLToPath } from 'node:url';
import { Dex } from '@pkmn/dex'; import { Generations } from '@pkmn/data';
const __d = path.dirname(fileURLToPath(import.meta.url));
const D = path.resolve(__d, '../../MonsterBattler/Assets/StreamingAssets/dex');
const load = (f) => JSON.parse(fs.readFileSync(path.join(D, f), 'utf8'));
const rb = load('randbats.json'), moves = load('moves.json');
const impl = new Set(fs.readFileSync('/tmp/impl_effects.txt', 'utf8').split(/\s+/).filter(Boolean));
const toID = (s) => s.toLowerCase().replace(/[^a-z0-9]/g, '');
const g = new Generations(Dex).get(9);

// --- abilities used in randbats ---
const ab = new Set();
for (const e of Object.values(rb)) for (const s of e.sets) for (const a of s.abilities) ab.add(toID(a));
const abMissing = [...ab].filter((a) => !impl.has(a));
console.log(`ABILITIES used in randbats: ${ab.size} | implemented: ${ab.size - abMissing.length} | MISSING: ${abMissing.length}`);
console.log('  missing:', abMissing.sort().join(' '));

// --- moves used in randbats ---
const mv = new Set();
for (const e of Object.values(rb)) for (const s of e.sets) for (const m of s.movepool) mv.add(toID(m));
let statusDone = 0, statusMissing = [], dmgSecMissing = [], pureData = 0;
for (const id of mv) {
  const ours = moves[id]; const ps = g.moves.get(id);
  if (!ps) continue;
  const hasEffectId = ours && ours.effectId;
  if (ps.category === 'Status') {
    // Done if it has a custom effect OR is fully captured by data (self-boost setup moves are
    // applied generically by the engine from selfBoosts).
    if (hasEffectId || (ours && (ours.selfBoosts || ours.pivotsOut))) statusDone++; else statusMissing.push(id);
  } else {
    // damaging: needs custom code only if it has a meaningful secondary that the data-driven
    // secondary system doesn't already cover (status/boosts/self/non-flinch volatile).
    const secs = ps.secondaries || (ps.secondary ? [ps.secondary] : []);
    const meaningful = secs.some((s) => s && (s.status || s.boosts || s.self || (s.volatileStatus && s.volatileStatus !== 'flinch')));
    const modelled = (ours && (ours.secondaries || ours.selfBoosts)) || hasEffectId;
    if (meaningful && !modelled) dmgSecMissing.push(id);
    else pureData++;
  }
}
console.log(`\nMOVES used in randbats: ${mv.size}`);
console.log(`  status moves:  done ${statusDone}, MISSING ${statusMissing.length}`);
console.log(`  damaging w/ unmodelled secondary: MISSING ${dmgSecMissing.length}`);
console.log(`  pure-data damaging (no effect needed): ${pureData}`);
console.log('\n  missing status moves:', statusMissing.sort().join(' '));
console.log('\n  sample missing dmg-secondary moves:', dmgSecMissing.sort().slice(0, 40).join(' '));

const totalMissing = abMissing.length + statusMissing.length + dmgSecMissing.length;
console.log(`\n=== TOTAL distinct effects still needed for randbats parity: ~${totalMissing} ===`);
console.log(`   (${abMissing.length} abilities + ${statusMissing.length} status moves + ${dmgSecMissing.length} damaging-move secondaries)`);
