// Extracts the embedded gen9 randbats curated set data (`randomSetsJSON`) from
// @pkmn/randoms' bundle into a standalone JSON file, and prints a survey.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const src = fs.readFileSync(
  path.join(__dirname, 'node_modules/@pkmn/randoms/build/index.js'), 'utf8');

function extractObject(varName) {
  const marker = `var ${varName} = `;
  const start = src.indexOf(marker);
  if (start < 0) throw new Error(`${varName} not found`);
  let i = src.indexOf('{', start);
  let depth = 0, inStr = false, esc = false;
  const begin = i;
  for (; i < src.length; i++) {
    const c = src[i];
    if (inStr) { if (esc) esc = false; else if (c === '\\') esc = true; else if (c === '"') inStr = false; }
    else if (c === '"') inStr = true;
    else if (c === '{') depth++;
    else if (c === '}') { depth--; if (depth === 0) { i++; break; } }
  }
  return src.slice(begin, i);
}

const json = extractObject('randomSetsJSON');
const data = JSON.parse(json);

const OUT = path.resolve(__dirname, '../../MonsterBattler/Assets/StreamingAssets/dex/randbats.json');
fs.writeFileSync(OUT, JSON.stringify(data, null, 1) + '\n');

// Survey
const species = Object.keys(data);
const roles = {};
let setCount = 0;
const levels = {};
let hasEvs = 0, hasIvs = 0;
for (const s of species) {
  const e = data[s];
  levels[e.level] = (levels[e.level] || 0) + 1;
  for (const set of e.sets) {
    setCount++;
    roles[set.role] = (roles[set.role] || 0) + 1;
    if (set.evs) hasEvs++;
    if (set.ivs) hasIvs++;
  }
}
console.log(`randbats.json: ${species.length} species, ${setCount} sets -> ${OUT}`);
console.log('Roles:', JSON.stringify(roles, null, 0));
console.log('sets with explicit evs:', hasEvs, ' ivs:', hasIvs);
console.log('level range:', Math.min(...Object.keys(levels).map(Number)), '-', Math.max(...Object.keys(levels).map(Number)));
console.log('sample (great tusk):', JSON.stringify(data['greattusk']));
