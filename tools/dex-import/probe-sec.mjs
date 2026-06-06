import { Dex } from '@pkmn/dex'; import { Generations } from '@pkmn/data';
const g = new Generations(Dex).get(9);
for (const id of ['flamethrower','crunch','closecombat','overheat','firefang','chargebeam','poweruppunch','acidspray','icebeam','scald','dynamicpunch','superpower','dracometeor','rockslide','meteormash']) {
  const m = g.moves.get(id);
  const o = { secondary: m.secondary, secondaries: m.secondaries, self: m.self };
  console.log(id.padEnd(14), JSON.stringify(o));
}
