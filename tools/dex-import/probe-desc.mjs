import { Dex } from '@pkmn/dex'; import { Generations } from '@pkmn/data';
const g = new Generations(Dex).get(9);
for (const id of ['flamethrower','knockoff','closecombat','stealthrock','uturn','willowisp']) {
  const m = g.moves.get(id);
  console.log(id.padEnd(12), '| short:', JSON.stringify(m.shortDesc));
}
console.log('---abilities---');
for (const id of ['protosynthesis','intimidate','levitate','sheerforce']) {
  const a = g.abilities.get(id);
  console.log(id.padEnd(16), '| short:', JSON.stringify(a.shortDesc));
}
// size estimate
let mvShort=0,mvLong=0;
for (const m of g.moves){ mvShort+=(m.shortDesc||'').length; mvLong+=(m.desc||'').length; }
console.log(`\nmove shortDesc total ~${(mvShort/1024)|0}KB, desc total ~${(mvLong/1024)|0}KB`);
