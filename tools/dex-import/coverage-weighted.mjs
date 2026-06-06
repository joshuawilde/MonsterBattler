// Usage-weighted coverage: not "how many effects exist" but "how often does a battle hit one we have".
import fs from 'node:fs'; import path from 'node:path'; import { fileURLToPath } from 'node:url';
const D = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../MonsterBattler/Assets/StreamingAssets/dex');
const load = (f) => JSON.parse(fs.readFileSync(path.join(D, f), 'utf8'));
const rb = load('randbats.json');
const impl = new Set(fs.readFileSync('/tmp/impl_effects.txt', 'utf8').split(/\s+/).filter(Boolean));
const toID = (s) => s.toLowerCase().replace(/[^a-z0-9]/g, '');

// Ability usage weighted by # of sets that can roll it.
const abCount = {};
let setTotal = 0;
for (const e of Object.values(rb)) for (const s of e.sets) {
  setTotal++;
  for (const a of s.abilities) { const id = toID(a); abCount[id] = (abCount[id] || 0) + 1 / s.abilities.length; }
}
let abImplWeight = 0, abTotalWeight = 0;
for (const [id, w] of Object.entries(abCount)) { abTotalWeight += w; if (impl.has(id)) abImplWeight += w; }
console.log(`Ability slots covered by weight: ${(100 * abImplWeight / abTotalWeight).toFixed(0)}%`);

const missingRanked = Object.entries(abCount).filter(([id]) => !impl.has(id)).sort((a, b) => b[1] - a[1]);
console.log('\nTop 25 missing abilities by usage (≈sets that can roll them):');
console.log(missingRanked.slice(0, 25).map(([id, w]) => `${id}(${Math.round(w)})`).join('  '));

// What % of the missing weight is in the top 30?
const totMissW = missingRanked.reduce((s, [, w]) => s + w, 0);
const top30W = missingRanked.slice(0, 30).reduce((s, [, w]) => s + w, 0);
console.log(`\nTop 30 missing abilities = ${(100 * top30W / totMissW).toFixed(0)}% of all missing ability usage (${missingRanked.length} total missing).`);
