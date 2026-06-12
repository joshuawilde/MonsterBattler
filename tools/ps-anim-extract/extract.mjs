#!/usr/bin/env node
/**
 * extract.mjs — Pokemon Showdown move-animation extractor.
 *
 * Evaluates the PS client's BattleMoveAnims table (battle-animations-moves.ts)
 * against mock scene/sprite objects, records every showEffect / backgroundEffect /
 * sprite.anim / sprite.delay call, and solves each numeric scalar as an affine
 * function of the attacker/defender anchor coordinates:
 *
 *     v = ca * A + cd * D + k
 *
 * where A/D are the attacker/defender value of the same axis (x->x, y->y, z->z;
 * non-positional scalars are solved against the x anchors and normally come out
 * as pure constants). Each anim is run 3 times with different anchors to solve
 * the 3x3 system, then a 4th run verifies the fit; any scalar off by > 0.01
 * marks the move "exact": false and falls back to the run-1 raw value.
 *
 * Usage:  node extract.mjs
 * Output: anims.json, summary.json (next to this script)
 *
 * No dependencies — uses Node's built-in TypeScript type stripping
 * (node:module stripTypeScriptTypes, Node >= 22.13).
 */

import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';
import { stripTypeScriptTypes } from 'node:module';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const SOURCES = [
	{
		tmp: '/tmp/ps-anims-moves.ts',
		url: 'https://raw.githubusercontent.com/smogon/pokemon-showdown-client/master/play.pokemonshowdown.com/src/battle-animations-moves.ts',
	},
	{
		tmp: '/tmp/ps-anims.ts',
		url: 'https://raw.githubusercontent.com/smogon/pokemon-showdown-client/master/play.pokemonshowdown.com/src/battle-animations.ts',
	},
];

async function loadSource({ tmp, url }) {
	if (!fs.existsSync(tmp)) {
		console.log(`fetching ${url} -> ${tmp}`);
		const res = await fetch(url);
		if (!res.ok) throw new Error(`fetch failed ${res.status} for ${url}`);
		fs.writeFileSync(tmp, await res.text());
	}
	return fs.readFileSync(tmp, 'utf8');
}

// ---------------------------------------------------------------------------
// Deterministic PRNG (mulberry32). Reset to SEED before EVERY run of every
// anim so repeated runs are bit-identical.
// ---------------------------------------------------------------------------
const SEED = 0xC0FFEE;
function mulberry32(seed) {
	let a = seed >>> 0;
	return function () {
		a |= 0; a = (a + 0x6D2B79F5) | 0;
		let t = Math.imul(a ^ (a >>> 15), 1 | a);
		t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
		return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
	};
}
let rng = mulberry32(SEED);
function resetRng() { rng = mulberry32(SEED); }

// Math object for the vm context: real Math with a seeded random().
const mockMath = Object.create(null);
for (const k of Object.getOwnPropertyNames(Math)) {
	mockMath[k] = Math[k];
}
mockMath.random = () => rng();

// ---------------------------------------------------------------------------
// Mock scene + sprites. Signatures mirror battle-animations.ts:
//   showEffect(effect, start, end, transition, after?, additionalCss?)
//   animateEffect($effect, effect, start, end, transition, after?, additionalCss?)
//   backgroundEffect(bg, duration, opacity = 1, delay = 0)
//   wait(time) -> timeOffset += time   (timeOffset is added to effect times)
//   Sprite: x, y, z, sp, isFrontSprite, isMissedPokemon,
//           behindx/behindy/leftof/behind, anim(end, transition?), delay(time)
// ---------------------------------------------------------------------------
const chainable = () => {
	const stub = {};
	for (const m of ['animate', 'delay', 'css', 'queue', 'attr']) stub[m] = () => stub;
	return stub;
};

function makeRun(anchors) {
	const rec = [];

	function makeSprite(who, [x, y, z], isFrontSprite) {
		const sprite = {
			x, y, z,
			sp: { __who: who, w: 96, h: 96, url: '' },
			isFrontSprite,
			isMissedPokemon: false,
			behindx(offset) { return this.x + (this.isFrontSprite ? 1 : -1) * offset; },
			behindy(offset) { return this.y + (this.isFrontSprite ? -1 : 1) * offset; },
			leftof(offset) { return this.x + (this.isFrontSprite ? 1 : -1) * offset; },
			behind(offset) { return this.z + (this.isFrontSprite ? 1 : -1) * offset; },
			anim(end, transition) {
				end = { x: this.x, y: this.y, z: this.z, scale: 1, opacity: 1, time: 500, ...end };
				const step = { type: 'monAnim', who, to: end };
				if (transition) step.ease = transition;
				rec.push(step);
				return this;
			},
			delay(time) {
				rec.push({ type: 'monDelay', who, time });
				return this;
			},
		};
		return sprite;
	}

	const attacker = makeSprite('attacker', anchors.a, false);
	const defender = makeSprite('defender', anchors.d, true);

	function spriteName(effect) {
		if (typeof effect === 'string') return effect;
		if (effect === attacker.sp) return 'attacker.sp';
		if (effect === defender.sp) return 'defender.sp';
		// inline SpriteData literal (e.g. orderup's tatsugiri sprite)
		const obj = {};
		if (effect.url !== undefined) obj.url = effect.url;
		if (effect.w !== undefined) obj.w = effect.w;
		if (effect.h !== undefined) obj.h = effect.h;
		return obj;
	}

	// Mirrors BattleScene#animateEffect's normalization of start/end states.
	function recordEffect(scene, chainHandle, effect, start, end, transition, after, additionalCss) {
		start = { ...start };
		end = { ...end };
		if (!start.time) start.time = 0;
		if (!end.time) end.time = start.time + 500;
		start.time += scene.timeOffset;
		end.time += scene.timeOffset;
		if (!end.scale && end.scale !== 0 && start.scale) end.scale = start.scale;
		if (!end.xscale && end.xscale !== 0 && start.xscale) end.xscale = start.xscale;
		if (!end.yscale && end.yscale !== 0 && start.yscale) end.yscale = start.yscale;
		end = { ...start, ...end };
		const step = { type: 'effect', sprite: spriteName(effect), from: start, to: end };
		if (transition) step.ease = transition;
		if (after) step.fade = after;
		if (additionalCss) step.css = additionalCss;
		if (chainHandle) step.chain = chainHandle.idx; // continues the element of steps[chain]
		const idx = rec.push(step) - 1;
		return { __fx: true, idx };
	}

	const scene = {
		timeOffset: 0,
		battle: { mySide: { x: 0, y: 0, z: 0 }, farSide: { x: 0, y: 0, z: 0 } },
		$bg: chainable(),
		acceleration: 1,
		gen: 9,
		wait(time) { this.timeOffset += time; },
		showEffect(effect, start, end, transition, after, additionalCss) {
			return recordEffect(this, null, effect, start, end, transition, after, additionalCss);
		},
		animateEffect($effect, effect, start, end, transition, after, additionalCss) {
			return recordEffect(this, $effect, effect, start, end, transition, after, additionalCss);
		},
		backgroundEffect(bg, duration, opacity = 1, delay = 0) {
			rec.push({ type: 'bg', color: bg, duration, opacity, delay });
		},
	};

	return { rec, scene, attacker, defender };
}

// ---------------------------------------------------------------------------
// Anchor coordinate sets. Runs 0-2 solve the affine system, run 3 verifies.
// ---------------------------------------------------------------------------
const RUNS = [
	{ a: [100, 80, 50], d: [-90, 60, -40] },
	{ a: [13, 27, 91], d: [57, -33, 8] },
	{ a: [-71, 5, -23], d: [19, 88, 64] },
	{ a: [42, -17, 33], d: [-58, 71, -12] }, // verification run
];

const AXIS = { x: 0, y: 1, z: 2 };
function anchorsFor(key, run) {
	const axis = AXIS[key] ?? 0; // non-positional scalars solve against x anchors
	return [run.a[axis], run.d[axis]];
}

function det3(m) {
	return m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1])
		- m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0])
		+ m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]);
}

// Solve [A D 1][ca cd k]^T = v for the 3 solve runs, per axis key.
function solveScalar(key, values) {
	const rows = [0, 1, 2].map(i => [...anchorsFor(key, RUNS[i]), 1]);
	const D = det3(rows);
	const col = j => det3(rows.map((r, i) => r.map((c, jj) => (jj === j ? values[i] : c))));
	return { ca: col(0) / D, cd: col(1) / D, k: col(2) / D };
}

// Sanity-check anchor conditioning once.
for (const key of ['x', 'y', 'z']) {
	const D = det3([0, 1, 2].map(i => [...anchorsFor(key, RUNS[i]), 1]));
	if (Math.abs(D) < 1e3) throw new Error(`anchor matrix for axis ${key} is poorly conditioned (det=${D})`);
}

function tidy(c) {
	if (Math.abs(c) < 1e-7) return 0;
	const r = Math.round(c);
	if (Math.abs(c - r) < 1e-7) return r;
	return Math.round(c * 1e6) / 1e6;
}

function coeffObj({ ca, cd, k }) {
	const o = {};
	if (ca !== 0) o.ca = ca;
	if (cd !== 0) o.cd = cd;
	if (k !== 0 || (ca === 0 && cd === 0)) o.k = k;
	return o;
}

// Structural signature: a run with numbers blanked out. Two runs of the same
// anim must produce identical signatures or the move is non-affine in shape.
function signature(v) {
	if (typeof v === 'number') return '#';
	if (Array.isArray(v)) return v.map(signature);
	if (v && typeof v === 'object') {
		const o = {};
		for (const k of Object.keys(v)) o[k] = signature(v[k]);
		return o;
	}
	return v;
}

// Walk run-0 steps; replace each number with solved coefficients using the
// values at the same path in all runs. Returns { steps, exact }.
function solveSteps(runRecs) {
	let exact = true;
	const PLAIN_KEYS = new Set(['duration', 'delay', 'chain']); // bg timing + chain index stay plain numbers

	function walk(values, key, plain) {
		const v0 = values[0];
		if (typeof v0 === 'number') {
			if (plain) {
				// must be constant across runs
				if (values.some(v => Math.abs(v - v0) > 0.01)) exact = false;
				return tidy(v0);
			}
			const { ca, cd, k } = solveScalar(key, values.slice(0, 3));
			const tca = tidy(ca), tcd = tidy(cd), tk = tidy(k);
			const [a4, d4] = anchorsFor(key, RUNS[3]);
			const pred = tca * a4 + tcd * d4 + tk;
			if (!isFinite(pred) || Math.abs(pred - values[3]) > 0.01) {
				exact = false;
				return { k: tidy(v0) }; // run-1 raw value
			}
			return coeffObj({ ca: tca, cd: tcd, k: tk });
		}
		if (Array.isArray(v0)) {
			return v0.map((_, i) => walk(values.map(v => v[i]), key, plain));
		}
		if (v0 && typeof v0 === 'object') {
			const o = {};
			for (const k of Object.keys(v0)) {
				o[k] = walk(values.map(v => v[k]), k, plain || PLAIN_KEYS.has(k) || k === 'css');
			}
			return o;
		}
		// strings/bools must match across runs (same seed => deterministic)
		if (values.some(v => v !== v0)) exact = false;
		return v0;
	}

	// bg steps keep plain numbers per schema; everything else gets coefficients
	const steps = runRecs[0].map((step0, i) => {
		const stepVals = runRecs.map(r => r[i]);
		if (step0.type === 'bg') {
			return walk(stepVals, null, true);
		}
		const out = {};
		for (const k of Object.keys(step0)) {
			out[k] = walk(stepVals.map(s => s[k]), k,
				k === 'type' || k === 'who' || k === 'sprite' || k === 'ease' || k === 'fade' ||
				PLAIN_KEYS.has(k) || k === 'css');
		}
		return out;
	});
	return { steps, exact };
}

// Fallback for shape-mismatched moves: run-1 raw values as {k: v} constants.
function rawSteps(recs) {
	function walk(v, plain) {
		if (typeof v === 'number') return plain ? tidy(v) : { k: tidy(v) };
		if (Array.isArray(v)) return v.map(x => walk(x, plain));
		if (v && typeof v === 'object') {
			const o = {};
			for (const k of Object.keys(v)) {
				o[k] = walk(v[k], plain || k === 'css' || k === 'duration' || k === 'delay' || k === 'chain');
			}
			return o;
		}
		return v;
	}
	return recs.map(step => {
		if (step.type === 'bg') return walk(step, true);
		const o = {};
		for (const k of Object.keys(step)) {
			o[k] = walk(step[k], ['type', 'who', 'sprite', 'ease', 'fade', 'css', 'duration', 'delay', 'chain'].includes(k));
		}
		return o;
	});
}

// ---------------------------------------------------------------------------
// Evaluate the TS sources in a vm context.
// ---------------------------------------------------------------------------
function tsToJs(src) {
	const stripped = stripTypeScriptTypes(src, { mode: 'strip' });
	return stripped
		.replace(/^import .*$/gm, '')
		.replace(/^export const /gm, 'const ');
}

async function main() {
	const movesSrc = await loadSource(SOURCES[0]);
	const animsSrc = await loadSource(SOURCES[1]);

	// Extract the BattleOtherAnims table (moves reference it for shared anims
	// like contactattack/dance) from battle-animations.ts.
	const startIdx = animsSrc.indexOf('export const BattleOtherAnims');
	const endIdx = animsSrc.indexOf('export const BattleStatusAnims');
	if (startIdx < 0 || endIdx < 0) throw new Error('could not locate BattleOtherAnims block');
	const otherAnimsSrc = animsSrc.slice(startIdx, endIdx);

	const context = vm.createContext({
		Math: mockMath,
		Config: { routes: { client: 'play.pokemonshowdown.com' } },
		console,
	});

	vm.runInContext(tsToJs(otherAnimsSrc) + '\nglobalThis.BattleOtherAnims = BattleOtherAnims;', context, { filename: 'battle-animations(BattleOtherAnims).js' });
	vm.runInContext(tsToJs(movesSrc) + '\nglobalThis.BattleMoveAnims = BattleMoveAnims;', context, { filename: 'battle-animations-moves.js' });

	const table = context.BattleMoveAnims;
	const moveIds = Object.keys(table);
	console.log(`BattleMoveAnims entries: ${moveIds.length}`);

	const out = {};
	const errors = [];
	const spriteCounts = {};
	const easeCounts = {};
	const fadeCounts = {};
	let exactCount = 0, inexactCount = 0;

	function extractFn(fn) {
		const recs = RUNS.map(run => {
			resetRng();
			const { rec, scene, attacker, defender } = makeRun(run);
			fn(scene, [attacker, defender]);
			return rec;
		});
		const sig0 = JSON.stringify(signature(recs[0]));
		const sameShape = recs.every(r => JSON.stringify(signature(r)) === sig0);
		if (!sameShape) {
			return { steps: rawSteps(recs[0]), exact: false };
		}
		return solveSteps(recs);
	}

	for (const id of moveIds) {
		const entry = table[id];
		try {
			let exact = true;
			const result = {};
			if (typeof entry.anim === 'function') {
				const r = extractFn(entry.anim);
				result.steps = r.steps;
				exact &&= r.exact;
			} else {
				result.steps = [];
			}
			if (typeof entry.prepareAnim === 'function') {
				const r = extractFn(entry.prepareAnim);
				result.prepare = r.steps;
				exact &&= r.exact;
			}
			if (typeof entry.residualAnim === 'function') {
				const r = extractFn(entry.residualAnim);
				result.residual = r.steps;
				exact &&= r.exact;
			}
			out[id] = { exact, ...result };
			if (exact) exactCount++; else inexactCount++;

			for (const steps of [result.steps, result.prepare, result.residual]) {
				if (!steps) continue;
				for (const step of steps) {
					if (step.type === 'effect') {
						const name = typeof step.sprite === 'string' ? step.sprite : `<inline:${step.sprite.url ?? '?'}>`;
						spriteCounts[name] = (spriteCounts[name] || 0) + 1;
						if (step.ease) easeCounts[step.ease] = (easeCounts[step.ease] || 0) + 1;
						if (step.fade) fadeCounts[step.fade] = (fadeCounts[step.fade] || 0) + 1;
					} else if (step.type === 'monAnim' && step.ease) {
						easeCounts[step.ease] = (easeCounts[step.ease] || 0) + 1;
					}
				}
			}
		} catch (err) {
			errors.push({ move: id, error: String(err && err.message || err) });
			out[id] = { error: String(err && err.message || err) };
		}
	}

	const sortDesc = obj => Object.fromEntries(Object.entries(obj).sort((a, b) => b[1] - a[1]));
	const summary = {
		totalEntries: moveIds.length,
		extracted: moveIds.length - errors.length,
		exact: exactCount,
		inexact: inexactCount,
		inexactMoves: moveIds.filter(id => out[id] && out[id].exact === false),
		errors,
		sprites: sortDesc(spriteCounts),
		eases: sortDesc(easeCounts),
		fades: sortDesc(fadeCounts),
	};

	fs.writeFileSync(path.join(__dirname, 'anims.json'), JSON.stringify(out));
	fs.writeFileSync(path.join(__dirname, 'summary.json'), JSON.stringify(summary, null, 2));

	console.log(`extracted: ${summary.extracted}/${summary.totalEntries}  exact: ${exactCount}  inexact: ${inexactCount}  errors: ${errors.length}`);
	if (errors.length) console.log('errors:', errors);
	console.log('distinct sprites:', Object.keys(spriteCounts).length);
	console.log('eases:', summary.eases);
	console.log('fades:', summary.fades);
}

main().catch(err => { console.error(err); process.exit(1); });
