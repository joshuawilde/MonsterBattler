import { Dex } from '@pkmn/dex';
import { Generations } from '@pkmn/data';
const gens = new Generations(Dex);
const g = gens.get(9);

function pick(o, keys){const r={};for(const k of keys)if(o[k]!==undefined)r[k]=o[k];return r;}

let count=0, nonstd={};
for (const s of g.species) { count++; nonstd[s.isNonstandard||'null']=(nonstd[s.isNonstandard||'null']||0)+1; }
console.log('SPECIES total iterated:', count, 'isNonstandard buckets:', nonstd);
const pik = g.species.get('pikachu');
console.log('pikachu:', JSON.stringify(pick(pik,['id','name','types','baseStats','abilities','isNonstandard','gen','num'])));

let mc=0; for(const m of g.moves) mc++;
console.log('MOVES total:', mc);
for (const id of ['tackle','uturn','flamethrower','solarbeam','doubleedge','gigadrain','rockslide','bulletseed','closecombat','protect','spikes','trickroom','knockoff','explosion','fly','earthquake','crosspoison']) {
  const m=g.moves.get(id);
  if(!m){console.log(id,'MISSING');continue;}
  console.log(id, JSON.stringify(pick(m,['category','basePower','accuracy','pp','priority','critRatio','willCrit','recoil','drain','selfdestruct','selfSwitch','multihit','secondary','secondaries','target','flags'])));
}
let ac=0;for(const a of g.abilities)ac++; let ic=0;for(const i of g.items)ic++;
console.log('ABILITIES:',ac,'ITEMS:',ic);
const lo=g.items.get('lifeorb'), berry=g.items.get('sitrusberry');
console.log('lifeorb:',JSON.stringify(pick(lo,['id','name','isBerry','isGem','fling','naturalGift'])));
console.log('sitrus:',JSON.stringify(pick(berry,['id','name','isBerry','isGem'])));
